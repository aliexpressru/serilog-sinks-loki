using System.Threading.Tasks.Dataflow;
using Aerx.Serilog.Sinks.Loki.Extensions;
using Aerx.Serilog.Sinks.Loki.HttpClient;
using Aerx.Serilog.Sinks.Loki.Interfaces;
using Aerx.Serilog.Sinks.Loki.Metrics;
using Aerx.Serilog.Sinks.Loki.Models;
using Aerx.Serilog.Sinks.Loki.Options;
using Microsoft.Extensions.ObjectPool;
using Microsoft.Extensions.Options;
using Serilog.Core;
using Serilog.Debugging;
using Serilog.Events;
using Serilog.Formatting;

// ReSharper disable PrivateFieldCanBeConvertedToLocalVariable

namespace Aerx.Serilog.Sinks.Loki.Logger;

internal class LokiSink : ILogEventSink
{
    private readonly BatchBlock<LokiLogEvent> _headBlock;
    private readonly TransformBlock<LokiLogEvent[], LokiBatch> _formatterBlock;
    private readonly ActionBlock<LokiBatch> _tailBlock;

    public LokiSink(
        IOptions<LokiOptions> lokiOptions,
        ITextFormatter textFormatter,
        ILokiBatchFormatter batchFormatter,
        LokiHttpClientPooledObjectPolicy pooledObjectPolicy, 
        IMeterService meterService)
    {
        var pool = new DefaultObjectPoolProvider
        {
            MaximumRetained = lokiOptions.Value.ParallelismFactor ?? Constants.DefaultParallelismFactor
        }.Create(pooledObjectPolicy);
        
        var batchPostingLimit = lokiOptions.Value.BatchPostingLimit ?? Constants.DefaultBatchPostingLimit;

        var executionDataflowBlockOptions = new ExecutionDataflowBlockOptions
        {
            MaxDegreeOfParallelism = lokiOptions.Value.ParallelismFactor ?? Constants.DefaultParallelismFactor,
            BoundedCapacity = DataflowBlockOptions.Unbounded,
            EnsureOrdered = false
        };

        _headBlock = new BatchBlock<LokiLogEvent>(
            batchPostingLimit,
            new GroupingDataflowBlockOptions
            {
                BoundedCapacity = DataflowBlockOptions.Unbounded,
                EnsureOrdered = false
            });

        _formatterBlock = new TransformBlock<LokiLogEvent[], LokiBatch>(
            messages =>
            {
                try
                {
                    return batchFormatter.Format(messages, textFormatter);
                }
                catch (Exception e)
                {
                    meterService.ObserveOnLogsWriteFail(messages.Length);
                    SelfLog.WriteLine($"Transformation block exception: {e}");
                    return new LokiBatch();
                }
            },
            executionDataflowBlockOptions);

        _tailBlock = new ActionBlock<LokiBatch>(
            async batch =>
            {
                var batchSize = batch.Streams.Sum(x => x.Values.Count);

                if (batch.Streams.Count > 0)
                {
                    var httpClient = pool.Get();

                    try
                    {
                        var result = await httpClient.Push(batch).ConfigureAwait(false);

                        if (result.Response is null)
                        {
                            SelfLog.WriteLine("Empty response");
                        }
                        else if (result.Response is not { IsSuccessStatusCode: true })
                        {
                            SelfLog.WriteLine(
                                "Received failure on HTTP request ({0}): {1}",
                                (int)result.Response.StatusCode,
                                await result.Response.Content.ReadAsStringAsync().ConfigureAwait(false));
                        }
                        else
                        {
                            meterService.ObserveOnLogsWriteSuccess(batchSize);
                            meterService.ObserveOnLogsBatchSize(result.ContentSizeInKb);
                        }
                    }
                    catch (Exception)
                    {
                        meterService.ObserveOnLogsWriteFail(batchSize);
                    }
                    finally
                    {
                        pool.Return(httpClient);
                    }
                }
            },
            executionDataflowBlockOptions);

        _headBlock.LinkToWithPropagation(_formatterBlock);
        _formatterBlock.LinkToWithPropagation(_tailBlock);

        AppDomain.CurrentDomain.ProcessExit += (_, _) =>
        {
            _headBlock.Complete();
            _tailBlock.Completion.GetAwaiter().GetResult();
        };

        Console.CancelKeyPress += (_, _) =>
        {
            _headBlock.Complete();
            _tailBlock.Completion.GetAwaiter().GetResult();
        };
    }

    public void Emit(LogEvent logEvent)
    {
        if (logEvent == null)
        {
            throw new ArgumentNullException(nameof(logEvent));
        }

        _headBlock.Post(new LokiLogEvent(logEvent.Copy()));
    }
}

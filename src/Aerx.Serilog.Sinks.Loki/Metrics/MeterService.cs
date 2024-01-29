using System.Diagnostics.Metrics;
using Aerx.Serilog.Sinks.Loki.Options;
using Microsoft.Extensions.Options;

namespace Aerx.Serilog.Sinks.Loki.Metrics;

internal class MeterService: IMeterService
{
    public static readonly string MeterName = typeof(MeterService).FullName;

    private readonly Counter<long> _logsWriteFailCounter;
    private readonly Counter<long> _logsWriteSuccessCounter;
    private readonly Counter<double> _logsBatchSizeCounter;
    private readonly KeyValuePair<string, object>[] _labels;
    
    private readonly IOptions<LokiOptions> _lokiOptions;

    public MeterService(IMeterFactory meterFactory, IOptions<LokiOptions> lokiOptions)
    {
        _lokiOptions = lokiOptions;

        if (_lokiOptions.Value.EnableMetrics)
        {
            _labels = new []{ new KeyValuePair<string, object>("app_name", lokiOptions.Value.AppName) };
            var meter = meterFactory.Create(MeterName);

            _logsWriteFailCounter = meter.CreateCounter<long>(
                name: _lokiOptions.Value.Metrics?.LogsWriteFailCounterName ?? "logs_write_fail_counter",
                description: string.Empty);

            _logsWriteSuccessCounter = meter.CreateCounter<long>(
                _lokiOptions.Value.Metrics?.LogsWriteSuccessCounterName ?? "logs_write_success_counter",
                description: string.Empty);

            _logsBatchSizeCounter = meter.CreateCounter<double>(
                _lokiOptions.Value.Metrics?.LogsSizeKbCounterName ?? "logs_size_kb_counter",
                description: string.Empty);
        }
    }

    public void ObserveOnLogsWriteFail(int failLogsCount)
    {
        if (_lokiOptions.Value.EnableMetrics)
        {
            _logsWriteFailCounter
                .Add(failLogsCount, _labels);
        }
    }
    
    public void ObserveOnLogsWriteSuccess(int successLogsCount)
    {
        if (_lokiOptions.Value.EnableMetrics)
        {
            _logsWriteSuccessCounter
                .Add(successLogsCount, _labels);
        }
    }
    
    public void ObserveOnLogsBatchSize(double kbSize)
    {
        if (_lokiOptions.Value.EnableMetrics)
        {
            _logsBatchSizeCounter
                .Add(kbSize, _labels);
        }
    }
}
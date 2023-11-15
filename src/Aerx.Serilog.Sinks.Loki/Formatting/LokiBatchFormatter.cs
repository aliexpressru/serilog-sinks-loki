using System.Text;
using Aerx.Serilog.Sinks.Loki.Extensions;
using Aerx.Serilog.Sinks.Loki.Interfaces;
using Aerx.Serilog.Sinks.Loki.Models;
using Aerx.Serilog.Sinks.Loki.Options;
using Microsoft.Extensions.Options;
using Serilog.Debugging;
using Serilog.Events;
using Serilog.Formatting;
using LokiBatch = Aerx.Serilog.Sinks.Loki.Models.LokiBatch;
using LokiLabel = Aerx.Serilog.Sinks.Loki.Models.LokiLabel;

namespace Aerx.Serilog.Sinks.Loki.Formatting;

internal class LokiBatchFormatter : ILokiBatchFormatter
{
    private readonly Dictionary<string, string> _globalLabels;
    private readonly IEnumerable<string> _propertiesAsLabels;
    private readonly bool _leavePropertiesIntact;
    private readonly bool _useInternalTimestamp;
    private readonly char[] _charsToTrim = { '\r', '\n' };

    public LokiBatchFormatter(IOptions<LokiOptions> lokiOptions)
    {
        var labels = lokiOptions.Value.Labels?.Select(p =>
            new Models.LokiLabel
            {
                Key = p.Key,
                Value = p.Value
            }) ?? Enumerable.Empty<Models.LokiLabel>();

        _globalLabels = labels.ToDictionary(label => label.Key, label => label.Value);

        _propertiesAsLabels = lokiOptions.Value.PropertiesAsLabels ?? Enumerable.Empty<string>();
        _useInternalTimestamp = lokiOptions.Value.UseInternalTimestamp ?? false;
        _leavePropertiesIntact = lokiOptions.Value.LeavePropertiesIntact ?? false;
    }

    public Models.LokiBatch Format(IReadOnlyCollection<Models.LokiLogEvent> lokiLogEvents, ITextFormatter formatter)
    {
        var batch = new Models.LokiBatch();

        if (formatter == null || lokiLogEvents == null || lokiLogEvents.Count == 0)
        {
            return batch;
        }
        
        var groups = lokiLogEvents
            .Select(lokiLogEvent =>
            {
                lokiLogEvent.LogEvent
                    .RenamePropertyIfPresent(Constants.Level, originalName => $"_{originalName}");

                lokiLogEvent.LogEvent.AddOrUpdateProperty(
                    new LogEventProperty(Constants.Level,
                        new ScalarValue(lokiLogEvent.LogEvent.Level.ToGrafanaLogLevel()))
                );

                return lokiLogEvent;
            })
            .Select(GenerateLabels)
            .GroupBy(
                le => le.Labels,
                le => le.LokiLogEvent,
                DictionaryComparer<string, string>.Instance
            );

        foreach (var group in groups)
        {
            var labels = group.Key;
            var stream = new Models.LokiBatch.LokiStream();

            foreach (var (key, value) in labels)
            {
                stream.Stream[key] = value;
            }

            foreach (var logEvent in group.OrderBy(
                         x =>
                             _useInternalTimestamp
                                 ? x.InternalTimestamp
                                 : x.LogEvent.Timestamp))
            {
                if (logEvent.LogEvent.MessageTemplate.Text.StartsWith(Constants.Target)
                    || logEvent.LogEvent.MessageTemplate.Text.StartsWith(Constants.Mesh))
                {
                    // we assume that the Target and Mesh identifiers are embedded in the message
                    // thus we are saving several allocations
                    // if this is incorrect - use old version
                    continue;
                }

                using var buffer = new StringWriter(new StringBuilder(Constants.DefaultWriteBufferCapacity));

                var timestamp = logEvent.LogEvent.Timestamp;

                if (_useInternalTimestamp)
                {
                    logEvent.LogEvent.AddPropertyIfAbsent(
                        new LogEventProperty(Constants.Timestamp, new ScalarValue(timestamp)));

                    timestamp = logEvent.InternalTimestamp;
                }

                formatter.Format(logEvent.LogEvent, buffer);

                stream.Values.Add(
                    new[]
                    {
                        (timestamp.ToUnixTimeMilliseconds() * Constants.NanosecondsMultiplier).ToString(),
                        buffer.ToString().TrimEnd(_charsToTrim)
                    }
                );
            }

            if (stream.Values.Count > 0)
            {
                batch.Streams.Add(stream);
            }
        }

        return batch;
    }

    private (Dictionary<string, string> Labels, Models.LokiLogEvent LokiLogEvent) GenerateLabels(LokiLogEvent lokiLogEvent)
    {
        var labels = new Dictionary<string, string>(_globalLabels);

        var properties = lokiLogEvent.Properties;

        var (propertiesAsLabels, remainingProperties) =
            properties.Partition(
                kvp => _propertiesAsLabels.Contains(kvp.Key));

        foreach (var property in propertiesAsLabels)
        {
            if (property.Value == null)
            {
                continue;
            }

            var key = property.Key;

            if (char.IsDigit(key[0]))
            {
                key = $"param{key}";
            }

            if (labels.ContainsKey(key))
            {
                var valueWithTheSameKey = property.Value;

                SelfLog.WriteLine(
                    "Labels already contains key {0}, added from global labels. Property value ({1}) with the same key is ignored",
                    key,
                    valueWithTheSameKey);

                continue;
            }

            // here we know that property.Value is not null
            var value = property.Value.ToString()!.Replace("\"", string.Empty);

            labels.Add(key, value);
        }

        return (labels, lokiLogEvent.CopyWithProperties(
            _leavePropertiesIntact
                ? properties
                : remainingProperties));
    }
}
using Serilog.Events;

namespace Aerx.Serilog.Sinks.Loki.Models;

public class LokiLogEvent
{
    public DateTimeOffset InternalTimestamp { get; }

    public LogEvent LogEvent { get; private set; }

    public IReadOnlyDictionary<string, LogEventPropertyValue> Properties => LogEvent.Properties;
    
    public LokiLogEvent(LogEvent logEvent)
    {
        InternalTimestamp = DateTimeOffset.Now;
        LogEvent = logEvent;
    }

    internal LokiLogEvent CopyWithProperties(IEnumerable<KeyValuePair<string, LogEventPropertyValue>> properties)
    {
        LogEvent = new LogEvent(
            LogEvent.Timestamp,
            LogEvent.Level,
            LogEvent.Exception,
            LogEvent.MessageTemplate,
            properties.Where(x => x.Value is not null)
                .Select(p => new LogEventProperty(p.Key, p.Value)));

        return this;
    }
}
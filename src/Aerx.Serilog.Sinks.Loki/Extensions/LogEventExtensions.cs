using Serilog.Events;

namespace Aerx.Serilog.Sinks.Loki.Extensions;

internal static class LogEventExtensions
{
    internal static void RenamePropertyIfPresent(
        this LogEvent logEvent,
        string propertyName,
        Func<string, string> renamingStrategy)
    {
        if (logEvent.Properties.TryGetValue(propertyName, out var value))
        {
            var newName = renamingStrategy(propertyName);
            logEvent.RemovePropertyIfPresent(propertyName);
            logEvent.RenamePropertyIfPresent(newName, renamingStrategy);
            logEvent.AddOrUpdateProperty(
                new LogEventProperty(newName, value ?? new ScalarValue(""))
            );
        }
    }
    
    internal static string ToGrafanaLogLevel(this LogEventLevel level) =>
        level switch
        {
            LogEventLevel.Verbose => Constants.Trace,
            LogEventLevel.Debug => Constants.Debug,
            LogEventLevel.Information => Constants.Info,
            LogEventLevel.Warning => Constants.Warning,
            LogEventLevel.Error => Constants.Error,
            LogEventLevel.Fatal => Constants.Critical,
            _ => Constants.Unknown
        };

    internal static string WithStringWriter<T>(this T template, Action<T, StringWriter> func)
    {
        using var sw = new StringWriter();
        func(template, sw);
        
        return sw.ToString();
    } 
    
    public static LogEvent Copy(this LogEvent logEvent)
    {
        var properties = new List<LogEventProperty>(logEvent.Properties.Count);
        properties.AddRange(logEvent.Properties.Keys.Select(key => new LogEventProperty(key, logEvent.Properties[key])));

        return new LogEvent(
            logEvent.Timestamp,
            logEvent.Level,
            logEvent.Exception,
            logEvent.MessageTemplate,
            properties);
    }
}
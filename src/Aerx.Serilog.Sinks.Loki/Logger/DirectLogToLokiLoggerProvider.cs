using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using Serilog.Extensions.Logging;
using Serilog.Formatting;
using Serilog.Sinks.SystemConsole.Themes;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace Aerx.Serilog.Sinks.Loki.Logger;

internal class DirectLogToLokiLoggerProvider : ILoggerProvider
{
    private readonly ILogEventEnricher[] _enrichers;
    private readonly LokiSink _lokiSink;
    private readonly ITextFormatter _consoleTextFormatter;

    public DirectLogToLokiLoggerProvider(
        IEnumerable<ILogEventEnricher> enrichers,
        LokiSink lokiSink, 
        ITextFormatter consoleTextFormatter = null)
    {
        _enrichers = enrichers.ToArray();
        _lokiSink = lokiSink;
        _consoleTextFormatter = consoleTextFormatter;
    }

    public ILogger CreateLogger(string categoryName)
    {
        var logger = Log.Logger;

        var loggerType = logger.GetType();
        if (loggerType.Name != "SilentLogger")
        {
            return new SerilogLoggerProvider(logger).CreateLogger(categoryName);
        }

        var serilogConfig = new LoggerConfiguration()
            .MinimumLevel.Information()
            .MinimumLevel.Override("System", LogEventLevel.Warning)
            .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
            .MinimumLevel.Override("Jaeger", LogEventLevel.Warning)
            .MinimumLevel.Override("OpenTracing", LogEventLevel.Warning)
            .Enrich.FromLogContext()
            .Enrich.With(_enrichers);

        if (Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Local")
        {
            var theme = RuntimeInformation.IsOSPlatform(OSPlatform.OSX)
                ? ConsoleTheme.None
                : null;
            
            serilogConfig.WriteTo.Async(x => x.Console(theme: theme));
        }

        if (_consoleTextFormatter != null)
        {
            serilogConfig.WriteTo.Async(x => x.Console(_consoleTextFormatter));
        }
        
        serilogConfig.WriteTo.Sink(_lokiSink, LevelAlias.Minimum);

        logger = serilogConfig.CreateLogger();
        Log.Logger = logger;

        var loggerProvider = new SerilogLoggerProvider(logger);

        return loggerProvider.CreateLogger(categoryName);
    }

    public void Dispose()
    {
    }
}
using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using Serilog.Extensions.Logging;
using Serilog.Sinks.SystemConsole.Themes;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace Aerx.Serilog.Sinks.Loki.Logger;

internal class DirectLogToLokiLoggerProvider : ILoggerProvider
{
    private readonly ILogEventEnricher[] _enrichers;
    private readonly LokiSink _lokiSink;

    public DirectLogToLokiLoggerProvider(
        IEnumerable<ILogEventEnricher> enrichers,
        LokiSink lokiSink)
    {
        _enrichers = enrichers.ToArray();
        _lokiSink = lokiSink;
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
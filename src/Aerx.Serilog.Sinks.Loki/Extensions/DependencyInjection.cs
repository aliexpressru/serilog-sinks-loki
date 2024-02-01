using Aerx.Serilog.Sinks.Loki.Formatting;
using Aerx.Serilog.Sinks.Loki.HttpClient;
using Aerx.Serilog.Sinks.Loki.Interfaces;
using Aerx.Serilog.Sinks.Loki.Logger;
using Aerx.Serilog.Sinks.Loki.Metrics;
using Aerx.Serilog.Sinks.Loki.Options;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenTelemetry.Metrics;
using Serilog.Core;
using Serilog.Formatting;
using Serilog.Formatting.Json;

namespace Aerx.Serilog.Sinks.Loki.Extensions;

public static class DependencyInjection
{
    /// <summary>
    /// Add direct to loki logging
    /// Required properties in configuration
    /// "Loki": {
    ///     "AppName": "app-name",
    ///     "Labels": {
    ///         "app": "app-name"
    ///     },
    ///     "PropertiesAsLabels": [
    ///         "level",
    ///         "another_prop"
    ///     ]
    /// }
    /// 
    /// If you want to turn on metrics to export, set 
    /// "EnableMetrics": true,
    /// "Metrics": {
    ///     "MapPath": "/metrics",
    ///     "UseDefaultCollectors": false,
    ///     "LogsWriteFailCounterName": "logs_write_fail_counter",
    ///     "LogsWriteSuccessCounterName": "logs_write_success_counter",
    ///     "LogsSizeKbCounterName": "logs_size_kb_counter"
    /// }
    /// 
    /// "Metrics" section is optional. The default values are listed above.
    /// 
    /// Also need to configure http client for direct to loki push
    /// services.AddHttpClient<ILokiHttpClient, LokiGzipHttpClient>(c => c.BaseAddress = new Uri("http://your-domain-loki.net"))
    /// 
    /// </summary>
    /// <param name="services">ServiceCollection</param>
    /// <param name="configuration">IConfiguration</param>
    /// <param name="configureHttp">Configuration of httpClient</param>
    /// <param name="consoleFormatter">Default console formatter</param>
    public static IServiceCollection AddDirectToLokiLogging(this IServiceCollection services,
        IConfiguration configuration,
        Action<IServiceCollection> configureHttp,
        ITextFormatter consoleFormatter = null)
    {
        if (services == null)
        {
            throw new ArgumentNullException(nameof(services));
        }

        if (!IsValid(configuration))
        {
            return services;
        }

        services.AddOptions<LokiOptions>()
            .BindConfiguration(Constants.Loki)
            .ValidateDataAnnotations()
            .ValidateOnStart();
        
        configureHttp(services);

        services.AddLogging(b => b.ClearProviders().SetMinimumLevel(LogLevel.Trace));
        services.TryAddSingleton(x => new JsonValueFormatter(Constants.TypeTagName));
        services.TryAddSingleton<ITextFormatter, LokiMessageFormatter>();
        services.TryAddSingleton<ILokiBatchFormatter, LokiBatchFormatter>();
        services.TryAddSingleton<LokiHttpClientPooledObjectPolicy>();
        services.TryAddSingleton<LokiSink>();
        services.TryAddSingleton<ILoggerProvider>(sp => new DirectLogToLokiLoggerProvider(
                sp.GetServices<ILogEventEnricher>(),
                sp.GetRequiredService<LokiSink>(),
                consoleFormatter));
        services.TryAddSingleton<IMeterService, MeterService>();
        
        return services;
    }
    
    /// <summary>
    /// Add metrics for open telemetry
    /// </summary>
    /// <param name="builder">Meter provider from OpenTelemetry lib</param>
    /// <returns></returns>
    public static MeterProviderBuilder AddDirectLokiLoggingMeter(this MeterProviderBuilder builder) => 
        builder.AddMeter(MeterService.MeterName);

    /// <summary>
    /// Add direct to loki logging
    /// Required properties in configuration
    /// "Loki": {
    ///     "AppName": "app-name",
    ///     "Labels": {
    ///         "app": "app-name"
    ///     },
    ///     "PropertiesAsLabels": [
    ///         "level",
    ///         "another_prop"
    ///     ]
    /// }
    /// 
    /// If you want to turn on metrics to export, set 
    /// "EnableMetrics": true,
    /// "Metrics": {
    ///     "MapPath": "/metrics",
    ///     "UseDefaultCollectors": false,
    ///     "LogsWriteFailCounterName": "logs_write_fail_counter",
    ///     "LogsWriteSuccessCounterName": "logs_write_success_counter",
    ///     "LogsSizeKbCounterName": "logs_size_kb_counter"
    /// }
    /// 
    /// "Metrics" section is optional. The default values are listed above.
    /// Also need to configure http client for direct to loki push like an action delegate&
    /// 
    /// </summary>
    /// <param name="services">ServiceCollection</param>
    /// <param name="configuration">IConfiguration</param>
    /// <param name="httpClientConfiguration">Configuration of httpClient</param>
    /// <param name="consoleFormatter">Default console formatter</param>
    /// <returns></returns>
    public static IServiceCollection AddDirectToLokiLogging(this IServiceCollection services,
        IConfiguration configuration,
        Action<System.Net.Http.HttpClient> httpClientConfiguration,
        ITextFormatter consoleFormatter = null)
    {
        return AddDirectToLokiLogging(services, 
            configuration, 
            s => s.AddHttpClient<ILokiHttpClient, LokiGzipHttpClient>(httpClientConfiguration),
            consoleFormatter);
    }

    /// <summary>
    /// This function uses LokiOptions.LogLoadTest flag to check
    /// headers from LoadTestHeaders option
    /// if headers in ctx contains one of LoadTestHeaders
    /// log was ignore for request/response
    /// </summary>
    /// <param name="ctx">HttpContext of request pipe</param>
    /// <returns></returns>
    public static bool RequestResponseLoggingFilter(this HttpContext ctx)
    {
        if (ctx?.Request == null)
        {
            return true;
        }

        var options = ctx.RequestServices.GetRequiredService<IOptions<LokiOptions>>().Value;

        if (options.SkipReqResWithHeaders?.Count > 0)
        {
            foreach (var (header, value) in options.SkipReqResWithHeaders)
            {
                var res = ctx.CheckHeader(header, value);
                if (res)
                {
                    return false;
                }
            }
        }

        return true;
    }

    private static bool CheckHeader(this HttpContext ctx, string header, string value) =>
        (ctx?.Request?.Headers?.TryGetValue(header, out var val) ?? false) && value.Equals(val, StringComparison.InvariantCultureIgnoreCase);
    
    private static bool IsValid(IConfiguration configuration)
    {
        var lokiSection = configuration.GetSection(Constants.Loki);
        if (!lokiSection.Exists())
        {
            return false;
        }

        var disabled = lokiSection.GetValue<bool?>(Constants.Disabled);
        if (disabled.HasValue && disabled.Value)
        {
            return false;
        }
        
        var appName = lokiSection.GetValue<string>(Constants.AppName);
        var labels = lokiSection.GetSection(Constants.Labels);
        var propertiesAsLabels = lokiSection.GetSection(Constants.PropertiesAsLabels);
        
        return labels.Exists()
               && propertiesAsLabels.Exists()
               && !string.IsNullOrEmpty(appName);
    }
}
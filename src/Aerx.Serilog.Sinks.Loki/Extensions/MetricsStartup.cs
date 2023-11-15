using Aerx.Serilog.Sinks.Loki.Options;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Prometheus.Client;
using Prometheus.Client.Collectors;

namespace Aerx.Serilog.Sinks.Loki.Extensions
{
    internal sealed class MetricsStartup : IStartupFilter
    {
        private readonly ICollectorRegistry _collectorRegistry;
        private readonly IOptions<LokiOptions> _lokiOptions;
        
        public MetricsStartup(
            ICollectorRegistry collectorRegistry, 
            IOptions<LokiOptions> lokiOptions)
        {
            _collectorRegistry = collectorRegistry;
            _lokiOptions = lokiOptions;
        }
        
        public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)
        {
            return app =>
            {
                app.UsePrometheusServer(_lokiOptions.Value.Metrics, _collectorRegistry);
                next(app);
            };
        }
    }
    
    public static class PrometheusExtensions
    {
        public static IApplicationBuilder UsePrometheusServer(this IApplicationBuilder app,
            LokiOptions.MetricsOptions metricsOptions, ICollectorRegistry collectorRegistry)
        {
            if (app == null)
            {
                throw new ArgumentNullException(nameof(app));
            }
            
            if (metricsOptions == null)
            {
                metricsOptions = new LokiOptions.MetricsOptions();
            }

            if (string.IsNullOrEmpty(metricsOptions.MapPath))
            {
                metricsOptions.MapPath = "/metrics";
            }
            
            if (!metricsOptions.MapPath.StartsWith("/"))
            {
                throw new ArgumentException($"MapPath '{metricsOptions.MapPath}' should start with '/'");
            }
            
            if (metricsOptions.UseDefaultCollectors)
            {
                collectorRegistry.UseDefaultCollectors();
            }
            
            if (metricsOptions.Port == null)
            {
                return app.Map(metricsOptions.MapPath, AddMetricsHandler);
            }
            
            return app.Map(metricsOptions.MapPath, cfg => cfg.MapWhen(PortMatches, AddMetricsHandler));
            
            bool PortMatches(HttpContext context) => context.Connection.LocalPort == metricsOptions.Port;
            
            void AddMetricsHandler(IApplicationBuilder coreapp)
            {
                coreapp.Run(async context =>
                {
                    var response = context.Response;
                    response.ContentType = "text/plain; version=0.0.4";
                    await using var outputStream = response.Body;
                    await ScrapeHandler.ProcessAsync(collectorRegistry, outputStream);
                });
            }
        }
    }
}

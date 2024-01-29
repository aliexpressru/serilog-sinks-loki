using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;

namespace WebApi;

internal sealed class MetricsStartup : IStartupFilter
{
    private readonly IServiceProvider _serviceProvider;

    public MetricsStartup(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }
    
    public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)
    {
        return app =>
        {
            app.UseOpenTelemetryPrometheusScrapingEndpoint();
            next(app);
        };
    }
}

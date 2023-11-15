using Aerx.Serilog.Sinks.Loki.Interfaces;
using Aerx.Serilog.Sinks.Loki.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.ObjectPool;
using Microsoft.Extensions.Options;

namespace Aerx.Serilog.Sinks.Loki.HttpClient;

internal class LokiHttpClientPooledObjectPolicy: PooledObjectPolicy<ILokiHttpClient>
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IOptions<LokiOptions> _lokiOptions;

    public LokiHttpClientPooledObjectPolicy(
        IServiceProvider serviceProvider,
        IOptions<LokiOptions> lokiOptions)
    {
        _serviceProvider = serviceProvider;
        _lokiOptions = lokiOptions;
    }

    public override ILokiHttpClient Create() =>
        _serviceProvider
            .GetRequiredService<ILokiHttpClient>()
            .SetTenantId(_lokiOptions.Value.TenantId);

    public override bool Return(ILokiHttpClient obj) => true;
}
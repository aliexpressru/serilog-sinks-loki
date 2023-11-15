using Aerx.Serilog.Sinks.Loki.Models;

namespace Aerx.Serilog.Sinks.Loki.Interfaces;

public interface ILokiHttpClient : IDisposable
{
    Task<LokiPushResponse> Push(LokiBatch batch);

    ILokiHttpClient SetTenantId(string tenantId);
}
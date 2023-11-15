using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Aerx.Serilog.Sinks.Loki.Tests.Helpers;

internal static class ServiceCollectionExtensions
{
    public static IServiceCollection AddMock<T>(this IServiceCollection services) where T : class
    {
        var mock = new Mock<T>();
        services.AddScoped<Mock<T>>(_ => mock);
        services.AddScoped<T>(_ => mock.Object);

        return services;
    }
}

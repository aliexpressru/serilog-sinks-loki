using Aerx.Serilog.Sinks.Loki.Extensions;
using Aerx.Serilog.Sinks.Loki.Interfaces;
using Aerx.Serilog.Sinks.Loki.Logger;
using Aerx.Serilog.Sinks.Loki.Metrics;
using Aerx.Serilog.Sinks.Loki.Models;
using Aerx.Serilog.Sinks.Loki.Tests.Helpers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Serilog.Events;
using Serilog.Parsing;

namespace Aerx.Serilog.Sinks.Loki.Tests;

[TestClass]
public class UnitTests
{
    private IServiceProvider Init()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder().AddJsonFile("appsettings.json")
            .Build();

        services.AddDirectToLokiLogging(configuration, httpClient =>
        {
            httpClient.BaseAddress = new Uri(configuration.GetValue<string>("LokiHttpClient:BaseAddress"));
            httpClient.Timeout = TimeSpan.FromSeconds(5);
        });
        
        services.AddSingleton<IConfiguration>(configuration);
        services.AddMock<IMetricService>();
        services.AddMock<ILokiHttpClient>();

        var sp = services.BuildServiceProvider();
        
        var client = sp.GetRequiredService<Mock<ILokiHttpClient>>();
        var metricService = sp.GetRequiredService<Mock<IMetricService>>();
        
        client.Setup(x => x.Push(It.IsAny<LokiBatch>()))
            .ReturnsAsync(new LokiPushResponse
            {
                ContentSizeInKb = 10,
                Response = new HttpResponseMessage
                {
                    StatusCode = System.Net.HttpStatusCode.OK,
                    Content = new StringContent("OK")
                }
            });
        
        client.Setup(x => x.SetTenantId(It.IsAny<string>()))
            .Returns(client.Object);
        
        metricService.Setup(m => m.ObserveOnLogsBatchSize(It.IsAny<double>()));
        metricService.Setup(m => m.ObserveOnLogsWriteSuccess(It.IsAny<int>()));
        
        return sp;
    }
    
    [TestMethod]
    public async Task GetAllControllers_AllDependenciesExists_Success()
    {
        var task1 = Task.Run(() =>
        {
            Host.CreateDefaultBuilder(Array.Empty<string>())
                .ConfigureServices((builder, s) => s.AddDirectToLokiLogging(builder.Configuration, httpClient =>
                {
                    httpClient.BaseAddress = new Uri("http://localhost:5000");
                    httpClient.Timeout = TimeSpan.FromSeconds(5);
                }))
                .Build();
        });

        var task2 = Task.Delay(1000);

        await Task.WhenAny(task1, task2);

        if (!task1.IsCompleted)
        {
            Assert.Fail("Too long IoC registration");
        }
    }

    [TestMethod]
    public async Task TestMetrics()
    {
        var sp = Init();

        var lokiSink = sp.GetRequiredService<LokiSink>();
        var metricService = sp.GetRequiredService<Mock<IMetricService>>();

        for (var j = 1; j <= 1_000; j++)
        {
            lokiSink.Emit(
                new LogEvent(
                    DateTimeOffset.Now,
                    LogEventLevel.Information,
                    null,
                    new MessageTemplate(
                        "log",
                        new[]
                        {
                            new TextToken("l")
                        }
                    ),
                    new[]
                    {
                        new LogEventProperty("level", new ScalarValue("Info"))
                    })
            );
        }
        
        await Task.Delay(200);
        
        metricService.Verify(m => m.ObserveOnLogsBatchSize(It.IsAny<double>()), times: Times.Exactly(500));
        metricService.Verify(m => m.ObserveOnLogsWriteSuccess(It.IsAny<int>()), times: Times.Exactly(500));
    }

    [TestMethod]
    public async Task TestFormatters()
    {
        var sp = Init();
        
        var lokiSink = sp.GetRequiredService<LokiSink>();

        for (int i = 0; i < 2; i++)
        {
            lokiSink.Emit(new LogEvent
                (
                    DateTimeOffset.Now,
                    LogEventLevel.Information,
                    null,
                    new MessageTemplate(
                        "log",
                        new[]
                        {
                            new TextToken("l")
                        }
                    ),
                    new[]
                    {
                        new LogEventProperty("level", new ScalarValue("Info"))
                    }
                )
            );
        }
        
        await Task.Delay(200);
    }
}

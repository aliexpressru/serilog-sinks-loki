using System;
using System.Threading.Tasks;
using Aerx.Serilog.Sinks.Loki.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

var services = new ServiceCollection();
var configuration = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json")
    .Build();

services.AddSingleton<IConfiguration>(_ => configuration);

services.AddDirectToLokiLogging(configuration, c =>
{
    c.BaseAddress = new Uri("http://localhost:3100");
    c.Timeout = TimeSpan.FromSeconds(5);
});

var sp = services.BuildServiceProvider();

var logger = sp.GetRequiredService<ILogger<Program>>();

for (var i = 0; i < 10; i++)
{
    logger.LogInformation("message from consoleApp");
    await Task.Delay(500);
}

Console.WriteLine("Logs writing complete.");
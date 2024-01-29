using System;
using System.Text.Json;
using Aerx.Serilog.Sinks.Loki.Extensions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using WebApi;
using WebApi.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDirectToLokiLogging(builder.Configuration, c =>
{
    c.BaseAddress = new Uri(builder.Configuration.GetValue<string>("LokiHttpClient:BaseAddress"));
    c.Timeout = TimeSpan.FromSeconds(5);
});

builder.Services.AddMetrics();
builder.Services.AddOpenTelemetry()
    .WithMetrics(b =>
    {
        b.AddPrometheusExporter(options => options.ScrapeEndpointPath = "/metrics")
            .AddDirectLokiLoggingMeter();
    });

builder.Services.TryAddEnumerable(ServiceDescriptor.Transient<IStartupFilter, MetricsStartup>());
builder.Services.AddSwaggerGen();
builder.Services.AddControllers();

var app = builder.Build();

app.UseMiddleware<RequestResponseLoggingMiddleware>();
app.UseExceptionHandler(eHandler => eHandler.Run(async context =>
{
    context.Response.StatusCode = StatusCodes.Status500InternalServerError;
    context.Response.ContentType = "application/json";
    
    var error = context.Features.Get<IExceptionHandlerPathFeature>()?.Error;

    if (error is not null)
    {
        await context.Response.WriteAsJsonAsync(new ErrorResponse
        {
            Message = error.Message,
            StackTrace = error.StackTrace
        }, new JsonSerializerOptions(JsonSerializerDefaults.Web));
    }
    
}));

app.UseSwagger().UseSwaggerUI();
app.UseRouting();
app.UseEndpoints(x => x.MapControllers());

app.Run();

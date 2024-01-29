# Aerx.Serilog.Sinks.Loki

This library allows you to log events from [Serilog](https://serilog.net/) directly to loki asynchronously without blocking the main thread of application execution (request in asp.net).

## Configuration

There is a full description of section that is needed in appsettings.json.
```
{
    "Loki": {
         "AppName": "app-name",
         "TenantId: "admin",
         "Labels": {
            "app": "app-name"
         },
         "PropertiesAsLabels": [
            "level"
         ],
         "SuppressProperties": [
            "EventId",
            "SourceContext",
            "RequestId",
            "RequestPath",
            "ConnectionId"
         ],
         "BatchPostingLimit": 50,
         "ParallelismFactor": 2,
         "UseInternalTimestamp": true,
         "LeavePropertiesIntact": false,
         "SkipReqResWithHeaders": {
            "test-header": true
         },
         "EnableMetrics": true,
         "Metrics": {
            "LogsWriteFailCounterName": "logs_write_fail_counter",
            "LogsWriteSuccessCounterName": "logs_write_success_counter",
            "LogsSizeKbCounterName": "logs_size_kb_counter"
         }
   }
}
```


### Required parameters:

---

- `AppName` (required) - your application name which lib write to logs
- `Labels` (required) - labels which provide to each log - key-value
- `PropertiesAsLabels` (required) - labels which could be extracted from logEvent

```
{
    "Loki": {
         "AppName": "app-name",
         "Labels": {
            "app": "app-name"
         },
         "PropertiesAsLabels": [
            "level"
         ]
    }
}
```



### Optional parameters:

---

- `TenantId` (default = null) - header value for `X-Scope-OrgID` which was added to typed LokiHttpClient
- `SuppressProperties` (default = false) - properties from logEvent which can be deleted before sending to loki
- `BatchPostingLimit` (default = 20) - the size of the pack that will be sent to the loki in one request
- `ParallelismFactor` (default = 1) - how many parallel tasks will maintain logs queue
- `UseInternalTimestamp` (default = false) - if `true` use internalTimestamp from log
- `LeavePropertiesIntact` (default = false) - if `true` logger remove not-labeled properties from logEvent, so formatter select only PropertiesAsLabels
- `SkipReqResWithHeaders` (default = null) - list of headers which should be ignored during logging

```
{
    "Loki": {
         "TenantId: "admin",
         "SuppressProperties": [
            "EventId",
            "SourceContext",
            "RequestId",
            "RequestPath",
            "ConnectionId"
         ],
         "BatchPostingLimit": 50,
         "ParallelismFactor": 2,
         "UseInternalTimestamp": true,
         "LeavePropertiesIntact": false,
         "SkipReqResWithHeaders": {
            "test-header": true
         }
   }
}
```

### Metrics 

---

If you need to enable metrics provide the additional section.
You already need to add OpenTelemetry nuget packages and register services in Startup.cs.
- `EnableMetrics` (default = false) - if `true` turn on metrics exporting
- `Metrics` - could be fulfilled if `EnableMetrics: true`
- `LogsWriteFailCounterName` (default = 'logs_write_fail_counter') - metric counter name for exporting (broken logs)
- `LogsWriteSuccessCounterName` (default = 'logs_write_success_counter') - metric counter name for exporting (correct logs)
- `LogsSizeKbCounterName` (default = 'logs_size_kb_counter') - metric counter name for exporting (correct logs size in kb)

```
{
    "Loki": {
         "EnableMetrics": true,
         "Metrics": {
            "MapPath": "/metrics",
            "Port": 82,
            "UseDefaultCollectors": false,
            "LogsWriteFailCounterName": "logs_write_fail_counter",
            "LogsWriteSuccessCounterName": "logs_write_success_counter",
            "LogsSizeKbCounterName": "logs_size_kb_counter"
         }
   }
}
```


## Required minimum of configuration

1) Provide section in appsettings.json
```
{
    "Loki": {
         "AppName": "app-name",
         "Labels": {
            "app": "app-name"
         },
         "PropertiesAsLabels": [
            "level"
         ]
    }
}
```

2) in Startup.ConfigureServices add line 
```
services.AddDirectToLokiLogging(configuration, c => 
   c.BaseAddress = new Uri("http://loki.net:3100"));
```

3) `Optional (1)`: if it is needed to skip logs in your RequestResponseLoggingMiddleware use
`RequestResponseLoggingFilter(httpContext)` method like an aid in determining which logs should be skipped.

4) `Optional (2)` if you need to export logs metrics, you should add 
```
services.AddOpenTelemetry()
   .WithMetrics(builder => builder
        .AddPrometheusExporter(...)
        .AddDirectLokiLoggingMeter();
   );
   ```
in startup.cs file.
   
   

## Usage

Create new ASP.NET Core WebApplication.

In appsettings.json add section below
```
"Loki": {
    "AppName": "example-app",
    "BatchPostingLimit": 2,
    "Labels": {
      "app": "example-app"
    },
    "PropertiesAsLabels": [
      "level"
    ],
    "SuppressProperties": [
      "EventId",
      "SourceContext",
      "RequestId",
      "RequestPath",
      "ConnectionId"
    ]
}
```

Your Program.cs should look like this

```
using System;
using Aerx.Serilog.Sinks.Loki.Extensions;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDirectToLokiLogging(builder.Configuration, c =>
{
    c.BaseAddress = new Uri("http://localhost:3100");
    c.Timeout = TimeSpan.FromSeconds(5);
});

var app = builder.Build();

app.Run();
```

Add new endpoint
```
app.MapPost("/test", async x =>
{
    var logger = x.RequestServices.GetRequiredService<ILogger<Program>>();
    logger.LogInformation("test log");
});
```

Then open grafana/loki explorer and search logs for label `{app="example-app"}`




---

There is a more advanced example in `samples` directory.


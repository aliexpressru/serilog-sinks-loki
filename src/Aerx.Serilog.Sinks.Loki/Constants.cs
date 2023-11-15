using System.Text;

namespace Aerx.Serilog.Sinks.Loki;

internal static class Constants
{
    internal const string Loki = nameof(Loki);
    
    internal const string EnableMetrics = nameof(EnableMetrics);

    internal const string TenantIdHeader = "X-Scope-OrgID";
    
    internal const string AppName = nameof(AppName);
    
    internal const string TenantId = nameof(TenantId);
    
    internal const string Labels = nameof(Labels);
    
    internal const string PropertiesAsLabels = nameof(PropertiesAsLabels);

    internal const string Level = "level";
    
    internal const string Trace = "trace";
    
    internal const string Debug = "debug";
    
    internal const string Info = "info";
    
    internal const string Warning = "warning";
    
    internal const string Error = "error";
    
    internal const string Critical = "critical";
    
    internal const string Unknown = "unknown";
    
    internal const string LokiPushUrl = "/loki/api/v1/push";

    internal const string JsonContentType = "application/json";

    internal const int DefaultWriteBufferCapacity = 256;

    internal const int DefaultBatchPostingLimit = 20;
    
    internal const int DefaultParallelismFactor = 1;
    
    internal const string RequestLogPropKey = "request";
    
    internal const string ResponseLogPropKey = "response";
    
    internal const string HttpClientRequestLogPropKey = "http_client_request";
    
    internal const string HttpClientResponseLogPropKey = "http_client_response";
    
    internal const string HeadersLogPropKey = "headers";
    
    internal const string BodyLogPropKey = "body";

    internal const string Target = "Target";
    
    internal const string Mesh = "Mesh";
    
    internal const string Timestamp = "Timestamp";

    internal const string TypeTagName = "$type";

    internal const string Gzip = "gzip";

    internal const long NanosecondsMultiplier = 1_000_000;
    
    internal static readonly Encoding Utf8WithoutBom = new UTF8Encoding(false);

    internal const double BytesInKb = 1024d;
        
    internal static readonly string[] ReservedKeywords =
    {
        "message",
        "message_template", 
        "renderings", 
        "exception"
    };
}
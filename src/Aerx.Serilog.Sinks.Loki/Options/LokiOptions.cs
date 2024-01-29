namespace Aerx.Serilog.Sinks.Loki.Options;

public class LokiOptions
{
    public string AppName { get; set; }
    
    public string TenantId { get; set; }
    
    public Dictionary<string, string> Labels { get; set; }

    public string[] PropertiesAsLabels { get; set; }
    
    public string[] SuppressProperties { get; set; }
    
    public int? BatchPostingLimit { get; set; }
    
    public int? ParallelismFactor { get; set; }

    public bool? UseInternalTimestamp { get; set; }

    public bool? LeavePropertiesIntact { get; set; }
    
    public Dictionary<string, string> SkipReqResWithHeaders { get; set; }
    
    public bool EnableMetrics { get; set; }
    
    public MetricsOptions Metrics { get; set; }

    public class MetricsOptions
    {
        public string LogsWriteFailCounterName { get; set; }
    
        public string LogsWriteSuccessCounterName { get; set; }
    
        public string LogsSizeKbCounterName { get; set; }
    }
}
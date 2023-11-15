namespace Aerx.Serilog.Sinks.Loki.Models;

public class LokiPushResponse
{
    public double ContentSizeInKb { get; set; }
    
    public HttpResponseMessage Response { get; set; }
}
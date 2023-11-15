namespace Aerx.Serilog.Sinks.Loki.Models;

public class LokiLabel
{
    public string Key { get; set; } = null!;
    
    public string Value { get; set; } = null!;
}
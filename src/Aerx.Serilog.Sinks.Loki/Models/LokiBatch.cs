namespace Aerx.Serilog.Sinks.Loki.Models;

public class LokiBatch
{
    public IList<LokiStream> Streams { get; } = new List<LokiStream>();

    public class LokiStream
    {
        public Dictionary<string, string> Stream { get; } = new();

        public IList<string[]> Values { get; } = new List<string[]>();
    }
}
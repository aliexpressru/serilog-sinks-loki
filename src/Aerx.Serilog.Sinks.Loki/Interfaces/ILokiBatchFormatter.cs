using Aerx.Serilog.Sinks.Loki.Models;
using Serilog.Formatting;

namespace Aerx.Serilog.Sinks.Loki.Interfaces;

public interface ILokiBatchFormatter
{
    LokiBatch Format(IReadOnlyCollection<LokiLogEvent> lokiLogEvents, ITextFormatter formatter);
}
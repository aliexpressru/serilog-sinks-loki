using System.Threading.Tasks.Dataflow;

namespace Aerx.Serilog.Sinks.Loki.Extensions;

internal static class DataflowExtensions
{
    public static void LinkToWithPropagation<T>(this ISourceBlock<T> source, ITargetBlock<T> target)
    {
        source.LinkTo(target, new DataflowLinkOptions
        {
            PropagateCompletion = true
        });
    }
}
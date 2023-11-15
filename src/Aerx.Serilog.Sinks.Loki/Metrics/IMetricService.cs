namespace Aerx.Serilog.Sinks.Loki.Metrics;

public interface IMetricService
{
    void ObserveOnLogsWriteFail(int failLogsCount);
    
    void ObserveOnLogsWriteSuccess(int successLogsCount);
    
    void ObserveOnLogsBatchSize(double kbSize);
}
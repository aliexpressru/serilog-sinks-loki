using Aerx.Serilog.Sinks.Loki.Options;
using Microsoft.Extensions.Options;
using Prometheus.Client;

namespace Aerx.Serilog.Sinks.Loki.Metrics;

internal class MetricService: IMetricService
{
    private readonly IMetricFamily<ICounter> _logsWriteFailCounter;
    private readonly IMetricFamily<ICounter> _logsWriteSuccessCounter;
    private readonly IMetricFamily<ICounter> _logsBatchSizeCounter;
    private readonly IOptions<LokiOptions> _lokiOptions;

    public MetricService(IMetricFactory metricFactory, IOptions<LokiOptions> lokiOptions)
    {
        _lokiOptions = lokiOptions;

        if (_lokiOptions.Value.EnableMetrics)
        {
            _logsWriteFailCounter = metricFactory.CreateCounter(
                name: _lokiOptions.Value.Metrics?.LogsWriteFailCounterName ?? "logs_write_fail_counter",
                help: "",
                labelNames: new[] { "app_name" });

            _logsWriteSuccessCounter = metricFactory.CreateCounter(
                _lokiOptions.Value.Metrics?.LogsWriteSuccessCounterName ?? "logs_write_success_counter",
                "",
                labelNames: new[] { "app_name" });

            _logsBatchSizeCounter = metricFactory.CreateCounter(
                _lokiOptions.Value.Metrics?.LogsSizeKbCounterName ?? "logs_size_kb_counter",
                "",
                labelNames: new[] { "app_name" });
        }
    }

    public void ObserveOnLogsWriteFail(int failLogsCount)
    {
        if (_lokiOptions.Value.EnableMetrics)
        {
            _logsWriteFailCounter
                .WithLabels(_lokiOptions.Value.AppName)
                .Inc(failLogsCount);
        }
    }
    
    public void ObserveOnLogsWriteSuccess(int successLogsCount)
    {
        if (_lokiOptions.Value.EnableMetrics)
        {
            _logsWriteSuccessCounter
                .WithLabels(_lokiOptions.Value.AppName)
                .Inc(successLogsCount);
        }
    }
    
    public void ObserveOnLogsBatchSize(double kbSize)
    {
        if (_lokiOptions.Value.EnableMetrics)
        {
            _logsBatchSizeCounter
                .WithLabels(_lokiOptions.Value.AppName)
                .Inc(kbSize);
        }
    }
}
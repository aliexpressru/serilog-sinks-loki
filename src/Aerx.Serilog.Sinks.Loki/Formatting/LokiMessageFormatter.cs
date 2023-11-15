using System.Text;
using System.Text.Json.Nodes;
using Aerx.Serilog.Sinks.Loki.Extensions;
using Aerx.Serilog.Sinks.Loki.Options;
using Microsoft.Extensions.Options;
using Serilog.Debugging;
using Serilog.Events;
using Serilog.Formatting;
using Serilog.Formatting.Json;
using Serilog.Parsing;

namespace Aerx.Serilog.Sinks.Loki.Formatting;

internal class LokiMessageFormatter : ITextFormatter
{
    private readonly JsonValueFormatter _valueFormatter;
    private readonly IOptions<LokiOptions> _lokiOptions;
    
    public LokiMessageFormatter(
        IOptions<LokiOptions> lokiOptions,
        JsonValueFormatter valueFormatter)
    {
        _lokiOptions = lokiOptions;
        _valueFormatter = valueFormatter;
    }

    public void Format(LogEvent logEvent, TextWriter output)
    {
        try
        {
            RewriteHttpProperty(logEvent, Constants.RequestLogPropKey);
            RewriteHttpProperty(logEvent, Constants.ResponseLogPropKey);
            RewriteHttpProperty(logEvent, Constants.HttpClientRequestLogPropKey);
            RewriteHttpProperty(logEvent, Constants.HttpClientResponseLogPropKey);
            
            foreach (var prop in _lokiOptions.Value.SuppressProperties ?? Array.Empty<string>())
            {
                logEvent.RemovePropertyIfPresent(prop);
            }
        }
        catch (Exception e)
        {
            SelfLog.WriteLine($"Properties rewriting execption: {e.Message}, {e.StackTrace}, {e.InnerException}");
        }

        FormatEvent(logEvent, output);
    }

    private void RewriteHttpProperty(LogEvent logEvent, string propertyName)
    {
        if (!logEvent.Properties.ContainsKey(propertyName))
        {
            return;
        }

        var property = logEvent.Properties[propertyName];
        var builder = new StringBuilder();
        using var writer = new StringWriter(builder);

        property.Render(writer, "l");
        var lines = builder.ToString().Split('\n');
        
        var first = lines[0];
        var body = lines[^1];
        var headers = lines[1..^1];

        logEvent.AddOrUpdateProperty(new LogEventProperty(propertyName, new ScalarValue(first)));
        logEvent.AddOrUpdateProperty(new LogEventProperty(Constants.HeadersLogPropKey, ConvertHeaders(headers)));

        if (!string.IsNullOrWhiteSpace(body))
        {
            var json = JsonNode.Parse(body)?.AsObject();
            if (json is not null)
            {
                var convertedJson = Convert(json);
                logEvent.AddOrUpdateProperty(new LogEventProperty(Constants.BodyLogPropKey, convertedJson));
            }
        }
    }

    private void FormatEvent(LogEvent logEvent, TextWriter output)
    {
        if (logEvent == null || output == null)
        {
            return;
        }
        
        output.Write("{\"message\":");
        JsonValueFormatter.WriteQuotedJsonString(
            logEvent.MessageTemplate.WithStringWriter((template, writer) => template.Render(logEvent.Properties, writer)), output);
        
        var tokensWithFormat = logEvent.MessageTemplate.Tokens
                .OfType<PropertyToken>()
                .Where(pt => pt.Format != null);

        var isRenderingsHeaderWritten = false;
        var delimiter = string.Empty;
        foreach (var r in tokensWithFormat)
        {
            if (!isRenderingsHeaderWritten)
            {
                output.Write(",\"renderings\":[");
                isRenderingsHeaderWritten = true;
            }
            
            output.Write(delimiter);
            delimiter = ",";

            JsonValueFormatter.WriteQuotedJsonString(
                r.WithStringWriter((prop, writer) => prop.Render(logEvent.Properties, writer)), output);
        }

        if (isRenderingsHeaderWritten)
        {
            output.Write(']');
        }

        if (logEvent.Exception != null)
        {
            output.Write(",\"exception\":");
            
            SerializeException(
                output,
                logEvent.Exception,
                1);
        }

        foreach (var (key, value) in logEvent.Properties)
        {
            if (string.IsNullOrEmpty(key) || value is null)
            {
                continue;
            }
            
            var snakeKey = key.ToSnake();
            var name = Constants.ReservedKeywords.Contains(snakeKey) ? $"_{snakeKey}" : snakeKey;
         
            output.Write(',');
            JsonValueFormatter.WriteQuotedJsonString(name, output);
            
            output.Write(':');
            try
            {
                _valueFormatter.Format(value, output);
            }
            catch (Exception e)
            {
                SelfLog.WriteLine($"ValueFormatterException, Key: {key}, ex: {e.Message}, {e.StackTrace}, {e.InnerException}");
            }
        }

        output.Write('}');
    }

    private void SerializeException(
        TextWriter output,
        Exception exception,
        int level)
    {
        if (level == 4)
        {
            JsonValueFormatter.WriteQuotedJsonString(exception.ToString(), output);

            return;
        }

        output.Write("{\"type\":");

        var exceptionTypeName = exception.GetType().FullName;
        
        JsonValueFormatter.WriteQuotedJsonString(exceptionTypeName, output);

        if (!string.IsNullOrWhiteSpace(exception.Message))
        {
            output.Write(",\"message\":");
            JsonValueFormatter.WriteQuotedJsonString(exception.Message, output);
        }

        if (!string.IsNullOrWhiteSpace(exception.StackTrace))
        {
            output.Write(",\"stackTrace\":");
            JsonValueFormatter.WriteQuotedJsonString(exception.StackTrace, output);
        }
        
        if (exception is AggregateException aggregateException)
        {
            output.Write(",\"innerExceptions\":[");
            var count = aggregateException.InnerExceptions.Count;
            for (var i = 0; i < count; i++)
            {
                var isLast = i == count - 1;
                SerializeException(
                    output,
                    aggregateException.InnerExceptions[i],
                    level + 1);
                if (!isLast)
                {
                    output.Write(',');
                }
            }

            output.Write("]");
        }
        else if (exception.InnerException != null)
        {
            output.Write(",\"innerException\":");
            SerializeException(
                output,
                exception.InnerException,
                level + 1);
        }

        output.Write('}');
    }

    private DictionaryValue ConvertHeaders(IEnumerable<string> headers) =>
        new(
            headers.Select(h => h.Split(':'))
                .ToDictionary(
                    h => new ScalarValue(h[0]),
                    h => (LogEventPropertyValue) 
                        new ScalarValue(string.Join(':', h[1..]).Replace("\\\"", ""))
                )
            );

    private static LogEventPropertyValue Convert(JsonNode node) =>
        node switch
        {
            JsonValue value => new ScalarValue(
                value.TryGetValue(out decimal d)
                    ? d
                    : value.TryGetValue(out bool b)
                        ? b
                        : value.ToString()),
            JsonArray array =>  new SequenceValue(array.Where(x => x != null).Select(Convert)),
            JsonObject obj => new StructureValue(obj.Where(x => !string.IsNullOrEmpty(x.Key) && x.Value != null)
                .Select(Convert)),
            _ => null
        };

    private static LogEventProperty Convert(KeyValuePair<string, JsonNode> pair) => new(pair.Key, Convert(pair.Value));
}

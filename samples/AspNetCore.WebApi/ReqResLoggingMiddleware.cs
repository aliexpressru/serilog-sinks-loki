using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Aerx.Serilog.Sinks.Loki.Extensions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.IO;
using Serilog.Context;

namespace WebApi;

public class RequestResponseLoggingMiddleware
{
    private static readonly Action<ILogger, Exception> RequestLogged =
        LoggerMessage.Define(LogLevel.Information, new EventId(101, nameof(RequestLogged)), "Request logged");
    
    private static readonly Action<ILogger, Exception> ResponseLogged =
        LoggerMessage.Define(LogLevel.Information, new EventId(102, nameof(ResponseLogged)), "Response logged");

    private static readonly Action<ILogger, Exception> ErrorResponseLogged =
        LoggerMessage.Define(LogLevel.Error, new EventId(103, nameof(ErrorResponseLogged)), "Response logged");
    
    private readonly RequestDelegate _next;
    private readonly ILogger _logger;
    private readonly RecyclableMemoryStreamManager _streamManager;

    public RequestResponseLoggingMiddleware(
        RequestDelegate next,
        ILogger<RequestResponseLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
        _streamManager = new RecyclableMemoryStreamManager();
    }
    
    public async Task InvokeAsync(HttpContext context)
    {
        if (!context.RequestResponseLoggingFilter())
        {
            await _next(context);
            return;
        }
        
        var requestStream = _streamManager.GetStream();
        await context.Request.Body.CopyToAsync(requestStream);
        requestStream.Position = 0;
        context.Request.Body = requestStream;
        
        var originalHttpResponseBody = context.Response.Body;
        var tmpStream = _streamManager.GetStream();
        context.Response.Body = tmpStream;

        try
        {
            var requestString = await FormatRequestString(context);

            using (LogContext.PushProperty("request", requestString))
            {
                RequestLogged(_logger, null);
            }

            await _next(context);
        
            var responseString = string.Empty;

            if (context.Response is { ContentLength: > 0, Body: { } } || context.Response.Body.CanRead && context.Response.Body.Length > 0)
            {
                responseString = await FormatResponseMessage(context);
            }
        
            if (IsErrorStatusCode(context.Response.StatusCode))
            {
                using (LogContext.PushProperty("response", responseString))
                {
                    ErrorResponseLogged(_logger, null);
                }
            }
            else
            {
                using (LogContext.PushProperty("response", responseString))
                {
                    ResponseLogged(_logger, null);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Couldn't log request");
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            await context.Response.WriteAsync(ex.ToString());
        }
        finally
        {
            context.Response.Body.Seek(0, SeekOrigin.Begin);
            var tempResponseBody = context.Response.Body;
            await tempResponseBody.CopyToAsync(originalHttpResponseBody);
            context.Response.Body = originalHttpResponseBody;
            await tempResponseBody.DisposeAsync();
        }
    }
    
    private static async Task<string> GetBodyString(Stream body)
    {
        body.Seek(0, SeekOrigin.Begin);
        var bodyString = await new StreamReader(body).ReadToEndAsync();
        body.Seek(0, SeekOrigin.Begin);
        return bodyString.Replace("\n", "").Replace(" ", "");
    }

    private static async Task<string> FormatRequestString(HttpContext context)
    {
        var bodyString = await GetBodyString(context.Request.Body);
        
        var sb = new StringBuilder(context.Request.Method);
        sb.Append(' ').Append(context.Request.Path).Append(' ').Append(context.Request.Protocol).Append('\n')
            .Append(string.Join("\n",
                context.Request.Headers
                    .Select(x => $"{x.Key}:{x.Value}")))
            .Append('\n')
            .Append(bodyString);
        
        var requestString = sb.ToString();
        return requestString;
    }

    private static async Task<string> FormatResponseMessage(HttpContext context)
    {
        var bodyString = await GetBodyString(context.Response.Body);
        
        var sb = new StringBuilder(context.Response.StatusCode.ToString());
        sb.Append(' ').Append((HttpStatusCode) context.Response.StatusCode).Append('\n')
            .Append(string.Join("\n",
                context.Response.Headers
                    .Select(x => $"{x.Key}:{x.Value}")))
            .Append('\n')
            .Append(bodyString);
        
        var responseString = sb.ToString();
        return responseString;
    }
    
    private static bool IsErrorStatusCode(int statusCode) => statusCode is >= 400 and <= 599;
}
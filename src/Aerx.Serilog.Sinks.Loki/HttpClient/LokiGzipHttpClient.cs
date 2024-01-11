using System.IO.Compression;
using System.Net.Http.Headers;
using Aerx.Serilog.Sinks.Loki.Interfaces;
using Aerx.Serilog.Sinks.Loki.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Aerx.Serilog.Sinks.Loki.HttpClient;

public class LokiGzipHttpClient : ILokiHttpClient
{
    private static readonly MediaTypeHeaderValue ContentType = MediaTypeHeaderValue.Parse(Constants.JsonContentType);
    private static readonly JsonSerializerSettings CamelCase = new()
    {
        ContractResolver = new DefaultContractResolver
        {
            NamingStrategy = new CamelCaseNamingStrategy()
        },
        NullValueHandling = NullValueHandling.Ignore
    };
    
    private readonly System.Net.Http.HttpClient _httpClient;
    
    public LokiGzipHttpClient(System.Net.Http.HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<LokiPushResponse> Push(LokiBatch batch)
    {
        using var contentStream = new MemoryStream();
        await using var contentWriter = new StreamWriter(contentStream, Constants.Utf8WithoutBom);

        await contentWriter.WriteAsync(JsonConvert.SerializeObject(batch, CamelCase)).ConfigureAwait(false);
        await contentWriter.FlushAsync().ConfigureAwait(false);

        var result = new LokiPushResponse
        {
            ContentSizeInKb = contentStream.Length / Constants.BytesInKb
        };
        
        contentStream.Position = 0;

        if (contentStream.Length > 0)
        {
            using var content = new StreamContent(contentStream);
            using var compressedContent = new CompressedContent(content, CompressionLevel.Optimal);
            compressedContent.Headers.ContentType = ContentType;

            var response = await _httpClient.SendAsync(new HttpRequestMessage(HttpMethod.Post, Constants.LokiPushUrl)
            {
                Content = compressedContent
            }).ConfigureAwait(false);

            result.Response = response;
        }

        return result;
    }

    public virtual ILokiHttpClient SetTenantId(string tenantId)
    {
        if (string.IsNullOrEmpty(tenantId) || _httpClient.DefaultRequestHeaders.Contains(Constants.TenantIdHeader))
        {
            return this;
        }

        _httpClient.DefaultRequestHeaders.Add(Constants.TenantIdHeader, tenantId);
        
        return this;
    }

    public void Dispose() => _httpClient.Dispose();
}
using System.IO.Compression;
using System.Net;

namespace Aerx.Serilog.Sinks.Loki.HttpClient;

internal class CompressedContent : HttpContent
{
    private readonly HttpContent _originalContent;
    private readonly CompressionLevel _compressionLevel;

    public CompressedContent(HttpContent content, CompressionLevel compressionLevel)
    {
        _originalContent = content ?? throw new ArgumentNullException(nameof(content));
        _compressionLevel = compressionLevel;
        
        foreach (var (key, value) in _originalContent.Headers)
        {
            Headers.TryAddWithoutValidation(key, value);
        }

        Headers.ContentEncoding.Add(Constants.Gzip);
    }

    protected override bool TryComputeLength(out long length)
    {
        length = -1;

        return false;
    }

    protected override async Task SerializeToStreamAsync(Stream stream, TransportContext context)
    {
        await using var compressedStream = new GZipStream(stream, _compressionLevel, leaveOpen: true);
        await _originalContent.CopyToAsync(compressedStream).ConfigureAwait(false);
    }
}
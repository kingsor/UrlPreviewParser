using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace UrlPreviewParser.Internal;

internal static class HtmlContentReader
{
    private const int SniffLength = 1024;

    private static readonly Regex MetaCharsetRegex = new Regex(
        @"<meta[^>]+charset\s*=\s*[""']?\s*([A-Za-z0-9_\-:.]+)",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    /// <summary>
    /// Reads at most <paramref name="maxLength"/> bytes of <paramref name="content"/> and decodes them,
    /// using (in order) the Content-Type charset, a byte order mark, a &lt;meta charset&gt; in the
    /// first 1024 bytes, and finally UTF-8.
    /// </summary>
    public static async Task<string> ReadAsStringAsync(
        HttpContent content,
        long maxLength,
        CancellationToken cancellationToken)
    {
        var bytes = await ReadBytesAsync(content, maxLength, cancellationToken).ConfigureAwait(false);
        return Decode(bytes, content.Headers.ContentType?.CharSet);
    }

    public static async Task<byte[]> ReadBytesAsync(
        HttpContent content,
        long maxLength,
        CancellationToken cancellationToken)
    {
        using var stream = await content.ReadAsStreamAsync().ConfigureAwait(false);
        using var buffer = new MemoryStream();
        var chunk = new byte[81920];

        while (buffer.Length < maxLength)
        {
            var toRead = (int)Math.Min(chunk.Length, maxLength - buffer.Length);
            var read = await stream.ReadAsync(chunk, 0, toRead, cancellationToken).ConfigureAwait(false);
            if (read == 0)
                break;

            buffer.Write(chunk, 0, read);
        }

        return buffer.ToArray();
    }

    public static string Decode(byte[] bytes, string? headerCharset)
    {
        var encoding = GetEncoding(headerCharset);
        var offset = 0;

        var bomEncoding = DetectBom(bytes, out var bomLength);
        if (bomEncoding is not null)
        {
            encoding ??= bomEncoding;
            offset = encoding.CodePage == bomEncoding.CodePage ? bomLength : 0;
        }

        if (encoding is null)
        {
            var sniffed = Encoding.ASCII.GetString(bytes, 0, Math.Min(bytes.Length, SniffLength));
            var match = MetaCharsetRegex.Match(sniffed);
            if (match.Success)
                encoding = GetEncoding(match.Groups[1].Value);
        }

        encoding ??= Encoding.UTF8;
        return encoding.GetString(bytes, offset, bytes.Length - offset);
    }

    private static Encoding? GetEncoding(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return null;

        try
        {
            return Encoding.GetEncoding(name!.Trim().Trim('"', '\''));
        }
        catch (ArgumentException)
        {
            // Unknown or unsupported charset (e.g. a legacy code page without CodePagesEncodingProvider).
            return null;
        }
    }

    private static Encoding? DetectBom(byte[] bytes, out int length)
    {
        length = 0;
        if (bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF)
        {
            length = 3;
            return Encoding.UTF8;
        }

        if (bytes.Length >= 2 && bytes[0] == 0xFF && bytes[1] == 0xFE)
        {
            length = 2;
            return Encoding.Unicode;
        }

        if (bytes.Length >= 2 && bytes[0] == 0xFE && bytes[1] == 0xFF)
        {
            length = 2;
            return Encoding.BigEndianUnicode;
        }

        return null;
    }
}

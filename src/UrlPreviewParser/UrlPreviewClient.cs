using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

using UrlPreviewParser.Internal;
using UrlPreviewParser.Models;

namespace UrlPreviewParser;

/// <summary>
/// Client for extracting link preview metadata (OpenGraph, Twitter Card, oEmbed) from URLs.
/// </summary>
public class UrlPreviewClient : IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly bool _ownsHttpClient;
    private readonly UrlPreviewOptions _options;

    /// <summary>
    /// Creates a new <see cref="UrlPreviewClient"/> with an internal <see cref="HttpClient"/> and default options.
    /// </summary>
    public UrlPreviewClient()
        : this(new UrlPreviewOptions())
    {
    }

    /// <summary>
    /// Creates a new <see cref="UrlPreviewClient"/> with an internal <see cref="HttpClient"/>.
    /// </summary>
    /// <param name="options">Fetching and parsing options.</param>
    public UrlPreviewClient(UrlPreviewOptions options)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _httpClient = new HttpClient();
        _httpClient.DefaultRequestHeaders.Add(
            "User-Agent",
            "Mozilla/5.0 (compatible; UrlPreviewParser/1.0; +https://github.com/kingsor/UrlPreviewParser)");
        _ownsHttpClient = true;
    }

    /// <summary>
    /// Creates a new <see cref="UrlPreviewClient"/> using the provided <see cref="HttpClient"/> and default options.
    /// The caller is responsible for disposing the <see cref="HttpClient"/>.
    /// </summary>
    public UrlPreviewClient(HttpClient httpClient)
        : this(httpClient, new UrlPreviewOptions())
    {
    }

    /// <summary>
    /// Creates a new <see cref="UrlPreviewClient"/> using the provided <see cref="HttpClient"/>.
    /// The caller is responsible for disposing the <see cref="HttpClient"/>.
    /// </summary>
    /// <param name="httpClient">The HTTP client used for all requests.</param>
    /// <param name="options">Fetching and parsing options.</param>
    public UrlPreviewClient(HttpClient httpClient, UrlPreviewOptions options)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _ownsHttpClient = false;
    }

    /// <summary>
    /// Fetches the given URL and extracts its link preview metadata.
    /// </summary>
    /// <remarks>
    /// At most <see cref="UrlPreviewOptions.MaxContentLength"/> bytes are read. The body is decoded using the
    /// Content-Type charset, a byte order mark or a &lt;meta charset&gt;, falling back to UTF-8. Legacy code pages
    /// (e.g. windows-1252) require <c>Encoding.RegisterProvider(CodePagesEncodingProvider.Instance)</c> on .NET.
    /// </remarks>
    /// <param name="url">The URL to fetch and parse.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A <see cref="UrlPreviewResult"/> with all extracted metadata.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="url"/> is null or empty.</exception>
    /// <exception cref="HttpRequestException">Thrown when the response status code is not successful.</exception>
    /// <exception cref="TimeoutException">Thrown when <see cref="UrlPreviewOptions.Timeout"/> elapses.</exception>
    public async Task<UrlPreviewResult> GetPreviewAsync(
        string url,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(url))
            throw new ArgumentException("URL is required.", nameof(url));

        using var timeoutSource = CreateTimeoutSource(cancellationToken);
        var token = timeoutSource?.Token ?? cancellationToken;

        try
        {
            using var response = await _httpClient
                .GetAsync(url, HttpCompletionOption.ResponseHeadersRead, token)
                .ConfigureAwait(false);
            response.EnsureSuccessStatusCode();

            var html = await HtmlContentReader
                .ReadAsStringAsync(response.Content, _options.MaxContentLength, token)
                .ConfigureAwait(false);
            var finalUrl = response.RequestMessage?.RequestUri?.AbsoluteUri ?? url;

            var result = await MetadataParser
                .ParseAsync(url, html, finalUrl, _options.ResolveRelativeUrls)
                .ConfigureAwait(false);

            if (_options.FetchOembed)
            {
                await OembedFetcher
                    .FetchAsync(_httpClient, result, _options.MaxContentLength, token)
                    .ConfigureAwait(false);
            }

            return result;
        }
        catch (OperationCanceledException) when (timeoutSource is not null
            && timeoutSource.IsCancellationRequested
            && !cancellationToken.IsCancellationRequested)
        {
            throw new TimeoutException($"Fetching '{url}' did not complete within {_options.Timeout}.");
        }
    }

    /// <summary>
    /// Parses link preview metadata from an HTML string without making any HTTP requests.
    /// oEmbed endpoint data will <b>not</b> be fetched.
    /// </summary>
    /// <param name="url">The URL the HTML was fetched from (used to populate <see cref="UrlPreviewResult.Url"/>
    /// and as the base for relative URLs).</param>
    /// <param name="html">Raw HTML content to parse.</param>
    /// <returns>A <see cref="UrlPreviewResult"/> with all extracted metadata.</returns>
    public Task<UrlPreviewResult> ParseAsync(string url, string html)
    {
        if (string.IsNullOrWhiteSpace(url))
            throw new ArgumentException("URL is required.", nameof(url));
        if (html is null)
            throw new ArgumentNullException(nameof(html));

        return MetadataParser.ParseAsync(url, html, url, _options.ResolveRelativeUrls);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_ownsHttpClient)
            _httpClient.Dispose();
    }

    private CancellationTokenSource? CreateTimeoutSource(CancellationToken cancellationToken)
    {
        if (_options.Timeout is not { } timeout || timeout <= TimeSpan.Zero)
            return null;

        var source = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        source.CancelAfter(timeout);
        return source;
    }
}

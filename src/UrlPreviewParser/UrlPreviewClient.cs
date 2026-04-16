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

    /// <summary>
    /// Creates a new <see cref="UrlPreviewClient"/> with an internal <see cref="HttpClient"/>.
    /// </summary>
    public UrlPreviewClient()
    {
        _httpClient = new HttpClient();
        _httpClient.DefaultRequestHeaders.Add(
            "User-Agent",
            "Mozilla/5.0 (compatible; UrlPreviewParser/1.0; +https://github.com/kingsor/UrlPreviewParser)");
        _ownsHttpClient = true;
    }

    /// <summary>
    /// Creates a new <see cref="UrlPreviewClient"/> using the provided <see cref="HttpClient"/>.
    /// The caller is responsible for disposing the <see cref="HttpClient"/>.
    /// </summary>
    public UrlPreviewClient(HttpClient httpClient)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _ownsHttpClient = false;
    }

    /// <summary>
    /// Fetches the given URL and extracts its link preview metadata.
    /// </summary>
    /// <param name="url">The URL to fetch and parse.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A <see cref="UrlPreviewResult"/> with all extracted metadata.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="url"/> is null or empty.</exception>
    public async Task<UrlPreviewResult> GetPreviewAsync(
        string url,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(url))
            throw new ArgumentException("URL is required.", nameof(url));

        var response = await _httpClient
            .GetAsync(url, cancellationToken)
            .ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        var html = await response.Content
            .ReadAsStringAsync()
            .ConfigureAwait(false);

        var result = await MetadataParser.ParseAsync(url, html).ConfigureAwait(false);
        await OembedFetcher.FetchAsync(_httpClient, result, cancellationToken).ConfigureAwait(false);

        return result;
    }

    /// <summary>
    /// Parses link preview metadata from an HTML string without making any HTTP requests.
    /// oEmbed endpoint data will <b>not</b> be fetched.
    /// </summary>
    /// <param name="url">The URL the HTML was fetched from (used to populate <see cref="UrlPreviewResult.Url"/>).</param>
    /// <param name="html">Raw HTML content to parse.</param>
    /// <returns>A <see cref="UrlPreviewResult"/> with all extracted metadata.</returns>
    public Task<UrlPreviewResult> ParseAsync(string url, string html)
    {
        if (string.IsNullOrWhiteSpace(url))
            throw new ArgumentException("URL is required.", nameof(url));
        if (html is null)
            throw new ArgumentNullException(nameof(html));

        return MetadataParser.ParseAsync(url, html);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_ownsHttpClient)
            _httpClient.Dispose();
    }
}

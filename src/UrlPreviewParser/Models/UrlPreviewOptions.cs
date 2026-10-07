using System;

namespace UrlPreviewParser.Models;

/// <summary>
/// Options that control how <see cref="UrlPreviewClient"/> fetches and parses pages.
/// </summary>
public class UrlPreviewOptions
{
    /// <summary>
    /// Maximum time allowed for fetching the page (and the oEmbed endpoint).
    /// When exceeded a <see cref="TimeoutException"/> is thrown.
    /// Set to <see langword="null" /> to rely only on the <see cref="System.Net.Http.HttpClient.Timeout"/>.
    /// Defaults to 15 seconds.
    /// </summary>
    public TimeSpan? Timeout { get; set; } = TimeSpan.FromSeconds(15);

    /// <summary>
    /// Maximum number of bytes read from the response body. Content beyond this limit is ignored:
    /// link preview metadata lives in the <c>&lt;head&gt;</c>, so truncation is normally harmless.
    /// Defaults to 5 MB.
    /// </summary>
    public long MaxContentLength { get; set; } = 5 * 1024 * 1024;

    /// <summary>
    /// When <see langword="true" /> (default), <see cref="UrlPreviewClient.GetPreviewAsync"/> fetches the
    /// discovered oEmbed JSON endpoint and stores its body in <see cref="OembedData.Body"/>.
    /// </summary>
    public bool FetchOembed { get; set; } = true;

    /// <summary>
    /// When <see langword="true" /> (default), relative URLs found in URL-valued properties
    /// (e.g. <c>og:image</c>, <c>twitter:image</c>, canonical, favicon, oEmbed endpoints) are resolved
    /// to absolute URLs, honoring <c>&lt;base href&gt;</c> and the final URL after redirects.
    /// <see cref="UrlPreviewResult.Head"/> always contains the raw, unresolved attribute values.
    /// </summary>
    public bool ResolveRelativeUrls { get; set; } = true;
}

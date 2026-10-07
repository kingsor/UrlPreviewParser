using System.Collections.Generic;

namespace UrlPreviewParser.Models;

/// <summary>
/// Complete metadata extracted from a URL, including OpenGraph, Twitter Card, and oEmbed data.
/// </summary>
public class UrlPreviewResult
{
    /// <summary>OpenGraph metadata (og:* tags).</summary>
    public OpenGraphData OpenGraph { get; set; } = new OpenGraphData();

    /// <summary>Twitter Card metadata (twitter:* tags).</summary>
    public TwitterCardData TwitterCard { get; set; } = new TwitterCardData();

    /// <summary>oEmbed discovery data and response.</summary>
    public OembedData Oembed { get; set; } = new OembedData();

    /// <summary>Basic page metadata (title, description, canonical, favicon, language, …).</summary>
    public TagsData Tags { get; set; } = new TagsData();

    /// <summary>
    /// All child elements of &lt;head&gt; as attribute dictionaries.
    /// Each entry maps attribute names to their raw (unresolved) values for one head element.
    /// </summary>
    public List<Dictionary<string, string?>> Head { get; set; } = new List<Dictionary<string, string?>>();

    /// <summary>The input URL that was scraped.</summary>
    public string Url { get; set; } = string.Empty;

    /// <summary>
    /// The URL of the page actually parsed, after following redirects.
    /// Equal to <see cref="Url"/> when no redirect occurred or when parsing offline.
    /// </summary>
    public string? FinalUrl { get; set; }

    /// <summary>Raw HTML body of the fetched page.</summary>
    public string? Body { get; set; }
}

using System.Collections.Generic;

namespace UrlPreviewParser.Models;

/// <summary>
/// Complete metadata extracted from a URL, including OpenGraph, Twitter Card, and oEmbed data.
/// </summary>
public class UrlPreviewResult
{
    /// <summary>OpenGraph metadata (og:* tags).</summary>
    public OpenGraphData OpenGraph { get; } = new OpenGraphData();

    /// <summary>Twitter Card metadata (twitter:* tags).</summary>
    public TwitterCardData TwitterCard { get; } = new TwitterCardData();

    /// <summary>oEmbed discovery data and response.</summary>
    public OembedData Oembed { get; } = new OembedData();

    /// <summary>Basic page metadata (title, images).</summary>
    public TagsData Tags { get; } = new TagsData();

    /// <summary>
    /// All child elements of &lt;head&gt; as attribute dictionaries.
    /// Each entry maps attribute names to values for one head element.
    /// </summary>
    public List<Dictionary<string, string?>> Head { get; } = new List<Dictionary<string, string?>>();

    /// <summary>The input URL that was scraped.</summary>
    public string Url { get; set; } = string.Empty;

    /// <summary>Raw HTML body of the fetched page.</summary>
    public string? Body { get; set; }
}

using System.Collections.Generic;

namespace UrlPreviewParser.Models;

/// <summary>
/// oEmbed discovery data and response body fetched from a webpage.
/// </summary>
public class OembedData
{
    /// <summary>
    /// Discovered oEmbed formats (e.g., "json", "xml").
    /// </summary>
    public List<string> Formats { get; } = new List<string>();

    /// <summary>
    /// URL of the oEmbed JSON endpoint (from &lt;link rel="alternate" type="application/json+oembed"&gt;).
    /// </summary>
    public string? Json { get; set; }

    /// <summary>
    /// URL of the oEmbed XML endpoint (from &lt;link rel="alternate" type="text/xml+oembed"&gt;).
    /// </summary>
    public string? Xml { get; set; }

    /// <summary>
    /// Raw response body from the oEmbed endpoint.
    /// </summary>
    public string? Body { get; set; }

    /// <summary>
    /// Error message if the oEmbed endpoint fetch failed.
    /// </summary>
    public string? Error { get; set; }
}

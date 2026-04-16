using System.Collections.Generic;

namespace UrlPreviewParser.Models;

/// <summary>
/// OpenGraph metadata extracted from a webpage.
/// Keys are property names (e.g., "og:title", "og:description").
/// </summary>
public class OpenGraphData : Dictionary<string, string>
{
    /// <summary>og:title</summary>
    public string? Title => TryGetValue("og:title", out var v) ? v : null;

    /// <summary>og:description</summary>
    public string? Description => TryGetValue("og:description", out var v) ? v : null;

    /// <summary>og:image</summary>
    public string? Image => TryGetValue("og:image", out var v) ? v : null;

    /// <summary>og:url</summary>
    public string? Url => TryGetValue("og:url", out var v) ? v : null;

    /// <summary>og:type</summary>
    public string? Type => TryGetValue("og:type", out var v) ? v : null;

    /// <summary>og:site_name</summary>
    public string? SiteName => TryGetValue("og:site_name", out var v) ? v : null;
}

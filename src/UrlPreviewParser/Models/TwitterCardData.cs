using System.Collections.Generic;

namespace UrlPreviewParser.Models;

/// <summary>
/// Twitter Card metadata extracted from a webpage.
/// Keys are property names (e.g., "twitter:title", "twitter:card").
/// </summary>
public class TwitterCardData : Dictionary<string, string>
{
    /// <summary>twitter:card (summary, summary_large_image, app, player)</summary>
    public string? Card => TryGetValue("twitter:card", out var v) ? v : null;

    /// <summary>twitter:title</summary>
    public string? Title => TryGetValue("twitter:title", out var v) ? v : null;

    /// <summary>twitter:description</summary>
    public string? Description => TryGetValue("twitter:description", out var v) ? v : null;

    /// <summary>twitter:image or twitter:image:src</summary>
    public string? Image =>
        TryGetValue("twitter:image", out var v) ? v :
        TryGetValue("twitter:image:src", out v) ? v : null;

    /// <summary>twitter:url</summary>
    public string? Url => TryGetValue("twitter:url", out var v) ? v : null;

    /// <summary>twitter:site (@username of website)</summary>
    public string? Site => TryGetValue("twitter:site", out var v) ? v : null;

    /// <summary>twitter:creator (@username of content creator)</summary>
    public string? Creator => TryGetValue("twitter:creator", out var v) ? v : null;
}

using System.Collections.Generic;

namespace UrlPreviewParser.Models;

/// <summary>
/// Basic HTML metadata extracted from a webpage.
/// </summary>
public class TagsData
{
    /// <summary>
    /// Content of the HTML &lt;title&gt; tag.
    /// </summary>
    public string? Title { get; set; }

    /// <summary>
    /// Image URLs found in the page (from og:image, twitter:image, or &lt;img&gt; tags).
    /// </summary>
    public List<string> Images { get; } = new List<string>();
}

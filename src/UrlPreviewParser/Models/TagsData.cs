using System.Collections.Generic;

namespace UrlPreviewParser.Models;

/// <summary>
/// Basic HTML metadata extracted from a webpage.
/// </summary>
public class TagsData
{
    /// <summary>
    /// Content of the first HTML &lt;title&gt; tag.
    /// </summary>
    public string? Title { get; set; }

    /// <summary>
    /// Image URLs found in the page (from og:image, twitter:image, or &lt;img&gt; tags).
    /// </summary>
    public List<string> Images { get; set; } = new List<string>();

    /// <summary>
    /// Content of &lt;meta name="description"&gt;.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Href of &lt;link rel="canonical"&gt;.
    /// </summary>
    public string? Canonical { get; set; }

    /// <summary>
    /// Preferred favicon: the first &lt;link rel="icon"&gt; (including <c>shortcut icon</c>), otherwise the first
    /// <c>apple-touch-icon</c>, <c>apple-touch-icon-precomposed</c>, <c>mask-icon</c> or <c>fluid-icon</c>.
    /// The <c>/favicon.ico</c> fallback is not probed.
    /// </summary>
    public string? Favicon { get; set; }

    /// <summary>
    /// Value of the <c>lang</c> attribute of the &lt;html&gt; element.
    /// </summary>
    public string? Language { get; set; }

    /// <summary>
    /// Character set declared by &lt;meta charset&gt; or &lt;meta http-equiv="Content-Type"&gt;.
    /// </summary>
    public string? Charset { get; set; }

    /// <summary>
    /// Content of &lt;meta name="viewport"&gt;.
    /// </summary>
    public string? Viewport { get; set; }

    /// <summary>
    /// Content of &lt;meta name="robots"&gt;.
    /// </summary>
    public string? Robots { get; set; }

    /// <summary>
    /// Content of &lt;meta name="theme-color"&gt;.
    /// </summary>
    public string? ThemeColor { get; set; }

    /// <summary>
    /// Content of &lt;meta name="generator"&gt;.
    /// </summary>
    public string? Generator { get; set; }
}

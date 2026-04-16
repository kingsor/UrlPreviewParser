using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AngleSharp.Html.Parser;
using UrlPreviewParser.Models;

namespace UrlPreviewParser.Internal;

internal static class MetadataParser
{
    public static async Task<UrlPreviewResult> ParseAsync(string url, string html)
    {
        var result = new UrlPreviewResult { Url = url, Body = html };

        var parser = new HtmlParser();
        using var document = await parser.ParseDocumentAsync(html).ConfigureAwait(false);

        var head = document.Head;
        if (head is null)
            return result;

        foreach (var element in head.Children)
        {
            // Collect all attributes of this head element
            var attrs = new Dictionary<string, string?>();
            foreach (var attr in element.Attributes)
                attrs[attr.Name] = attr.Value;
            result.Head.Add(attrs);

            var tagName = element.TagName.ToUpperInvariant();

            switch (tagName)
            {
                case "META":
                {
                    var property = element.GetAttribute("property");
                    var name = element.GetAttribute("name");
                    var content = element.GetAttribute("content") ?? string.Empty;

                    if (property != null && property.StartsWith("og:", StringComparison.OrdinalIgnoreCase))
                        result.OpenGraph[property.ToLowerInvariant()] = content;

                    if (name != null && name.StartsWith("twitter:", StringComparison.OrdinalIgnoreCase))
                        result.TwitterCard[name.ToLowerInvariant()] = content;
                    break;
                }

                case "TITLE":
                    result.Tags.Title = element.TextContent?.Trim();
                    break;

                case "LINK":
                {
                    var rel = element.GetAttribute("rel");
                    var type = element.GetAttribute("type");
                    var href = element.GetAttribute("href");

                    if (rel == "alternate" && href != null)
                    {
                        if (string.Equals(type, "application/json+oembed", StringComparison.OrdinalIgnoreCase))
                        {
                            result.Oembed.Json = href;
                            result.Oembed.Formats.Add("json");
                        }
                        else if (string.Equals(type, "text/xml+oembed", StringComparison.OrdinalIgnoreCase))
                        {
                            result.Oembed.Xml = href;
                            result.Oembed.Formats.Add("xml");
                        }
                    }
                    break;
                }
            }
        }

        // Populate Tags.Images from OG or Twitter image
        var image = result.OpenGraph.Image ?? result.TwitterCard.Image;
        if (image != null)
            result.Tags.Images.Add(image);

        return result;
    }
}

using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using AngleSharp.Dom;
using AngleSharp.Html.Parser;
using UrlPreviewParser.Models;

namespace UrlPreviewParser.Internal;

internal static class MetadataParser
{
    private static readonly char[] RelSeparators = [' ', '\t', '\n', '\r', '\f'];

    private static readonly string[] SecondaryIconRels =
    [
        "apple-touch-icon", "apple-touch-icon-precomposed", "mask-icon", "fluid-icon",
    ];

    private static readonly Regex ContentTypeCharsetRegex = new Regex(
        @"charset\s*=\s*[""']?([^\s;""']+)",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    /// <param name="url">The requested URL.</param>
    /// <param name="html">The HTML to parse.</param>
    /// <param name="finalUrl">The URL after redirects (base for relative URLs); <see langword="null" /> to use <paramref name="url"/>.</param>
    /// <param name="resolveRelativeUrls">Whether URL-valued properties are made absolute.</param>
    public static async Task<UrlPreviewResult> ParseAsync(
        string url,
        string html,
        string? finalUrl = null,
        bool resolveRelativeUrls = true)
    {
        var result = new UrlPreviewResult { Url = url, FinalUrl = finalUrl ?? url, Body = html };

        var parser = new HtmlParser();
        using var document = await parser.ParseDocumentAsync(html).ConfigureAwait(false);

        result.Tags.Language = NullIfEmpty(document.DocumentElement?.GetAttribute("lang"));

        var head = document.Head;
        if (head is null)
            return result;

        string? secondaryIcon = null;

        foreach (var element in head.Children)
        {
            // Collect all attributes of this head element
            var attrs = new Dictionary<string, string?>();

            attrs["tag-name"] = element.LocalName;

            foreach (var attr in element.Attributes)
                attrs[attr.Name] = attr.Value;

            if (element.Attributes.Length > 0)
            {
                result.Head.Add(attrs);
            }

            switch (element.LocalName)
            {
                case "meta":
                    ParseMeta(element, result);
                    break;

                case "title":
                    result.Tags.Title ??= element.TextContent?.Trim();
                    break;

                case "link":
                    ParseLink(element, result, ref secondaryIcon);
                    break;
            }
        }

        result.Tags.Favicon ??= secondaryIcon;

        if (resolveRelativeUrls)
            ResolveUrls(result, document);

        // Populate Tags.Images from OG or Twitter image
        var image = result.OpenGraph.Image ?? result.TwitterCard.Image;
        if (image is not null)
            result.Tags.Images.Add(image);

        return result;
    }

    private static void ParseMeta(IElement element, UrlPreviewResult result)
    {
        var property = element.GetAttribute("property");
        var name = element.GetAttribute("name");
        var content = element.GetAttribute("content") ?? string.Empty;

        // Sites mix up "property" and "name": accept both for og:* and twitter:* (first occurrence wins).
        AddPrefixed(result.OpenGraph, "og:", property, content);
        AddPrefixed(result.OpenGraph, "og:", name, content);
        AddPrefixed(result.TwitterCard, "twitter:", name, content);
        AddPrefixed(result.TwitterCard, "twitter:", property, content);

        var charset = element.GetAttribute("charset");
        if (!string.IsNullOrWhiteSpace(charset))
            result.Tags.Charset ??= charset!.Trim();

        var httpEquiv = element.GetAttribute("http-equiv");
        if (string.Equals(httpEquiv, "content-type", StringComparison.OrdinalIgnoreCase))
        {
            var match = ContentTypeCharsetRegex.Match(content);
            if (match.Success)
                result.Tags.Charset ??= match.Groups[1].Value;
        }

        if (name is null || content.Length == 0)
            return;

        switch (name.Trim().ToLowerInvariant())
        {
            case "description":
                result.Tags.Description ??= content;
                break;
            case "viewport":
                result.Tags.Viewport ??= content;
                break;
            case "robots":
                result.Tags.Robots ??= content;
                break;
            case "theme-color":
                result.Tags.ThemeColor ??= content;
                break;
            case "generator":
                result.Tags.Generator ??= content;
                break;
        }
    }

    private static void ParseLink(IElement element, UrlPreviewResult result, ref string? secondaryIcon)
    {
        var href = element.GetAttribute("href");
        if (string.IsNullOrWhiteSpace(href))
            return;

        var type = element.GetAttribute("type");
        var rels = (element.GetAttribute("rel") ?? string.Empty)
            .ToLowerInvariant()
            .Split(RelSeparators, StringSplitOptions.RemoveEmptyEntries);

        foreach (var rel in rels)
        {
            switch (rel)
            {
                case "alternate":
                    if (string.Equals(type, "application/json+oembed", StringComparison.OrdinalIgnoreCase)
                        && result.Oembed.Json is null)
                    {
                        result.Oembed.Json = href;
                        result.Oembed.Formats.Add("json");
                    }
                    else if (string.Equals(type, "text/xml+oembed", StringComparison.OrdinalIgnoreCase)
                        && result.Oembed.Xml is null)
                    {
                        result.Oembed.Xml = href;
                        result.Oembed.Formats.Add("xml");
                    }
                    break;

                case "canonical":
                    result.Tags.Canonical ??= href;
                    break;

                case "icon":
                    result.Tags.Favicon ??= href;
                    break;

                default:
                    if (Array.IndexOf(SecondaryIconRels, rel) >= 0)
                        secondaryIcon ??= href;
                    break;
            }
        }
    }

    private static void ResolveUrls(UrlPreviewResult result, IDocument document)
    {
        var baseHref = document.QuerySelector("base[href]")?.GetAttribute("href");
        var baseUri = UrlResolver.GetBaseUri(result.FinalUrl ?? result.Url, baseHref);
        if (baseUri is null)
            return;

        UrlResolver.ResolveKeys(result.OpenGraph, UrlResolver.OpenGraphUrlKeys, baseUri);
        UrlResolver.ResolveKeys(result.TwitterCard, UrlResolver.TwitterUrlKeys, baseUri);

        if (result.Oembed.Json is not null)
            result.Oembed.Json = UrlResolver.Resolve(baseUri, result.Oembed.Json);
        if (result.Oembed.Xml is not null)
            result.Oembed.Xml = UrlResolver.Resolve(baseUri, result.Oembed.Xml);
        if (result.Tags.Canonical is not null)
            result.Tags.Canonical = UrlResolver.Resolve(baseUri, result.Tags.Canonical);
        if (result.Tags.Favicon is not null)
            result.Tags.Favicon = UrlResolver.Resolve(baseUri, result.Tags.Favicon);
    }

    private static void AddPrefixed(Dictionary<string, string> target, string prefix, string? key, string content)
    {
        if (key is null || !key.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            return;

        var normalized = key.Trim().ToLowerInvariant();
        if (!target.ContainsKey(normalized))
            target[normalized] = content;
    }

    private static string? NullIfEmpty(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value!.Trim();
}

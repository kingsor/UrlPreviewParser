using System;
using System.Collections.Generic;
using System.Text;

namespace UrlPreviewParser.Tests;

public class MetadataParserTests
{
    private static Task<Models.UrlPreviewResult> Parse(string html) =>
            new UrlPreviewClient().ParseAsync("https://example.com", html);

    [Fact]
    public async Task Parse_ExtractsOpenGraphTags()
    {
        const string html = """
                <html>
                <head>
                    <meta property="og:title" content="OG Title" />
                    <meta property="og:description" content="OG Description" />
                    <meta property="og:image" content="https://example.com/image.jpg" />
                    <meta property="og:url" content="https://example.com" />
                    <meta property="og:type" content="website" />
                    <meta property="og:site_name" content="Example" />
                </head>
                <body></body>
                </html>
                """;

        var result = await Parse(html);

        Assert.Equal("OG Title", result.OpenGraph.Title);
        Assert.Equal("OG Description", result.OpenGraph.Description);
        Assert.Equal("https://example.com/image.jpg", result.OpenGraph.Image);
        Assert.Equal("https://example.com", result.OpenGraph.Url);
        Assert.Equal("website", result.OpenGraph.Type);
        Assert.Equal("Example", result.OpenGraph.SiteName);
    }

    [Fact]
    public async Task Parse_ExtractsTwitterCardTags()
    {
        const string html = """
                <html>
                <head>
                    <meta name="twitter:card" content="summary_large_image" />
                    <meta name="twitter:title" content="Twitter Title" />
                    <meta name="twitter:description" content="Twitter Desc" />
                    <meta name="twitter:image" content="https://example.com/tw.jpg" />
                    <meta name="twitter:site" content="@example" />
                </head>
                <body></body>
                </html>
                """;

        var result = await Parse(html);

        Assert.Equal("summary_large_image", result.TwitterCard.Card);
        Assert.Equal("Twitter Title", result.TwitterCard.Title);
        Assert.Equal("Twitter Desc", result.TwitterCard.Description);
        Assert.Equal("https://example.com/tw.jpg", result.TwitterCard.Image);
        Assert.Equal("@example", result.TwitterCard.Site);
    }

    [Fact]
    public async Task Parse_ExtractsTitleTag()
    {
        const string html = """
                <html>
                <head><title>  Page Title  </title></head>
                <body></body>
                </html>
                """;

        var result = await Parse(html);

        Assert.Equal("Page Title", result.Tags.Title);
    }

    [Fact]
    public async Task Parse_DiscoversOembedJsonEndpoint()
    {
        const string html = """
                <html>
                <head>
                    <link rel="alternate" type="application/json+oembed"
                          href="https://example.com/oembed?url=https://example.com/post" />
                </head>
                <body></body>
                </html>
                """;

        var result = await Parse(html);

        Assert.Contains("json", result.Oembed.Formats);
        Assert.Equal("https://example.com/oembed?url=https://example.com/post", result.Oembed.Json);
    }

    [Fact]
    public async Task Parse_DiscoversOembedXmlEndpoint()
    {
        const string html = """
                <html>
                <head>
                    <link rel="alternate" type="text/xml+oembed"
                          href="https://example.com/oembed?format=xml" />
                </head>
                <body></body>
                </html>
                """;

        var result = await Parse(html);

        Assert.Contains("xml", result.Oembed.Formats);
        Assert.Equal("https://example.com/oembed?format=xml", result.Oembed.Xml);
    }

    [Fact]
    public async Task Parse_CollectsHeadElements()
    {
        const string html = """
                <html>
                <head>
                    <meta charset="UTF-8" />
                    <meta property="og:title" content="Title" />
                    <title>Page</title>
                </head>
                <body></body>
                </html>
                """;

        var result = await Parse(html);

        Assert.Equal(2, result.Head.Count);
        Assert.True(result.Head[0].ContainsKey("charset"));
    }

    [Fact]
    public async Task Parse_PopulatesUrlInResult()
    {
        var result = await new UrlPreviewClient()
            .ParseAsync("https://example.com/page", "<html><head></head><body></body></html>");

        Assert.Equal("https://example.com/page", result.Url);
    }

    [Fact]
    public async Task Parse_ReturnsEmptyCollectionsWhenNoMetadata()
    {
        var result = await Parse("<html><head></head><body></body></html>");

        Assert.Empty(result.OpenGraph);
        Assert.Empty(result.TwitterCard);
        Assert.Empty(result.Oembed.Formats);
        Assert.Null(result.Tags.Title);
    }

    [Fact]
    public async Task Parse_PopulatesTagsImagesFromOgImage()
    {
        const string html = """
                <html>
                <head>
                    <meta property="og:image" content="https://example.com/cover.jpg" />
                </head>
                <body></body>
                </html>
                """;

        var result = await Parse(html);

        Assert.Single(result.Tags.Images);
        Assert.Equal("https://example.com/cover.jpg", result.Tags.Images[0]);
    }

    [Fact]
    public async Task Parse_KeysAreLowercased()
    {
        const string html = """
                <html>
                <head>
                    <meta property="OG:TITLE" content="Upper Title" />
                    <meta name="Twitter:Card" content="summary" />
                </head>
                <body></body>
                </html>
                """;

        var result = await Parse(html);

        Assert.Equal("Upper Title", result.OpenGraph.Title);
        Assert.Equal("summary", result.TwitterCard.Card);
    }

    [Fact]
    public async Task Parse_StoresRawHtmlInBody()
    {
        const string html = "<html><head></head><body>hello</body></html>";

        var result = await Parse(html);

        Assert.Equal(html, result.Body);
    }

    [Fact]
    public async Task Parse_TwitterImageSrcFallback()
    {
        const string html = """
                <html>
                <head>
                    <meta name="twitter:image:src" content="https://example.com/src.jpg" />
                </head>
                <body></body>
                </html>
                """;

        var result = await Parse(html);

        Assert.Equal("https://example.com/src.jpg", result.TwitterCard.Image);
    }

    [Fact]
    public async Task Parse_ReadsOpenGraphFromNameAndTwitterFromProperty()
    {
        const string html = """
                <html>
                <head>
                    <meta name="og:title" content="OG via name" />
                    <meta property="twitter:card" content="summary" />
                </head>
                <body></body>
                </html>
                """;

        var result = await Parse(html);

        Assert.Equal("OG via name", result.OpenGraph.Title);
        Assert.Equal("summary", result.TwitterCard.Card);
    }

    [Fact]
    public async Task Parse_FirstOccurrenceWins()
    {
        const string html = """
                <html>
                <head>
                    <title>First</title>
                    <title>Second</title>
                    <meta property="og:image" content="https://example.com/first.jpg" />
                    <meta property="og:image" content="https://example.com/second.jpg" />
                </head>
                <body></body>
                </html>
                """;

        var result = await Parse(html);

        Assert.Equal("First", result.Tags.Title);
        Assert.Equal("https://example.com/first.jpg", result.OpenGraph.Image);
    }

    [Fact]
    public async Task Parse_ResolvesRelativeUrls()
    {
        const string html = """
                <html>
                <head>
                    <meta property="og:image" content="/img/cover.png" />
                    <meta property="og:url" content="post" />
                    <meta name="twitter:image" content="//cdn.example.net/tw.png" />
                    <link rel="canonical" href="../canonical" />
                    <link rel="icon" href="favicon.svg" />
                    <link rel="alternate" type="application/json+oembed" href="/oembed?id=1" />
                </head>
                <body></body>
                </html>
                """;

        var result = await new UrlPreviewClient().ParseAsync("https://example.com/blog/post", html);

        Assert.Equal("https://example.com/img/cover.png", result.OpenGraph.Image);
        Assert.Equal("https://example.com/blog/post", result.OpenGraph.Url);
        Assert.Equal("https://cdn.example.net/tw.png", result.TwitterCard.Image);
        Assert.Equal("https://example.com/canonical", result.Tags.Canonical);
        Assert.Equal("https://example.com/blog/favicon.svg", result.Tags.Favicon);
        Assert.Equal("https://example.com/oembed?id=1", result.Oembed.Json);
        Assert.Equal("https://example.com/img/cover.png", result.Tags.Images[0]);
    }

    [Fact]
    public async Task Parse_ResolvesAgainstBaseHref()
    {
        const string html = """
                <html>
                <head>
                    <base href="https://static.example.org/assets/" />
                    <meta property="og:image" content="cover.png" />
                </head>
                <body></body>
                </html>
                """;

        var result = await Parse(html);

        Assert.Equal("https://static.example.org/assets/cover.png", result.OpenGraph.Image);
    }

    [Fact]
    public async Task Parse_DoesNotResolveNonUrlValues()
    {
        const string html = """
                <html>
                <head>
                    <meta property="og:image" content="data:image/png;base64,AAAA" />
                    <meta property="og:image:width" content="1200" />
                    <meta property="og:image:alt" content="A picture" />
                    <link rel="alternate" hreflang="it" href="/it" />
                </head>
                <body></body>
                </html>
                """;

        var result = await Parse(html);

        Assert.Equal("data:image/png;base64,AAAA", result.OpenGraph.Image);
        Assert.Equal("1200", result.OpenGraph["og:image:width"]);
        Assert.Equal("A picture", result.OpenGraph["og:image:alt"]);
        Assert.Contains(result.Head, h => h.TryGetValue("href", out var href) && href == "/it");
    }

    [Fact]
    public async Task Parse_KeepsRelativeUrlsWhenResolutionDisabled()
    {
        const string html = """
                <html>
                <head><meta property="og:image" content="/cover.png" /></head>
                <body></body>
                </html>
                """;

        var client = new UrlPreviewClient(new Models.UrlPreviewOptions { ResolveRelativeUrls = false });
        var result = await client.ParseAsync("https://example.com", html);

        Assert.Equal("/cover.png", result.OpenGraph.Image);
    }

    [Fact]
    public async Task Parse_ExtractsTypedTags()
    {
        const string html = """
                <html lang="it-IT">
                <head>
                    <meta charset="utf-8" />
                    <meta name="Description" content="Page description" />
                    <meta name="viewport" content="width=device-width" />
                    <meta name="robots" content="index, follow" />
                    <meta name="theme-color" content="#112233" />
                    <meta name="generator" content="Hugo" />
                </head>
                <body></body>
                </html>
                """;

        var result = await Parse(html);

        Assert.Equal("it-IT", result.Tags.Language);
        Assert.Equal("utf-8", result.Tags.Charset);
        Assert.Equal("Page description", result.Tags.Description);
        Assert.Equal("width=device-width", result.Tags.Viewport);
        Assert.Equal("index, follow", result.Tags.Robots);
        Assert.Equal("#112233", result.Tags.ThemeColor);
        Assert.Equal("Hugo", result.Tags.Generator);
    }

    [Fact]
    public async Task Parse_ExtractsCharsetFromHttpEquiv()
    {
        const string html = """
                <html>
                <head><meta http-equiv="Content-Type" content="text/html; charset=ISO-8859-1" /></head>
                <body></body>
                </html>
                """;

        var result = await Parse(html);

        Assert.Equal("ISO-8859-1", result.Tags.Charset);
    }

    [Fact]
    public async Task Parse_FaviconPrefersIconOverAppleTouchIcon()
    {
        const string html = """
                <html>
                <head>
                    <link rel="apple-touch-icon" href="https://example.com/apple.png" />
                    <link rel="Shortcut Icon" href="https://example.com/favicon.ico" />
                </head>
                <body></body>
                </html>
                """;

        var result = await Parse(html);

        Assert.Equal("https://example.com/favicon.ico", result.Tags.Favicon);
    }

    [Fact]
    public async Task Parse_FaviconFallsBackToAppleTouchIcon()
    {
        const string html = """
                <html>
                <head><link rel="apple-touch-icon" href="https://example.com/apple.png" /></head>
                <body></body>
                </html>
                """;

        var result = await Parse(html);

        Assert.Equal("https://example.com/apple.png", result.Tags.Favicon);
    }

    [Fact]
    public async Task Parse_ResultRoundTripsThroughSystemTextJson()
    {
        const string html = """
                <html lang="en">
                <head>
                    <title>Title</title>
                    <meta property="og:image" content="/cover.png" />
                    <meta name="twitter:card" content="summary" />
                    <link rel="canonical" href="/canonical" />
                </head>
                <body></body>
                </html>
                """;

        var result = await Parse(html);

        var json = System.Text.Json.JsonSerializer.Serialize(result);
        var copy = System.Text.Json.JsonSerializer.Deserialize<Models.UrlPreviewResult>(json)!;

        Assert.Equal("https://example.com/cover.png", copy.OpenGraph.Image);
        Assert.Equal("summary", copy.TwitterCard.Card);
        Assert.Equal("https://example.com/canonical", copy.Tags.Canonical);
        Assert.Equal("en", copy.Tags.Language);
        Assert.Equal(result.Head.Count, copy.Head.Count);
    }

    [Fact]
    public async Task Parse_RelIsCaseInsensitive()
    {
        const string html = """
                <html>
                <head>
                    <link rel="Alternate" type="application/json+oembed" href="https://example.com/oembed" />
                </head>
                <body></body>
                </html>
                """;

        var result = await Parse(html);

        Assert.Equal("https://example.com/oembed", result.Oembed.Json);
    }
}

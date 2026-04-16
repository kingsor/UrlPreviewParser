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

        Assert.Equal(3, result.Head.Count);
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
}

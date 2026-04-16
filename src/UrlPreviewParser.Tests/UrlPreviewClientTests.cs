namespace UrlPreviewParser.Tests;

public class UrlPreviewClientTests
{
    [Fact]
    public async Task GetPreviewAsync_ThrowsOnNullUrl()
    {
        using var client = new UrlPreviewClient();
        await Assert.ThrowsAsync<ArgumentException>(() => client.GetPreviewAsync(null!));
    }

    [Fact]
    public async Task GetPreviewAsync_ThrowsOnEmptyUrl()
    {
        using var client = new UrlPreviewClient();
        await Assert.ThrowsAsync<ArgumentException>(() => client.GetPreviewAsync(string.Empty));
    }

    [Fact]
    public async Task GetPreviewAsync_ThrowsOnWhitespaceUrl()
    {
        using var client = new UrlPreviewClient();
        await Assert.ThrowsAsync<ArgumentException>(() => client.GetPreviewAsync("   "));
    }

    [Fact]
    public async Task ParseAsync_ThrowsOnEmptyUrl()
    {
        using var client = new UrlPreviewClient();
        await Assert.ThrowsAsync<ArgumentException>(() => client.ParseAsync("", "<html></html>"));
    }

    [Fact]
    public async Task ParseAsync_ThrowsOnNullHtml()
    {
        using var client = new UrlPreviewClient();
        await Assert.ThrowsAsync<ArgumentNullException>(() => client.ParseAsync("https://example.com", null!));
    }

    [Fact]
    public async Task ParseAsync_ReturnsMetadataWithoutHttpRequest()
    {
        const string html = """
                <html>
                <head>
                    <meta property="og:title" content="My Title" />
                    <meta property="og:description" content="My Desc" />
                    <meta name="twitter:card" content="summary" />
                </head>
                <body></body>
                </html>
                """;

        using var client = new UrlPreviewClient();
        var result = await client.ParseAsync("https://example.com", html);

        Assert.Equal("My Title", result.OpenGraph.Title);
        Assert.Equal("My Desc", result.OpenGraph.Description);
        Assert.Equal("summary", result.TwitterCard.Card);
        Assert.Equal("https://example.com", result.Url);
    }

    [Fact]
    public async Task ParseAsync_DoesNotFetchOembedEndpoint()
    {
        // ParseAsync should discover the oEmbed endpoint but NOT fetch it
        const string html = """
                <html>
                <head>
                    <link rel="alternate" type="application/json+oembed"
                          href="https://this-should-never-be-fetched.invalid/oembed" />
                </head>
                <body></body>
                </html>
                """;

        using var client = new UrlPreviewClient();
        var result = await client.ParseAsync("https://example.com", html);

        Assert.Equal("https://this-should-never-be-fetched.invalid/oembed", result.Oembed.Json);
        Assert.Null(result.Oembed.Body);
    }
}
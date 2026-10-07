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

    [Fact]
    public async Task GetPreviewAsync_UsesFinalUrlAfterRedirectAsBase()
    {
        var handler = new StubHandler((request, _) =>
        {
            var response = HtmlResponse("""<html><head><meta property="og:image" content="cover.png" /></head></html>""");
            response.RequestMessage = new HttpRequestMessage(HttpMethod.Get, "https://www.example.com/new/page");
            return Task.FromResult(response);
        });

        using var httpClient = new HttpClient(handler);
        using var client = new UrlPreviewClient(httpClient);
        var result = await client.GetPreviewAsync("https://example.com/old");

        Assert.Equal("https://example.com/old", result.Url);
        Assert.Equal("https://www.example.com/new/page", result.FinalUrl);
        Assert.Equal("https://www.example.com/new/cover.png", result.OpenGraph.Image);
    }

    [Fact]
    public async Task GetPreviewAsync_DecodesCharsetFromContentType()
    {
        var bytes = System.Text.Encoding.Latin1.GetBytes("<html><head><title>Caffè</title></head></html>");
        var handler = new StubHandler((_, _) =>
        {
            var content = new ByteArrayContent(bytes);
            content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("text/html") { CharSet = "iso-8859-1" };
            return Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK) { Content = content });
        });

        using var httpClient = new HttpClient(handler);
        using var client = new UrlPreviewClient(httpClient);
        var result = await client.GetPreviewAsync("https://example.com");

        Assert.Equal("Caffè", result.Tags.Title);
    }

    [Fact]
    public async Task GetPreviewAsync_DecodesCharsetFromMetaTag()
    {
        var bytes = System.Text.Encoding.Latin1.GetBytes(
            """<html><head><meta charset="iso-8859-1"><title>Perché</title></head></html>""");
        var handler = new StubHandler((_, _) =>
        {
            var content = new ByteArrayContent(bytes);
            content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("text/html");
            return Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK) { Content = content });
        });

        using var httpClient = new HttpClient(handler);
        using var client = new UrlPreviewClient(httpClient);
        var result = await client.GetPreviewAsync("https://example.com");

        Assert.Equal("Perché", result.Tags.Title);
    }

    [Fact]
    public async Task GetPreviewAsync_TruncatesContentAtMaxLength()
    {
        var html = "<html><head><title>T</title></head><body>" + new string('x', 10_000) + "</body></html>";
        var handler = new StubHandler((_, _) => Task.FromResult(HtmlResponse(html)));

        using var httpClient = new HttpClient(handler);
        using var client = new UrlPreviewClient(httpClient, new Models.UrlPreviewOptions { MaxContentLength = 1_000 });
        var result = await client.GetPreviewAsync("https://example.com");

        Assert.Equal(1_000, result.Body!.Length);
        Assert.Equal("T", result.Tags.Title);
    }

    [Fact]
    public async Task GetPreviewAsync_FetchesOembedOnlyWhenEnabled()
    {
        const string html = """<html><head><link rel="alternate" type="application/json+oembed" href="/oembed" /></head></html>""";
        var requests = new List<string>();
        var handler = new StubHandler((request, _) =>
        {
            requests.Add(request.RequestUri!.AbsoluteUri);
            return Task.FromResult(HtmlResponse(request.RequestUri.AbsolutePath == "/oembed" ? """{"type":"rich"}""" : html));
        });

        using var httpClient = new HttpClient(handler);
        using var enabled = new UrlPreviewClient(httpClient);
        var withOembed = await enabled.GetPreviewAsync("https://example.com/page");

        Assert.Equal("""{"type":"rich"}""", withOembed.Oembed.Body);
        Assert.Equal(["https://example.com/page", "https://example.com/oembed"], requests);

        requests.Clear();
        using var disabled = new UrlPreviewClient(httpClient, new Models.UrlPreviewOptions { FetchOembed = false });
        var withoutOembed = await disabled.GetPreviewAsync("https://example.com/page");

        Assert.Null(withoutOembed.Oembed.Body);
        Assert.Equal(["https://example.com/page"], requests);
    }

    [Fact]
    public async Task GetPreviewAsync_ThrowsTimeoutException()
    {
        var handler = new StubHandler(async (_, cancellationToken) =>
        {
            await Task.Delay(Timeout.Infinite, cancellationToken);
            return HtmlResponse("<html></html>");
        });

        using var httpClient = new HttpClient(handler);
        using var client = new UrlPreviewClient(httpClient, new Models.UrlPreviewOptions { Timeout = TimeSpan.FromMilliseconds(100) });

        await Assert.ThrowsAsync<TimeoutException>(() => client.GetPreviewAsync("https://example.com"));
    }

    [Fact]
    public async Task GetPreviewAsync_PropagatesCallerCancellation()
    {
        var handler = new StubHandler(async (_, cancellationToken) =>
        {
            await Task.Delay(Timeout.Infinite, cancellationToken);
            return HtmlResponse("<html></html>");
        });

        using var httpClient = new HttpClient(handler);
        using var client = new UrlPreviewClient(httpClient);
        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => client.GetPreviewAsync("https://example.com", cts.Token));
    }

    [Fact]
    public async Task GetPreviewAsync_ThrowsOnErrorStatus()
    {
        var handler = new StubHandler((_, _) => Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.NotFound)));

        using var httpClient = new HttpClient(handler);
        using var client = new UrlPreviewClient(httpClient);

        var exception = await Assert.ThrowsAsync<HttpRequestException>(() => client.GetPreviewAsync("https://example.com"));
        Assert.Equal(System.Net.HttpStatusCode.NotFound, exception.StatusCode);
    }

    private static HttpResponseMessage HtmlResponse(string html) =>
        new(System.Net.HttpStatusCode.OK) { Content = new StringContent(html, System.Text.Encoding.UTF8, "text/html") };

    private sealed class StubHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> send) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return send(request, cancellationToken);
        }
    }
}
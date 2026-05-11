# UrlPreviewParser

[![NuGet](https://img.shields.io/nuget/v/UrlPreviewParser.svg)](https://www.nuget.org/packages/UrlPreviewParser)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

A .NET library for extracting link preview metadata (OpenGraph, Twitter Card, oEmbed) from URLs.

## Installation

**.NET CLI**
```bash
dotnet add package UrlPreviewParser
```

**Package Manager**
```powershell
Install-Package UrlPreviewParser
```

**PackageReference**
```xml
<PackageReference Include="UrlPreviewParser" Version="1.0.2" />
```

## Quick Start

```csharp
using UrlPreviewParser;

using var client = new UrlPreviewClient();
var result = await client.GetPreviewAsync("https://example.com");

// OpenGraph
Console.WriteLine(result.OpenGraph.Title);
Console.WriteLine(result.OpenGraph.Description);
Console.WriteLine(result.OpenGraph.Image);

// Twitter Card
Console.WriteLine(result.TwitterCard.Title);
Console.WriteLine(result.TwitterCard.Image);

// Basic page title
Console.WriteLine(result.Tags.Title);
```

## Accessing the Data

### OpenGraph (`result.OpenGraph`)

`OpenGraphData` extends `Dictionary<string, string>`. Any `og:*` property is accessible by key
(e.g., `result.OpenGraph["og:title"]`). Convenience properties cover the most common fields:

| Property | Key |
|---|---|
| `Title` | `og:title` |
| `Description` | `og:description` |
| `Image` | `og:image` |
| `Url` | `og:url` |
| `Type` | `og:type` |
| `SiteName` | `og:site_name` |

### Twitter Card (`result.TwitterCard`)

`TwitterCardData` extends `Dictionary<string, string>`. Any `twitter:*` property is accessible
by key (e.g., `result.TwitterCard["twitter:label1"]`). Convenience properties:

| Property | Key | Notes |
|---|---|---|
| `Card` | `twitter:card` | e.g. `summary`, `summary_large_image` |
| `Title` | `twitter:title` | |
| `Description` | `twitter:description` | |
| `Image` | `twitter:image` | Falls back to `twitter:image:src` |
| `Url` | `twitter:url` | |
| `Site` | `twitter:site` | @username of the website |
| `Creator` | `twitter:creator` | @username of the content creator |

### oEmbed (`result.Oembed`)

Discovered oEmbed endpoint URLs and the raw response body:

| Property | Type | Description |
|---|---|---|
| `Json` | `string?` | URL of the JSON oEmbed endpoint |
| `Xml` | `string?` | URL of the XML oEmbed endpoint |
| `Formats` | `List<string>` | Discovered format names (e.g. `"json"`, `"xml"`) |
| `Body` | `string?` | Raw response body from the oEmbed endpoint |
| `Error` | `string?` | Error message if the endpoint fetch failed |

### Tags (`result.Tags`)

| Property | Type | Description |
|---|---|---|
| `Title` | `string?` | Content of the HTML `<title>` element |
| `Images` | `List<string>` | Image URLs found on the page |

### Raw head elements (`result.Head`)

`result.Head` is a `List<Dictionary<string, string?>>` containing the raw attributes of every
child element of `<head>`. Useful when you need access to tags not covered by the typed properties.

## Using Your Own HttpClient

When using dependency injection, pass your `HttpClient` directly. The caller is responsible for
its lifetime; `UrlPreviewClient` will not dispose it.

```csharp
// ASP.NET Core — register via IHttpClientFactory
builder.Services.AddHttpClient<MyService>();

// Then inject and use
public class MyService(IHttpClientFactory factory)
{
    public async Task<UrlPreviewResult> GetPreviewAsync(string url, CancellationToken ct)
    {
        var httpClient = factory.CreateClient();
        using var client = new UrlPreviewClient(httpClient);
        return await client.GetPreviewAsync(url, ct);
    }
}
```

## Offline Parsing

`ParseAsync` parses metadata from an HTML string without making any HTTP requests. The oEmbed
endpoint is discovered but **not** fetched.

```csharp
using var client = new UrlPreviewClient();
var result = await client.ParseAsync("https://example.com", htmlString);
```

## License

MIT — see [LICENSE](https://github.com/kingsor/UrlPreviewParser/blob/main/LICENSE) for details.

# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

A .NET library for extracting link preview metadata from URLs — a C# port of the Node.js [linkpreview](https://github.com/nicktacular/link-preview) project. Published as a NuGet package.

## Commands

**Build:**
```bash
dotnet restore --configuration Release
dotnet build --configuration Release --no-restore
```

**Test:**
```bash
dotnet test --configuration Release --no-build --collect:"XPlat Code Coverage" --results-directory ./code-coverage
```

**Run a single test:**
```bash
dotnet test --filter "FullyQualifiedName~MetadataParserTests.Parse_ExtractsOpenGraphTags"
```

**Pack NuGet:**
```bash
dotnet pack --configuration Release --no-build -o ./NuGet
```

## Architecture

The library has one public entry point and four internal components:

- **`UrlPreviewClient`** — Public API. Accepts an optional `HttpClient` and optional `UrlPreviewOptions` (timeout, max content length, oEmbed fetching, relative URL resolution) and is `IDisposable` (only disposes `HttpClient` when it owns it). Exposes `GetPreviewAsync(url)` (fetches + parses + oEmbed; throws `TimeoutException` when `Options.Timeout` elapses) and `ParseAsync(url, html)` (offline parsing only — oEmbed endpoint is discovered but never fetched).
- **`MetadataParser`** (internal, `UrlPreviewParser.Internal`) — Uses AngleSharp to extract OpenGraph (`og:*`) and Twitter Card (`twitter:*`) from either `property` or `name` (first occurrence wins), oEmbed endpoint links, typed tags (title, description, canonical, favicon, `<html lang>`, charset, viewport, robots, theme-color, generator), and raw `<head>` attributes. All dictionary keys are stored lowercased.
- **`UrlResolver`** (internal) — Resolves URL-valued properties against `<base href>` / `FinalUrl`. Only an explicit key list is resolved; `Head` keeps raw values.
- **`HtmlContentReader`** (internal) — Reads at most `MaxContentLength` bytes and decodes them (Content-Type charset → BOM → `<meta charset>` sniffing → UTF-8).
- **`OembedFetcher`** (internal, `UrlPreviewParser.Internal`) — After parsing, fetches the discovered oEmbed JSON endpoint and stores the raw response in `Oembed.Body`. Fetch errors are swallowed silently into `Oembed.Error` rather than thrown (caller cancellation still propagates).

### Namespaces

- `UrlPreviewParser` — public surface (`UrlPreviewClient`)
- `UrlPreviewParser.Models` — result types and `UrlPreviewOptions`
- `UrlPreviewParser.Internal` — `MetadataParser`, `UrlResolver`, `HtmlContentReader`, `OembedFetcher`

### Result model

`UrlPreviewResult` contains four sub-objects:

- **`OpenGraphData`** and **`TwitterCardData`** extend `Dictionary<string, string>` with typed convenience properties (e.g., `.Title`, `.Image`). Keys are always lowercase (e.g., `"og:title"`). `TwitterCard.Image` falls back to `twitter:image:src` when `twitter:image` is absent.
- **`OembedData`** — plain class with `Json`/`Xml` endpoint URLs, `Formats` list, raw `Body`, and `Error`.
- **`TagsData`** — plain class with `Title` (trimmed, first `<title>`), `Images` list (populated from OG or Twitter image) and typed tags (`Description`, `Canonical`, `Favicon`, `Language`, `Charset`, `Viewport`, `Robots`, `ThemeColor`, `Generator`).
- `Head` — `List<Dictionary<string, string?>>` of every `<head>` child element's raw attributes.
- `Url` (requested) and `FinalUrl` (after redirects). All properties have setters so the result round-trips through System.Text.Json.

## Code Style

- **C# 14**, file-scoped namespaces, primary constructors, collection expressions, pattern matching, switch expressions.
- Nullable reference types enforced — use `is null` / `is not null`, never `== null`.
- Insert a newline before the opening `{` of any code block (`if`, `for`, `foreach`, `using`, `try`, etc.).
- Use `ArgumentNullException.ThrowIfNull` to validate input parameters; `ObjectDisposedException.ThrowIf` where applicable.
- Async methods: `Async` suffix, return `Task`/`ValueTask`, always pass `CancellationToken`, call `.ConfigureAwait(false)`.
- All public APIs require XML documentation. Use `<remarks>` with links to relevant docs where helpful. Use `<see langword="null" />` (not inline backticks) for `null`/`true`/`false`. Overriding members use `/// <inheritdoc />`.
- NativeAOT compatibility where possible (no reflection, no dynamic).
- No UTF-8 BOM unless non-ASCII characters are present.
- Never modify `global.json` or `NuGet.config` unless explicitly asked.

## Testing

- Framework: **xUnit v3 SDK** with **NSubstitute** for mocks, **coverlet** for coverage.
- Tests target **net10.0**; library targets **netstandard2.0**.
- Add tests to existing files (`UrlPreviewClientTests.cs`, `MetadataParserTests.cs`) rather than creating new ones.
- No `// Arrange / Act / Assert` comments. No commented-out or disabled tests.
- When running tests, use `--filter` and verify test run counts to confirm tests actually executed.

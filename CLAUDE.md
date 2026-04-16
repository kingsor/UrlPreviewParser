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

The library has one public entry point and two internal components:

- **`UrlPreviewClient`** — Public API. Accepts an optional `HttpClient`. Exposes `GetPreviewAsync(url)` (fetches + parses) and `ParseAsync(html, url)` (offline parsing only).
- **`MetadataParser`** (internal) — Uses AngleSharp to extract OpenGraph (`og:*`), Twitter Card (`twitter:*`), oEmbed endpoint links, page title, and raw `<head>` attributes from HTML.
- **`OembedFetcher`** (internal) — After parsing, fetches the discovered oEmbed JSON endpoint and merges the response into the result.

Result is a `UrlPreviewResult` containing typed sub-objects: `OpenGraphData`, `TwitterCardData`, `OembedData`, and `TagsData`. Each uses a backing dictionary with convenience properties.

## Code Style (from `.github/copilot-instructions.md`)

- **C# 14**, file-scoped namespaces, primary constructors, collection expressions, pattern matching, switch expressions.
- Nullable reference types enforced — use `is null` / `is not null`.
- Async methods: `Async` suffix, return `Task`/`ValueTask`, always pass `CancellationToken`, call `.ConfigureAwait(false)`.
- All public APIs require XML documentation.
- NativeAOT compatibility where possible (no reflection, no dynamic).
- No UTF-8 BOM unless non-ASCII characters are present.

## Testing

- Framework: **xUnit v3 SDK** with **NSubstitute** for mocks, **coverlet** for coverage.
- Targets **net10.0**; library targets **netstandard2.0**.
- Add tests to existing files (`UrlPreviewClientTests.cs`, `MetadataParserTests.cs`) rather than creating new ones.
- No `// Arrange / Act / Assert` comments. No commented-out or disabled tests.

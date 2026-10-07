using System;
using System.Collections.Generic;

namespace UrlPreviewParser.Internal;

internal static class UrlResolver
{
    public static readonly HashSet<string> OpenGraphUrlKeys = new HashSet<string>(StringComparer.Ordinal)
    {
        "og:url",
        "og:image", "og:image:url", "og:image:secure_url",
        "og:video", "og:video:url", "og:video:secure_url",
        "og:audio", "og:audio:url", "og:audio:secure_url",
    };

    public static readonly HashSet<string> TwitterUrlKeys = new HashSet<string>(StringComparer.Ordinal)
    {
        "twitter:url",
        "twitter:image", "twitter:image:src",
        "twitter:player", "twitter:player:stream",
    };

    /// <summary>
    /// Computes the base URI of a document from its URL and an optional &lt;base href&gt;.
    /// </summary>
    public static Uri? GetBaseUri(string documentUrl, string? baseHref)
    {
        if (!TryCreateHttpUri(documentUrl, out var documentUri))
            return null;

        if (!string.IsNullOrWhiteSpace(baseHref)
            && Uri.TryCreate(documentUri, baseHref!.Trim(), out var baseUri)
            && IsHttp(baseUri))
        {
            return baseUri;
        }

        return documentUri;
    }

    /// <summary>
    /// Resolves <paramref name="value"/> against <paramref name="baseUri"/>. Absolute URLs (including
    /// non-HTTP schemes such as <c>data:</c>) and values that are not valid URI references are returned unchanged.
    /// </summary>
    public static string Resolve(Uri? baseUri, string value)
    {
        if (baseUri is null || string.IsNullOrWhiteSpace(value))
            return value;

        var trimmed = value.Trim();

        // On Unix "/path" parses as an absolute file:// URI, so root-relative paths are handled explicitly.
        if (!trimmed.StartsWith("/", StringComparison.Ordinal)
            && Uri.TryCreate(trimmed, UriKind.Absolute, out _))
        {
            return trimmed;
        }

        return Uri.TryCreate(baseUri, trimmed, out var resolved) ? resolved.AbsoluteUri : value;
    }

    public static void ResolveKeys(Dictionary<string, string> values, HashSet<string> keys, Uri baseUri)
    {
        foreach (var key in keys)
        {
            if (values.TryGetValue(key, out var value))
                values[key] = Resolve(baseUri, value);
        }
    }

    private static bool TryCreateHttpUri(string url, out Uri uri) =>
        Uri.TryCreate(url, UriKind.Absolute, out uri!) && IsHttp(uri);

    private static bool IsHttp(Uri uri) =>
        uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps;
}

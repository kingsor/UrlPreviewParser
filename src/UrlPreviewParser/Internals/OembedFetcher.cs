using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using UrlPreviewParser.Models;

namespace UrlPreviewParser.Internal;

internal static class OembedFetcher
{
    public static async Task FetchAsync(
        HttpClient httpClient,
        UrlPreviewResult result,
        long maxContentLength,
        CancellationToken cancellationToken = default)
    {
        if (result.Oembed.Formats.Count == 0 || result.Oembed.Json is null)
            return;

        try
        {
            using var response = await httpClient
                .GetAsync(result.Oembed.Json, HttpCompletionOption.ResponseHeadersRead, cancellationToken)
                .ConfigureAwait(false);
            response.EnsureSuccessStatusCode();
            result.Oembed.Body = await HtmlContentReader
                .ReadAsStringAsync(response.Content, maxContentLength, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
        {
            result.Oembed.Error = ex.Message;
        }
    }
}

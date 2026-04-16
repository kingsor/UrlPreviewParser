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
        CancellationToken cancellationToken = default)
    {
        if (result.Oembed.Formats.Count == 0 || result.Oembed.Json is null)
            return;

        try
        {
            var response = await httpClient
                .GetAsync(result.Oembed.Json, cancellationToken)
                .ConfigureAwait(false);
            response.EnsureSuccessStatusCode();
            result.Oembed.Body = await response.Content
                .ReadAsStringAsync()
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            result.Oembed.Error = ex.Message;
        }
    }
}

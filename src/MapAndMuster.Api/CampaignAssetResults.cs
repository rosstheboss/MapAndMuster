using MapAndMuster.Application.Campaigns;
using MapAndMuster.Application.Common;
using Microsoft.Net.Http.Headers;

namespace MapAndMuster.Api;

/// <summary>
/// Writes stored campaign files with cache headers that let a browser reuse them indefinitely.
/// </summary>
public static class CampaignAssetResults
{
    /// <summary>
    /// One year, the longest value RFC 9111 advises servers to send.
    /// </summary>
    private const int MaxAgeSeconds = 31_536_000;

    /// <summary>
    /// Reads the caller's cached tag from a request.
    /// </summary>
    /// <param name="request">The incoming request.</param>
    /// <returns>The raw <c>If-None-Match</c> value, or <see langword="null"/>.</returns>
    public static string? IfNoneMatch(HttpRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        var header = request.Headers.IfNoneMatch;
        return header.Count == 0 ? null : header.ToString();
    }

    /// <summary>
    /// Produces the response for a stored campaign file, including its cache policy.
    /// </summary>
    /// <param name="response">The response being written.</param>
    /// <param name="read">The result of reading the file.</param>
    /// <returns>A 304 when the caller's copy is current, otherwise the streamed file.</returns>
    /// <remarks>
    /// Caching is <c>private</c> because campaign assets are only served to members. The URL
    /// carries the same tag as the entity tag, so a changed file is a changed URL and
    /// <c>immutable</c> is safe: the browser never revalidates, and the old URL is simply no
    /// longer requested.
    /// </remarks>
    public static IResult File(HttpResponse response, CampaignAssetRead read)
    {
        ArgumentNullException.ThrowIfNull(response);
        ArgumentNullException.ThrowIfNull(read);
        ApplyCachePolicy(response);
        var entityTag = AssetTags.ToEntityTag(read.AssetTag);
        if (read.IsNotModified || read.File is null)
        {
            return new NotModifiedResult(entityTag);
        }

        return Results.Stream(
            read.File.Content,
            read.File.ContentType,
            read.File.DownloadName,
            entityTag: EntityTagHeaderValue.Parse(entityTag),
            enableRangeProcessing: true);
    }

    /// <summary>
    /// Applies the long-lived cache policy to a response that is about to be written.
    /// </summary>
    /// <param name="response">The response.</param>
    /// <param name="isPublic">Whether the file is served to anonymous callers.</param>
    public static void ApplyCachePolicy(HttpResponse response, bool isPublic = false)
    {
        ArgumentNullException.ThrowIfNull(response);
        var scope = isPublic ? "public" : "private";
        response.Headers.CacheControl = $"{scope}, max-age={MaxAgeSeconds}, immutable";
    }

    private sealed class NotModifiedResult : IResult
    {
        private readonly string _entityTag;

        public NotModifiedResult(string entityTag)
        {
            _entityTag = entityTag;
        }

        public Task ExecuteAsync(HttpContext httpContext)
        {
            ArgumentNullException.ThrowIfNull(httpContext);
            httpContext.Response.StatusCode = StatusCodes.Status304NotModified;
            httpContext.Response.Headers.ETag = _entityTag;
            return Task.CompletedTask;
        }
    }
}

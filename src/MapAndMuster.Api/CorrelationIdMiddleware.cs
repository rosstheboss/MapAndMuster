namespace MapAndMuster.Api;

/// <summary>
/// Assigns a correlation identifier to each request and includes it in log scopes. Never logs header values that look like secrets.
/// </summary>
public static class CorrelationIdMiddleware
{
    /// <summary>
    /// Request and response header name.
    /// </summary>
    public const string HeaderName = "X-Correlation-ID";

    /// <summary>
    /// Log-scope key.
    /// </summary>
    public const string ScopeKey = "CorrelationId";

    private const int MaxLength = 128;

    /// <summary>
    /// Adds correlation identifiers to the request, response, and logging scope.
    /// </summary>
    /// <param name="app">The application builder.</param>
    /// <returns>The same builder.</returns>
    public static IApplicationBuilder UseCampaignCorrelationId(this IApplicationBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);
        return app.Use(static async (context, next) =>
        {
            var correlationId = Resolve(context);
            context.Response.OnStarting(() =>
            {
                context.Response.Headers[HeaderName] = correlationId;
                return Task.CompletedTask;
            });

            var logger = context.RequestServices.GetRequiredService<ILoggerFactory>()
                .CreateLogger("MapAndMuster.Api.Correlation");
            using (logger.BeginScope(new Dictionary<string, object>
            {
                [ScopeKey] = correlationId,
                ["RequestId"] = context.TraceIdentifier,
            }))
            {
                await next().ConfigureAwait(false);
            }
        });
    }

    /// <summary>
    /// Returns a safe correlation identifier from the incoming header or the request trace identifier.
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    /// <returns>The identifier.</returns>
    public static string Resolve(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        var incoming = context.Request.Headers[HeaderName].ToString();
        return IsSafe(incoming) ? incoming : context.TraceIdentifier;
    }

    private static bool IsSafe(string value)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length > MaxLength)
        {
            return false;
        }

        foreach (var character in value)
        {
            if (!IsSafeCharacter(character))
            {
                return false;
            }
        }

        return true;
    }

    private static bool IsSafeCharacter(char character)
    {
        return char.IsAsciiLetterOrDigit(character)
            || character is '-' or '_' or ':' or '.';
    }
}

namespace MapAndMuster.Api;

/// <summary>
/// Treats a client-aborted request as a cancelled operation rather than a server failure.
/// Browsers abort in-flight image and poll requests on refresh, navigation, and debugger pauses.
/// </summary>
public static class CancelledRequestMiddleware
{
    /// <summary>
    /// Swallows <see cref="OperationCanceledException"/> when the HTTP client aborted the request.
    /// </summary>
    /// <param name="app">The application builder.</param>
    /// <returns>The same builder.</returns>
    public static IApplicationBuilder UseCampaignCancelledRequests(this IApplicationBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);
        return app.Use(static async (context, next) =>
        {
            try
            {
                await next().ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
            {
                // The client disconnected. Do not surface this as an unhandled 500.
            }
        });
    }
}

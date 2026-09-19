using MapAndMuster.Application.Common;
using MapAndMuster.Application.Identity;

namespace MapAndMuster.Api;

/// <summary>
/// Guests may read public campaign and chat data. Mutations are rejected except sign-out and
/// converting the session into a real account, so preview users cannot join, chat, or save.
/// </summary>
public static class GuestReadOnlyMiddleware
{
    private static readonly PathString[] AllowedAuthPosts =
    [
        "/api/auth/logout",
        "/api/auth/guest-login",
        "/api/auth/login",
        "/api/auth/register",
        "/api/auth/confirm-email",
        "/api/auth/resend-confirmation",
        "/api/auth/forgot-password",
        "/api/auth/reset-password",
        "/api/auth/external/complete",
    ];

    /// <summary>
    /// Rejects non-safe HTTP methods for guest sessions.
    /// </summary>
    /// <param name="app">The application builder.</param>
    /// <returns>The same builder.</returns>
    public static IApplicationBuilder UseGuestReadOnly(this IApplicationBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);
        return app.Use(static async (context, next) =>
        {
            if (context.User.IsGuest() && !IsAllowed(context.Request))
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                await context.Response.WriteAsJsonAsync(
                        new Contracts.ErrorResponse(ErrorCodes.GuestForbidden, GuestRestrictions.Message))
                    .ConfigureAwait(false);
                return;
            }

            await next().ConfigureAwait(false);
        });
    }

    private static bool IsAllowed(HttpRequest request)
    {
        if (HttpMethods.IsGet(request.Method)
            || HttpMethods.IsHead(request.Method)
            || HttpMethods.IsOptions(request.Method))
        {
            return true;
        }

        if (!HttpMethods.IsPost(request.Method))
        {
            return false;
        }

        foreach (var path in AllowedAuthPosts)
        {
            if (request.Path.Equals(path, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }
}

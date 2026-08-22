using System.Threading.RateLimiting;
using Campaign.Api;
using Campaign.Api.Endpoints;
using Campaign.Infrastructure;
using Microsoft.AspNetCore.Http.Features;

var builder = WebApplication.CreateBuilder(args);

ProductionConfiguration.Validate(builder.Configuration, builder.Environment);

builder.Services.AddOpenApi();
builder.Services.AddCampaignHealthChecks(builder.Configuration);
builder.Services.AddProblemDetails();
builder.Services.AddCampaignInfrastructure(builder.Configuration);
builder.Services.AddCampaignExternalAuthentication(builder.Configuration);
builder.Services.AddAuthorization();
builder.Services.AddHttpClient("external-avatar", client =>
{
    client.Timeout = TimeSpan.FromSeconds(10);
});

if (ForwardedHeadersHosting.ShouldEnable(builder.Configuration, builder.Environment))
{
    builder.Services.Configure<ForwardedHeadersOptions>(ForwardedHeadersHosting.Configure);
}

builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 24 * 1024 * 1024;
});
builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxRequestBodySize = 24 * 1024 * 1024;
});

builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.Name = "campaign.auth";
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
    options.SlidingExpiration = true;
    options.ExpireTimeSpan = TimeSpan.FromDays(14);
    options.Events.OnRedirectToLogin = context =>
    {
        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        return Task.CompletedTask;
    };
    options.Events.OnRedirectToAccessDenied = context =>
    {
        context.Response.StatusCode = StatusCodes.Status403Forbidden;
        return Task.CompletedTask;
    };
});

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy(IdentityHttp.AuthRateLimitPolicy, httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 20,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
            }));
    options.AddPolicy(IdentityHttp.UploadRateLimitPolicy, httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 10,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
            }));
    options.AddPolicy(IdentityHttp.ChatRateLimitPolicy, httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 30,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
            }));
});

var app = builder.Build();

if (ForwardedHeadersHosting.ShouldEnable(app.Configuration, app.Environment))
{
    app.UseForwardedHeaders();
}

app.UseCampaignCorrelationId();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

if (!app.Environment.IsEnvironment("Testing"))
{
    // The Angular dev server proxies /api over HTTP. Visual Studio still sets an HTTPS port from
    // the https launch profile, which would 307 the browser onto a different origin and fail CORS.
    if (!app.Environment.IsDevelopment())
    {
        app.UseHttpsRedirection();
    }

    app.UseRateLimiter();
}

app.UseAuthentication();
app.UseAuthorization();
app.MapCampaignHealthChecks();
app.MapAuthEndpoints();
app.MapProfileEndpoints();
app.MapExternalAuthEndpoints();
app.MapCampaignEndpoints();
app.MapHomeBoardEndpoints();
app.MapSiteChatEndpoints();

await DatabaseStartup.ApplyMigrationsAsync(app).ConfigureAwait(false);

app.Run();

/// <summary>
/// ASP.NET Core entry point for the campaign API host.
/// </summary>
public partial class Program
{
}

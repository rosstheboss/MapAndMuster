using Microsoft.AspNetCore.HttpOverrides;

namespace Campaign.Api;

/// <summary>
/// Configures forwarded-header processing for reverse proxies such as Render.
/// </summary>
public static class ForwardedHeadersHosting
{
    /// <summary>
    /// Configuration section name.
    /// </summary>
    public const string SectionName = "ForwardedHeaders";

    /// <summary>
    /// Returns whether forwarded headers should be consumed.
    /// </summary>
    /// <param name="configuration">The configuration.</param>
    /// <param name="environment">The host environment.</param>
    /// <returns><see langword="true"/> when enabled explicitly or when the environment is Production or Staging.</returns>
    public static bool ShouldEnable(IConfiguration configuration, IHostEnvironment environment)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(environment);
        var configured = configuration.GetValue<bool?>($"{SectionName}:Enabled");
        if (configured.HasValue)
        {
            return configured.Value;
        }

        return ProductionConfiguration.IsProductionLike(environment);
    }

    /// <summary>
    /// Trusts the platform proxy in front of the container. Do not expose the container port directly to the internet.
    /// </summary>
    /// <param name="options">The forwarded-header options.</param>
    public static void Configure(ForwardedHeadersOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        options.ForwardedHeaders = ForwardedHeaders.XForwardedFor
            | ForwardedHeaders.XForwardedProto
            | ForwardedHeaders.XForwardedHost;
        options.KnownIPNetworks.Clear();
        options.KnownProxies.Clear();
    }
}

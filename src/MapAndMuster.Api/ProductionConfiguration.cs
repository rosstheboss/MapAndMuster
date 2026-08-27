using MapAndMuster.Infrastructure.Identity;

namespace MapAndMuster.Api;

/// <summary>
/// Fails fast when production-like hosts are missing required configuration. Error text names keys, never values.
/// </summary>
public static class ProductionConfiguration
{
    /// <summary>
    /// Validates required production and staging settings.
    /// </summary>
    /// <param name="configuration">The configuration.</param>
    /// <param name="environment">The host environment.</param>
    /// <exception cref="InvalidOperationException">Thrown when required keys are missing or invalid.</exception>
    public static void Validate(IConfiguration configuration, IHostEnvironment environment)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(environment);
        if (!IsProductionLike(environment))
        {
            return;
        }

        var missing = new List<string>();
        Require(configuration, "ConnectionStrings:Campaign", missing);
        Require(configuration, "PublicWeb:Origin", missing);
        Require(configuration, "Email:Provider", missing);
        Require(configuration, "Email:FromAddress", missing);
        Require(configuration, IdentityBootstrapOptions.BootstrapAdminPasswordKey, missing);
        Require(configuration, IdentityBootstrapOptions.BootstrapAdminEmailKey, missing);

        var origin = configuration["PublicWeb:Origin"];
        if (!string.IsNullOrWhiteSpace(origin))
        {
            if (IsLocalOrigin(origin))
            {
                throw new InvalidOperationException("PublicWeb:Origin must not be a localhost URL in Production or Staging.");
            }

            if (environment.IsEnvironment("Staging") && !HostHasDnsLabel(origin, "staging"))
            {
                throw new InvalidOperationException(
                    "PublicWeb:Origin for Staging must include a staging DNS label, such as staging.<domain>.");
            }

            if (environment.IsProduction() && HostHasDnsLabel(origin, "staging"))
            {
                throw new InvalidOperationException(
                    "PublicWeb:Origin for Production must not include a staging DNS label.");
            }
        }

        var provider = configuration["Email:Provider"];
        if (string.Equals(provider, "Resend", StringComparison.OrdinalIgnoreCase))
        {
            Require(configuration, "Email:Resend:ApiKey", missing);
        }
        else if (string.Equals(provider, "Smtp", StringComparison.OrdinalIgnoreCase))
        {
            Require(configuration, "Email:SmtpHost", missing);
        }
        else if (!string.IsNullOrWhiteSpace(provider))
        {
            throw new InvalidOperationException("Email:Provider must be Smtp or Resend.");
        }

        if (missing.Count > 0)
        {
            throw new InvalidOperationException(
                "Missing required production configuration keys: " + string.Join(", ", missing) + ".");
        }
    }

    /// <summary>
    /// Returns whether the host should enforce production configuration.
    /// </summary>
    /// <param name="environment">The host environment.</param>
    /// <returns><see langword="true"/> for Production and Staging.</returns>
    public static bool IsProductionLike(IHostEnvironment environment)
    {
        ArgumentNullException.ThrowIfNull(environment);
        return environment.IsProduction() || environment.IsEnvironment("Staging");
    }

    private static void Require(IConfiguration configuration, string key, List<string> missing)
    {
        if (string.IsNullOrWhiteSpace(configuration[key]))
        {
            missing.Add(key);
        }
    }

    private static bool IsLocalOrigin(string origin)
    {
        if (!Uri.TryCreate(origin, UriKind.Absolute, out var uri))
        {
            return true;
        }

        return uri.IsLoopback;
    }

    private static bool HostHasDnsLabel(string origin, string label)
    {
        if (!Uri.TryCreate(origin, UriKind.Absolute, out var uri))
        {
            return false;
        }

        return uri.IdnHost.Split('.', StringSplitOptions.RemoveEmptyEntries)
            .Any(part => string.Equals(part, label, StringComparison.OrdinalIgnoreCase));
    }
}

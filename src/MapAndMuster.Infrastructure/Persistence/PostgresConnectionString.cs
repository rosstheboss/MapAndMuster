using System.Data.Common;
using Npgsql;

namespace MapAndMuster.Infrastructure.Persistence;

/// <summary>
/// Converts Render and other <c>postgres://</c> URIs into Npgsql keyword connection strings.
/// Npgsql's connection-string builder rejects URI form, which is what Render injects from
/// <c>fromDatabase.connectionString</c>.
/// </summary>
public static class PostgresConnectionString
{
    /// <summary>
    /// Upper bound on pooled connections. Hosted PostgreSQL plans allow far fewer connections than
    /// Npgsql's default of 100, so leaving the default in place means the pool is willing to open
    /// more connections than the server will accept and the failure arrives as a refused
    /// connection under load rather than as a queued request.
    /// </summary>
    private const int DefaultMaxPoolSize = 20;

    /// <summary>Kept above zero so an idle instance does not pay the TLS handshake on its first request.</summary>
    private const int DefaultMinPoolSize = 1;

    /// <summary>Seconds to wait for a connection, including time spent queued behind the pool limit.</summary>
    private const int DefaultConnectTimeoutSeconds = 15;

    /// <summary>Seconds a single statement may run before it is cancelled.</summary>
    private const int DefaultCommandTimeoutSeconds = 30;

    /// <summary>
    /// Returns a keyword-form connection string. Existing <c>Host=...</c> values pass through after trimming.
    /// </summary>
    /// <param name="connectionString">A PostgreSQL URI or Npgsql keyword string.</param>
    /// <returns>An Npgsql keyword-form connection string.</returns>
    /// <exception cref="InvalidOperationException">Thrown when a URI cannot be parsed. The message omits the value.</exception>
    public static string Normalize(string connectionString)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

        var trimmed = connectionString.Trim().Trim('\uFEFF').Trim('`').Trim('"').Trim('\'').TrimStart('<').TrimEnd('>');
        if (trimmed.Length == 0)
        {
            throw new InvalidOperationException("The PostgreSQL connection string is empty after trimming.");
        }

        if (!trimmed.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase)
            && !trimmed.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase))
        {
            return ApplyPoolDefaults(trimmed);
        }

        if (!Uri.TryCreate(trimmed, UriKind.Absolute, out var uri) || string.IsNullOrWhiteSpace(uri.Host))
        {
            throw new InvalidOperationException("The PostgreSQL connection string URI could not be parsed.");
        }

        var userInfo = uri.GetComponents(UriComponents.UserInfo, UriFormat.Unescaped);
        var separator = userInfo.IndexOf(':');
        var user = separator < 0 ? userInfo : userInfo[..separator];
        var password = separator < 0 ? string.Empty : userInfo[(separator + 1)..];
        var database = Uri.UnescapeDataString(uri.AbsolutePath.Trim('/')).Trim('`');
        var port = uri.IsDefaultPort || uri.Port <= 0 ? 5432 : uri.Port;
        var sslMode = ReadSslMode(uri.Query);

        var parts = new List<string>
        {
            $"Host={uri.Host}",
            $"Port={port}",
            $"Database={Quote(database)}",
            $"Username={Quote(user)}",
            $"Password={Quote(password)}",
        };

        if (uri.IsLoopback)
        {
            parts.Add("SSL Mode=Disable");
        }
        else
        {
            parts.Add($"SSL Mode={sslMode ?? "Require"}");
        }

        // Render (and local Docker) authenticate with password + TLS, not Kerberos.
        // Npgsql 10 prefers GSS encryption, which probes libgssapi_krb5.so.2 and fails loudly
        // in the official aspnet images that no longer ship that library.
        parts.Add("GSS Encryption Mode=Disable");

        return ApplyPoolDefaults(string.Join(';', parts));
    }

    /// <summary>
    /// Fills in pool and timeout keywords the caller left unset.
    /// </summary>
    /// <remarks>
    /// Only absent keywords are filled, so an operator can still tune any of these per environment
    /// through the configured connection string.
    /// </remarks>
    private static string ApplyPoolDefaults(string keywordForm)
    {
        // Presence is read from a plain builder because NpgsqlConnectionStringBuilder reports every
        // keyword it knows about as present, whether the caller wrote it or not.
        var provided = new DbConnectionStringBuilder { ConnectionString = keywordForm };
        var builder = new NpgsqlConnectionStringBuilder(keywordForm);
        if (!WasProvided(provided, "Maximum Pool Size", "MaxPoolSize"))
        {
            builder.MaxPoolSize = DefaultMaxPoolSize;
        }

        if (!WasProvided(provided, "Minimum Pool Size", "MinPoolSize"))
        {
            builder.MinPoolSize = DefaultMinPoolSize;
        }

        if (!WasProvided(provided, "Timeout"))
        {
            builder.Timeout = DefaultConnectTimeoutSeconds;
        }

        if (!WasProvided(provided, "Command Timeout", "CommandTimeout"))
        {
            builder.CommandTimeout = DefaultCommandTimeoutSeconds;
        }

        return builder.ConnectionString;
    }

    private static bool WasProvided(DbConnectionStringBuilder provided, params string[] keywords)
    {
        foreach (var keyword in keywords)
        {
            if (provided.ContainsKey(keyword))
            {
                return true;
            }
        }

        return false;
    }

    private static string? ReadSslMode(string query)
    {
        if (string.IsNullOrEmpty(query))
        {
            return null;
        }

        foreach (var pair in query.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var parts = pair.Split('=', 2);
            if (parts.Length != 2 || !parts[0].Equals("sslmode", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            return MapSslMode(Uri.UnescapeDataString(parts[1]));
        }

        return null;
    }

    private static string MapSslMode(string value) => value.Trim().ToLowerInvariant() switch
    {
        "disable" => "Disable",
        "allow" => "Allow",
        "prefer" => "Prefer",
        "require" => "Require",
        "verify-ca" or "verifyca" => "VerifyCA",
        "verify-full" or "verifyfull" => "VerifyFull",
        _ => "Require",
    };

    private static string Quote(string value)
    {
        if (value.IndexOfAny([';', '\'', '=', ' ']) < 0)
        {
            return value;
        }

        return "'" + value.Replace("'", "''", StringComparison.Ordinal) + "'";
    }
}

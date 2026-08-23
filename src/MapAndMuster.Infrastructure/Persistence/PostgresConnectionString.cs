namespace MapAndMuster.Infrastructure.Persistence;

/// <summary>
/// Converts Render and other <c>postgres://</c> URIs into Npgsql keyword connection strings.
/// Npgsql's connection-string builder rejects URI form, which is what Render injects from
/// <c>fromDatabase.connectionString</c>.
/// </summary>
public static class PostgresConnectionString
{
    /// <summary>
    /// Returns a keyword-form connection string. Existing <c>Host=...</c> values pass through after trimming.
    /// </summary>
    /// <param name="connectionString">A PostgreSQL URI or Npgsql keyword string.</param>
    /// <returns>An Npgsql keyword-form connection string.</returns>
    /// <exception cref="InvalidOperationException">Thrown when a URI cannot be parsed. The message omits the value.</exception>
    public static string Normalize(string connectionString)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

        var trimmed = connectionString.Trim().Trim('\uFEFF').Trim('"').Trim('\'').TrimStart('<').TrimEnd('>');
        if (trimmed.Length == 0)
        {
            throw new InvalidOperationException("The PostgreSQL connection string is empty after trimming.");
        }

        if (!trimmed.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase)
            && !trimmed.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase))
        {
            return trimmed;
        }

        if (!Uri.TryCreate(trimmed, UriKind.Absolute, out var uri) || string.IsNullOrWhiteSpace(uri.Host))
        {
            throw new InvalidOperationException("The PostgreSQL connection string URI could not be parsed.");
        }

        var userInfo = uri.GetComponents(UriComponents.UserInfo, UriFormat.Unescaped);
        var separator = userInfo.IndexOf(':');
        var user = separator < 0 ? userInfo : userInfo[..separator];
        var password = separator < 0 ? string.Empty : userInfo[(separator + 1)..];
        var database = Uri.UnescapeDataString(uri.AbsolutePath.Trim('/'));
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

        return string.Join(';', parts);
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

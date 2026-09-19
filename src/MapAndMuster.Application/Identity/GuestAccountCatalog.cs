using System.Diagnostics.CodeAnalysis;

namespace MapAndMuster.Application.Identity;

/// <summary>
/// Temporary preview accounts named Guest001, Guest002, and so on. Numbers return to the pool
/// when the session ends or expires. They have no credentials and cannot mutate campaign or chat data.
/// </summary>
public static class GuestAccountCatalog
{
    /// <summary>How long a guest session remains valid from the server clock.</summary>
    public static readonly TimeSpan Lifetime = TimeSpan.FromHours(24);

    /// <summary>Upper bound on simultaneous guest rows, so allocation cannot grow without limit.</summary>
    public const int MaxConcurrent = 10_000;

    /// <summary>Placeholder city, region, and country stored on guest rows. They are never shown as a real location.</summary>
    public const string Location = "Preview";

    /// <summary>Username prefix plus a zero-padded number, for example Guest001.</summary>
    public static string Username(int number)
    {
        return number < 1000 ? $"Guest{number:000}" : $"Guest{number}";
    }

    /// <summary>Non-deliverable mailbox used only as a unique Identity email.</summary>
    public static string Email(int number)
    {
        return $"guest{number}@guests.invalid";
    }

    /// <summary>Public display name, for example Guest 1.</summary>
    public static string DisplayName(int number)
    {
        return $"Guest {number}";
    }

    /// <summary>
    /// Returns the forced display name for a guest account.
    /// </summary>
    public static bool TryDisplayName(UserAccount account, [NotNullWhen(true)] out string? displayName)
    {
        ArgumentNullException.ThrowIfNull(account);
        if (account.IsGuestAccount && account.GuestAccountNumber is int number and > 0)
        {
            displayName = DisplayName(number);
            return true;
        }

        displayName = null;
        return false;
    }
}

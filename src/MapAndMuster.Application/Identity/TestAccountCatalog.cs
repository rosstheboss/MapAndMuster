using System.Diagnostics.CodeAnalysis;

namespace MapAndMuster.Application.Identity;

/// <summary>
/// Seeded Test 1 through Test 45 accounts. They have no email delivery, cannot password-login, and cannot use site chat.
/// Forty-five accounts covers every Old World faction plus each named subfaction, with spare bots.
/// </summary>
public static class TestAccountCatalog
{
    /// <summary>How many test accounts are seeded outside the Testing environment.</summary>
    public const int Count = 45;

    /// <summary>Username prefix plus the account number, for example test1.</summary>
    public static string Username(int number)
    {
        return $"test{number}";
    }

    /// <summary>Non-deliverable mailbox used only as a unique Identity email.</summary>
    public static string Email(int number)
    {
        return $"test{number}@users.invalid";
    }

    /// <summary>Public display name, for example Test 12.</summary>
    public static string DisplayName(int number)
    {
        return $"Test {number}";
    }

    /// <summary>
    /// Returns the forced display name for a seeded test account.
    /// </summary>
    public static bool TryDisplayName(UserAccount account, [NotNullWhen(true)] out string? displayName)
    {
        ArgumentNullException.ThrowIfNull(account);
        if (account.IsTestAccount && account.TestAccountNumber is int number and > 0)
        {
            displayName = DisplayName(number);
            return true;
        }

        displayName = null;
        return false;
    }
}

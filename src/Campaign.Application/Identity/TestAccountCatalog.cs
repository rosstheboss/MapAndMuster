using System.Diagnostics.CodeAnalysis;

namespace Campaign.Application.Identity;

/// <summary>
/// Seeded administrator test accounts Test 1 through Test 30. They have no email delivery and cannot use site chat.
/// </summary>
public static class TestAccountCatalog
{
    /// <summary>How many test accounts are seeded outside the Testing environment.</summary>
    public const int Count = 30;

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

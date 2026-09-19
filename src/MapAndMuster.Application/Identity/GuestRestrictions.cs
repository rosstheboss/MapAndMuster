using MapAndMuster.Application.Common;

namespace MapAndMuster.Application.Identity;

/// <summary>
/// Shared denial used when a guest session tries to mutate campaign, chat, or account state.
/// </summary>
public static class GuestRestrictions
{
    /// <summary>Safe explanation returned to guest callers who attempt a mutation.</summary>
    public const string Message =
        "Guest accounts can preview the site but cannot change anything. Sign up to create a real account.";

    /// <summary>Builds a failed operation result for a guest mutation.</summary>
    public static OperationResult Deny()
    {
        return OperationResult.Failure(ErrorCodes.GuestForbidden, Message);
    }

    /// <summary>Builds a failed typed operation result for a guest mutation.</summary>
    /// <typeparam name="T">The unused success type.</typeparam>
    public static OperationResult<T> Deny<T>()
    {
        return OperationResults.Failure<T>(ErrorCodes.GuestForbidden, Message);
    }
}

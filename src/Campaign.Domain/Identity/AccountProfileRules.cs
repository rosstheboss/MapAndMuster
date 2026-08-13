using System.Diagnostics.CodeAnalysis;
using Campaign.Domain.Common;

namespace Campaign.Domain.Identity;

/// <summary>
/// Collects username, legal-name, location, and time-zone validation without failing on the first field.
/// </summary>
public static class AccountProfileRules
{
    /// <summary>
    /// Validates every profile field and, when all succeed, returns the parsed value objects.
    /// </summary>
    /// <param name="username">The username.</param>
    /// <param name="firstName">The first name.</param>
    /// <param name="middleInitial">The optional middle initial.</param>
    /// <param name="lastName">The last name.</param>
    /// <param name="suffix">The optional name suffix.</param>
    /// <param name="city">The city.</param>
    /// <param name="region">The state, province, or region.</param>
    /// <param name="country">The country.</param>
    /// <param name="timeZoneId">The IANA time-zone identifier.</param>
    /// <param name="parsedUsername">The parsed username when validation succeeds.</param>
    /// <param name="name">The parsed name when validation succeeds.</param>
    /// <param name="location">The parsed location when validation succeeds.</param>
    /// <param name="timeZone">The parsed time zone when validation succeeds.</param>
    /// <param name="errors">Every field error, in a stable field order.</param>
    /// <returns><see langword="true"/> when every field is valid.</returns>
    public static bool TryCreate(
        string? username,
        string? firstName,
        string? middleInitial,
        string? lastName,
        string? suffix,
        string? city,
        string? region,
        string? country,
        string? timeZoneId,
        [NotNullWhen(true)] out Username? parsedUsername,
        [NotNullWhen(true)] out PersonName? name,
        [NotNullWhen(true)] out GeographicLocation? location,
        [NotNullWhen(true)] out IanaTimeZone? timeZone,
        out IReadOnlyList<DomainError> errors)
    {
        var collected = new List<DomainError>();

        if (!Username.TryCreate(username, out parsedUsername, out var usernameError))
        {
            collected.Add(usernameError);
        }

        collected.AddRange(PersonName.CollectErrors(firstName, middleInitial, lastName, suffix));
        collected.AddRange(GeographicLocation.CollectErrors(city, region, country));

        if (!IanaTimeZone.TryCreate(timeZoneId, out timeZone, out var timeZoneError))
        {
            collected.Add(timeZoneError);
        }

        if (collected.Count > 0)
        {
            name = null;
            location = null;
            errors = collected;
            return false;
        }

        // CollectErrors already accepted every remaining field, so these parses cannot fail.
        _ = PersonName.TryCreate(firstName, middleInitial, lastName, suffix, out name, out _);
        _ = GeographicLocation.TryCreate(city, region, country, out location, out _);
        errors = collected;
        return parsedUsername is not null && name is not null && location is not null && timeZone is not null;
    }
}

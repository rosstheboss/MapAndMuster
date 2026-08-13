using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;
using Campaign.Domain.Common;

namespace Campaign.Domain.Identity;

/// <summary>
/// A coarse public location for an account. Other users may see city, region, and country.
/// </summary>
public sealed class GeographicLocation
{
    /// <summary>
    /// Maximum length of a location field.
    /// </summary>
    public const int MaxLength = 100;

    private static readonly Regex LocationPattern = new(
        @"^[\p{L}\p{N}][\p{L}\p{N}\s'.,()/-]{0,99}$",
        RegexOptions.CultureInvariant | RegexOptions.Compiled,
        TimeSpan.FromMilliseconds(100));

    private GeographicLocation(string city, string? region, string country)
    {
        City = city;
        Region = region;
        Country = country;
    }

    /// <summary>
    /// Gets the city.
    /// </summary>
    public string City { get; }

    /// <summary>
    /// Gets the optional state, province, or region.
    /// </summary>
    public string? Region { get; }

    /// <summary>
    /// Gets the country.
    /// </summary>
    public string Country { get; }

    /// <summary>
    /// Attempts to create a location from user input.
    /// </summary>
    /// <param name="city">The city.</param>
    /// <param name="region">The optional state, province, or region.</param>
    /// <param name="country">The country.</param>
    /// <param name="location">The created location when validation succeeds.</param>
    /// <param name="error">The validation error when creation fails.</param>
    /// <returns><see langword="true"/> when the location is valid.</returns>
    public static bool TryCreate(
        string? city,
        string? region,
        string? country,
        [NotNullWhen(true)] out GeographicLocation? location,
        [NotNullWhen(false)] out DomainError? error)
    {
        location = null;

        if (!TryValidateRequired(city, "city.invalid", "City", out var parsedCity, out error))
        {
            return false;
        }

        if (!TryValidateRequired(country, "country.invalid", "Country", out var parsedCountry, out error))
        {
            return false;
        }

        string? parsedRegion = null;
        if (!string.IsNullOrWhiteSpace(region))
        {
            if (!TryValidateRequired(region, "region.invalid", "State or region", out parsedRegion, out error))
            {
                return false;
            }
        }

        location = new GeographicLocation(parsedCity, parsedRegion, parsedCountry);
        error = null;
        return true;
    }

    /// <summary>
    /// Formats the location for display.
    /// </summary>
    /// <returns>A comma-separated location string.</returns>
    public string Format()
    {
        return string.IsNullOrWhiteSpace(Region)
            ? $"{City}, {Country}"
            : $"{City}, {Region}, {Country}";
    }

    private static bool TryValidateRequired(
        string? raw,
        string errorCode,
        string fieldLabel,
        [NotNullWhen(true)] out string? value,
        [NotNullWhen(false)] out DomainError? error)
    {
        value = null;
        if (string.IsNullOrWhiteSpace(raw))
        {
            error = new DomainError(errorCode, $"{fieldLabel} is required.");
            return false;
        }

        var trimmed = CollapseWhitespace(raw.Trim());
        if (trimmed.Length > MaxLength || !LocationPattern.IsMatch(trimmed))
        {
            error = new DomainError(
                errorCode,
                $"{fieldLabel} must be 1-{MaxLength} characters and cannot include markup or control characters.");
            return false;
        }

        value = trimmed;
        error = null;
        return true;
    }

    private static string CollapseWhitespace(string value)
    {
        return Regex.Replace(value, @"\s+", " ", RegexOptions.CultureInvariant, TimeSpan.FromMilliseconds(100));
    }
}

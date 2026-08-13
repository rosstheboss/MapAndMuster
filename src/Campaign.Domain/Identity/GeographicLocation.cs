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

    private GeographicLocation(string city, string region, string country)
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
    /// Gets the state, province, or region.
    /// </summary>
    public string Region { get; }

    /// <summary>
    /// Gets the country.
    /// </summary>
    public string Country { get; }

    /// <summary>
    /// Attempts to create a location from user input.
    /// </summary>
    /// <param name="city">The city.</param>
    /// <param name="region">The state, province, or region.</param>
    /// <param name="country">The country.</param>
    /// <param name="location">The created location when validation succeeds.</param>
    /// <param name="error">The first validation error when creation fails.</param>
    /// <returns><see langword="true"/> when the location is valid.</returns>
    public static bool TryCreate(
        string? city,
        string? region,
        string? country,
        [NotNullWhen(true)] out GeographicLocation? location,
        [NotNullWhen(false)] out DomainError? error)
    {
        var errors = CollectErrors(city, region, country);
        if (errors.Count > 0)
        {
            location = null;
            error = errors[0];
            return false;
        }

        location = new GeographicLocation(
            CollapseWhitespace(city!.Trim()),
            CollapseWhitespace(region!.Trim()),
            CollapseWhitespace(country!.Trim()));
        error = null;
        return true;
    }

    /// <summary>
    /// Validates every location field and returns all failures.
    /// </summary>
    /// <param name="city">The city.</param>
    /// <param name="region">The state, province, or region.</param>
    /// <param name="country">The country.</param>
    /// <returns>The field errors, or an empty list.</returns>
    public static IReadOnlyList<DomainError> CollectErrors(string? city, string? region, string? country)
    {
        var errors = new List<DomainError>(3);
        if (!TryValidateRequired(city, "city", "City", out _, out var cityError))
        {
            errors.Add(cityError);
        }

        if (!TryValidateRequired(region, "region", "State or province", out _, out var regionError))
        {
            errors.Add(regionError);
        }

        if (!TryValidateRequired(country, "country", "Country", out _, out var countryError))
        {
            errors.Add(countryError);
        }

        return errors;
    }

    /// <summary>
    /// Formats the location for display.
    /// </summary>
    /// <returns>A comma-separated location string.</returns>
    public string Format()
    {
        return $"{City}, {Region}, {Country}";
    }

    private static bool TryValidateRequired(
        string? raw,
        string field,
        string fieldLabel,
        [NotNullWhen(true)] out string? value,
        [NotNullWhen(false)] out DomainError? error)
    {
        value = null;
        if (string.IsNullOrWhiteSpace(raw))
        {
            error = new DomainError($"{field}.invalid", $"{fieldLabel} is not filled in.", field);
            return false;
        }

        var trimmed = CollapseWhitespace(raw.Trim());
        if (trimmed.Length > MaxLength)
        {
            error = new DomainError(
                $"{field}.invalid",
                $"{fieldLabel} is too long (maximum {MaxLength} characters).",
                field);
            return false;
        }

        if (!LocationPattern.IsMatch(trimmed))
        {
            error = new DomainError(
                $"{field}.invalid",
                $"{fieldLabel} cannot include markup or control characters.",
                field);
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

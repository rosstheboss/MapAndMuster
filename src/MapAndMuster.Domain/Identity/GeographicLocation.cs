using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;
using MapAndMuster.Domain.Common;

namespace MapAndMuster.Domain.Identity;

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
    /// Validates an optional location. City requires a state or province, and a state or province requires a country.
    /// </summary>
    /// <param name="city">The optional city.</param>
    /// <param name="region">The optional state, province, or region.</param>
    /// <param name="country">The optional country.</param>
    /// <returns>The field errors, or an empty list.</returns>
    public static IReadOnlyList<DomainError> CollectOptionalErrors(string? city, string? region, string? country)
    {
        var errors = new List<DomainError>(3);
        var hasCity = !string.IsNullOrWhiteSpace(city);
        var hasRegion = !string.IsNullOrWhiteSpace(region);
        var hasCountry = !string.IsNullOrWhiteSpace(country);
        if (!hasCity && !hasRegion && !hasCountry)
        {
            return errors;
        }

        if (hasCity && !TryValidateRequired(city, "city", "City", out _, out var cityError))
        {
            errors.Add(cityError);
        }

        if (hasRegion && !TryValidateRequired(region, "region", "State or province", out _, out var regionError))
        {
            errors.Add(regionError);
        }

        if (hasCountry && !TryValidateRequired(country, "country", "Country", out _, out var countryError))
        {
            errors.Add(countryError);
        }

        if (hasCity && !hasRegion)
        {
            errors.Add(new DomainError(
                "region.invalid",
                "State or province is required when a city is provided.",
                "region"));
        }

        if (hasRegion && !hasCountry)
        {
            errors.Add(new DomainError(
                "country.invalid",
                "Country is required when a state or province is provided.",
                "country"));
        }

        return errors;
    }

    /// <summary>
    /// Normalizes an optional location after validating hierarchy and field shape.
    /// </summary>
    /// <param name="city">The optional city.</param>
    /// <param name="region">The optional state, province, or region.</param>
    /// <param name="country">The optional country.</param>
    /// <param name="normalizedCity">The trimmed city, or <see langword="null"/>.</param>
    /// <param name="normalizedRegion">The trimmed region, or <see langword="null"/>.</param>
    /// <param name="normalizedCountry">The trimmed country, or <see langword="null"/>.</param>
    /// <param name="errors">The field errors when validation fails.</param>
    /// <returns><see langword="true"/> when the optional location is valid.</returns>
    public static bool TryNormalizeOptional(
        string? city,
        string? region,
        string? country,
        out string? normalizedCity,
        out string? normalizedRegion,
        out string? normalizedCountry,
        out IReadOnlyList<DomainError> errors)
    {
        errors = CollectOptionalErrors(city, region, country);
        if (errors.Count > 0)
        {
            normalizedCity = null;
            normalizedRegion = null;
            normalizedCountry = null;
            return false;
        }

        normalizedCity = NormalizeOptionalPart(city);
        normalizedRegion = NormalizeOptionalPart(region);
        normalizedCountry = NormalizeOptionalPart(country);
        return true;
    }

    /// <summary>
    /// Formats the filled location parts for display.
    /// </summary>
    /// <param name="city">The optional city.</param>
    /// <param name="region">The optional state, province, or region.</param>
    /// <param name="country">The optional country.</param>
    /// <returns>A comma-separated location string, or <see langword="null"/> when empty.</returns>
    public static string? FormatOptional(string? city, string? region, string? country)
    {
        var parts = new[] { city, region, country }
            .Where(static part => !string.IsNullOrWhiteSpace(part))
            .Select(static part => CollapseWhitespace(part!.Trim()))
            .ToArray();
        return parts.Length == 0 ? null : string.Join(", ", parts);
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

    private static string? NormalizeOptionalPart(string? raw)
    {
        return string.IsNullOrWhiteSpace(raw) ? null : CollapseWhitespace(raw.Trim());
    }

    private static string CollapseWhitespace(string value)
    {
        return Regex.Replace(value, @"\s+", " ", RegexOptions.CultureInvariant, TimeSpan.FromMilliseconds(100));
    }
}

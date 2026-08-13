namespace Campaign.Domain.Common;

/// <summary>
/// A documented domain validation failure with a stable machine-readable code.
/// </summary>
/// <param name="Code">The stable error code.</param>
/// <param name="Message">A safe, non-secret explanation suitable for API clients.</param>
public sealed record DomainError(string Code, string Message);

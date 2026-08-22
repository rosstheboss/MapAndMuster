using MapAndMuster.Application.Ports;
using Microsoft.AspNetCore.Identity;

namespace MapAndMuster.Infrastructure.Security;

/// <summary>
/// Hashes shared secrets with ASP.NET Identity's PBKDF2 password hasher.
/// </summary>
public sealed class Pbkdf2SecretHasher : ISecretHasher
{
    private static readonly object Token = new();
    private readonly PasswordHasher<object> _hasher = new();

    /// <inheritdoc />
    public string Hash(string secret)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(secret);
        return _hasher.HashPassword(Token, secret);
    }

    /// <inheritdoc />
    public bool Verify(string hash, string secret)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(hash);
        ArgumentException.ThrowIfNullOrWhiteSpace(secret);
        return _hasher.VerifyHashedPassword(Token, hash, secret) != PasswordVerificationResult.Failed;
    }
}

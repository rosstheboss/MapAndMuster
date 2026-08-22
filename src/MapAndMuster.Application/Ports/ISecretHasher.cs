namespace MapAndMuster.Application.Ports;

/// <summary>
/// Hashes and verifies shared secrets such as private-campaign join passwords.
/// </summary>
public interface ISecretHasher
{
    /// <summary>
    /// Hashes a secret for storage.
    /// </summary>
    /// <param name="secret">The plaintext secret.</param>
    /// <returns>The hash.</returns>
    string Hash(string secret);

    /// <summary>
    /// Verifies a secret against a stored hash.
    /// </summary>
    /// <param name="hash">The stored hash.</param>
    /// <param name="secret">The plaintext secret.</param>
    /// <returns><see langword="true"/> when the secret matches.</returns>
    bool Verify(string hash, string secret);
}

using System.Security.Cryptography;

namespace WorldCuppy.Infrastructure.Auth;

/// <summary>PBKDF2 password hashing using built-in .NET cryptography — no external packages required.</summary>
internal static class PasswordHasher
{
    private const int SaltSize = 16;
    private const int KeySize = 32;
    private const int Iterations = 100_000;
    private static readonly HashAlgorithmName Algorithm = HashAlgorithmName.SHA256;

    /// <summary>Hashes <paramref name="password" /> with a random salt and returns a storable "salt:hash" string.</summary>
    internal static string Hash(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        var hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, Algorithm, KeySize);
        return $"{Convert.ToBase64String(salt)}:{Convert.ToBase64String(hash)}";
    }

    /// <summary>Returns <see langword="true" /> when <paramref name="password" /> matches <paramref name="storedHash" />.</summary>
    internal static bool Verify(string password, string storedHash)
    {
        var parts = storedHash.Split(':');
        if (parts.Length != 2)
        {
            return false;
        }

        var salt = Convert.FromBase64String(parts[0]);
        var expected = Convert.FromBase64String(parts[1]);
        var actual = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, Algorithm, KeySize);

        // Constant-time comparison prevents timing attacks.
        return CryptographicOperations.FixedTimeEquals(actual, expected);
    }
}

using System.Security.Cryptography;

namespace PetShop.Service.Security;

/// <summary>
/// PBKDF2 (SHA-256) password hashing with a per-password random salt.
/// Stored format: {iterations}.{base64(salt)}.{base64(hash)}.
/// </summary>
public class PasswordHasher : IPasswordHasher
{
    private const int SaltSize = 16;     // 128-bit
    private const int KeySize = 32;      // 256-bit
    private const int Iterations = 100_000;
    private static readonly HashAlgorithmName Algorithm = HashAlgorithmName.SHA256;
    private const char Delimiter = '.';

    public string Hash(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        var hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, Algorithm, KeySize);
        return string.Join(Delimiter, Iterations, Convert.ToBase64String(salt), Convert.ToBase64String(hash));
    }

    public bool Verify(string password, string storedHash)
    {
        var parts = storedHash.Split(Delimiter);
        if (parts.Length != 3) return false;
        if (!int.TryParse(parts[0], out var iterations)) return false;

        var salt = Convert.FromBase64String(parts[1]);
        var expected = Convert.FromBase64String(parts[2]);
        var actual = Rfc2898DeriveBytes.Pbkdf2(password, salt, iterations, Algorithm, expected.Length);
        return CryptographicOperations.FixedTimeEquals(actual, expected);
    }
}

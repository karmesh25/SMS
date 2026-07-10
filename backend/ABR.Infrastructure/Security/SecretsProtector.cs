using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using ABR.Application.Common;

namespace ABR.Infrastructure.Security;

public static class SecretsProtector
{
    // Authenticated format (v2): MAGIC(4) | salt(16) | nonce(12) | tag(16) | ciphertext
    // Uses AES-256-GCM (tamper-evident) with a random per-file PBKDF2 salt.
    private static readonly byte[] MagicV2 = "ABR2"u8.ToArray();
    private const int SaltSize = 16;
    private const int NonceSize = 12;
    private const int TagSize = 16;
    private const int KeyIterations = 100_000;

    // Legacy format (v1): IV(16) | AES-CBC ciphertext, keyed from a fixed salt.
    // Retained for read-only backward compatibility with older secrets.enc files.
    private static readonly byte[] LegacyKeyDerivationSalt = Encoding.UTF8.GetBytes("ABR-Secrets-Salt-v1");

    public static byte[] Encrypt(string plainText, string masterPassword) =>
        EncryptBytes(Encoding.UTF8.GetBytes(plainText), masterPassword);

    public static string Decrypt(byte[] cipher, string masterPassword) =>
        Encoding.UTF8.GetString(DecryptBytes(cipher, masterPassword));

    public static byte[] EncryptBytes(byte[] plainBytes, string masterPassword)
    {
        if (string.IsNullOrWhiteSpace(masterPassword))
            throw new ArgumentException("Master password is required.", nameof(masterPassword));

        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        var key = DeriveKey(masterPassword, salt);
        var nonce = RandomNumberGenerator.GetBytes(NonceSize);

        var cipherBytes = new byte[plainBytes.Length];
        var tag = new byte[TagSize];

        using (var aes = new AesGcm(key, TagSize))
        {
            aes.Encrypt(nonce, plainBytes, cipherBytes, tag);
        }

        var result = new byte[MagicV2.Length + SaltSize + NonceSize + TagSize + cipherBytes.Length];
        var offset = 0;
        MagicV2.CopyTo(result, offset); offset += MagicV2.Length;
        salt.CopyTo(result, offset); offset += SaltSize;
        nonce.CopyTo(result, offset); offset += NonceSize;
        tag.CopyTo(result, offset); offset += TagSize;
        cipherBytes.CopyTo(result, offset);
        return result;
    }

    public static byte[] DecryptBytes(byte[] cipher, string masterPassword)
    {
        if (string.IsNullOrWhiteSpace(masterPassword))
            throw new ArgumentException("Master password is required.", nameof(masterPassword));

        return HasMagic(cipher)
            ? DecryptV2(cipher, masterPassword)
            : DecryptLegacyCbc(cipher, masterPassword);
    }

    private static bool HasMagic(byte[] data)
    {
        if (data.Length < MagicV2.Length)
            return false;
        for (var i = 0; i < MagicV2.Length; i++)
        {
            if (data[i] != MagicV2[i])
                return false;
        }
        return true;
    }

    private static byte[] DecryptV2(byte[] cipher, string masterPassword)
    {
        var headerSize = MagicV2.Length + SaltSize + NonceSize + TagSize;
        if (cipher.Length < headerSize)
            throw new CryptographicException("Invalid or corrupted secrets file.");

        var offset = MagicV2.Length;
        var salt = cipher.AsSpan(offset, SaltSize).ToArray(); offset += SaltSize;
        var nonce = cipher.AsSpan(offset, NonceSize).ToArray(); offset += NonceSize;
        var tag = cipher.AsSpan(offset, TagSize).ToArray(); offset += TagSize;
        var cipherBytes = cipher.AsSpan(offset).ToArray();

        var key = DeriveKey(masterPassword, salt);
        var plainBytes = new byte[cipherBytes.Length];
        try
        {
            using var aes = new AesGcm(key, TagSize);
            aes.Decrypt(nonce, cipherBytes, tag, plainBytes);
        }
        catch (CryptographicException)
        {
            throw new CryptographicException("Invalid master password or corrupted secrets file.");
        }

        return plainBytes;
    }

    private static byte[] DecryptLegacyCbc(byte[] cipherWithIv, string masterPassword)
    {
        if (cipherWithIv.Length <= 16)
            throw new CryptographicException("Invalid secrets file.");

        using var derive = new Rfc2898DeriveBytes(masterPassword, LegacyKeyDerivationSalt, KeyIterations, HashAlgorithmName.SHA256);
        var key = derive.GetBytes(32);
        var iv = cipherWithIv.Take(16).ToArray();
        var cipherBytes = cipherWithIv.Skip(16).ToArray();

        using var aes = Aes.Create();
        aes.Key = key;
        aes.IV = iv;
        using var decryptor = aes.CreateDecryptor();
        try
        {
            return decryptor.TransformFinalBlock(cipherBytes, 0, cipherBytes.Length);
        }
        catch (CryptographicException)
        {
            throw new CryptographicException("Invalid master password or corrupted secrets file.");
        }
    }

    public static PendriveSecrets GenerateNew(string dbPassword)
    {
        return new PendriveSecrets
        {
            ConnectionStrings = new PendriveConnectionStrings
            {
                DefaultConnection =
                    $"Host=127.0.0.1;Port=5433;Database=abr_db;Username=postgres;Password={dbPassword}"
            },
            Jwt = new PendriveJwtSettings
            {
                SecretKey = GenerateRandomToken(64),
                Issuer = "ABR.Api",
                Audience = "ABR.Frontend",
                ExpiryHours = 8
            },
            Security = new PendriveSecuritySettings
            {
                LicenseSecret = GenerateRandomToken(64)
            }
        };
    }

    public static string Serialize(PendriveSecrets secrets) =>
        JsonSerializer.Serialize(secrets, JsonOptions);

    public static PendriveSecrets Deserialize(string json) =>
        JsonSerializer.Deserialize<PendriveSecrets>(json, JsonOptions)
        ?? throw new InvalidOperationException("Invalid secrets payload.");

    public static string GenerateDbPassword() => GenerateRandomToken(32);

    public static string? ExtractDbPassword(string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
            return null;

        foreach (var part in connectionString.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var idx = part.IndexOf('=');
            if (idx <= 0)
                continue;

            if (string.Equals(part[..idx].Trim(), "Password", StringComparison.OrdinalIgnoreCase))
                return part[(idx + 1)..].Trim();
        }

        return null;
    }

    public static IReadOnlyDictionary<string, string?> ToConfigurationMap(PendriveSecrets secrets) =>
        new Dictionary<string, string?>
        {
            ["ConnectionStrings:DefaultConnection"] = secrets.ConnectionStrings.DefaultConnection,
            ["Jwt:SecretKey"] = secrets.Jwt.SecretKey,
            ["Jwt:Issuer"] = secrets.Jwt.Issuer,
            ["Jwt:Audience"] = secrets.Jwt.Audience,
            ["Jwt:ExpiryHours"] = secrets.Jwt.ExpiryHours.ToString(),
            ["Security:LicenseSecret"] = secrets.Security.LicenseSecret
        };

    private static string GenerateRandomToken(int length)
    {
        const string alphabet = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-_";
        var bytes = RandomNumberGenerator.GetBytes(length);
        var chars = new char[length];
        for (var i = 0; i < length; i++)
            chars[i] = alphabet[bytes[i] % alphabet.Length];
        return new string(chars);
    }

    private static byte[] DeriveKey(string masterPassword, byte[] salt)
    {
        using var derive = new Rfc2898DeriveBytes(masterPassword, salt, KeyIterations, HashAlgorithmName.SHA256);
        return derive.GetBytes(32);
    }

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };
}

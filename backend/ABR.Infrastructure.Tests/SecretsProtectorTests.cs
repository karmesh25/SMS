using System.Security.Cryptography;
using ABR.Infrastructure.Security;

namespace ABR.Infrastructure.Tests;

public class SecretsProtectorTests
{
    [Fact]
    public void EncryptDecrypt_RoundTripsSecrets()
    {
        var secrets = SecretsProtector.GenerateNew("DbPass-Test-123!");
        var json = SecretsProtector.Serialize(secrets);
        var encrypted = SecretsProtector.Encrypt(json, "master-password-123");

        var decrypted = SecretsProtector.Decrypt(encrypted, "master-password-123");
        var restored = SecretsProtector.Deserialize(decrypted);

        Assert.Equal(secrets.ConnectionStrings.DefaultConnection, restored.ConnectionStrings.DefaultConnection);
        Assert.Equal(secrets.Jwt.SecretKey, restored.Jwt.SecretKey);
        Assert.Equal(secrets.Security.LicenseSecret, restored.Security.LicenseSecret);
    }

    [Fact]
    public void Decrypt_WithWrongPassword_Throws()
    {
        var encrypted = SecretsProtector.Encrypt("{}", "correct-password");
        Assert.Throws<CryptographicException>(() => SecretsProtector.Decrypt(encrypted, "wrong-password"));
    }

    [Fact]
    public void Encrypt_UsesAuthenticatedV2Format()
    {
        var encrypted = SecretsProtector.Encrypt("{}", "master-password-123");

        // v2 format starts with the "ABR2" magic header.
        Assert.True(encrypted.Length > 4);
        Assert.Equal(new byte[] { 0x41, 0x42, 0x52, 0x32 }, encrypted[..4]);
    }

    [Fact]
    public void Decrypt_WithTamperedCiphertext_Throws()
    {
        var encrypted = SecretsProtector.Encrypt("{\"secret\":\"value\"}", "master-password-123");

        // Flip a byte in the ciphertext body — GCM must reject it.
        encrypted[^1] ^= 0xFF;

        Assert.Throws<CryptographicException>(() => SecretsProtector.Decrypt(encrypted, "master-password-123"));
    }

    [Fact]
    public void EncryptBytes_DecryptBytes_RoundTripsBinaryData()
    {
        var data = new byte[512];
        new Random(42).NextBytes(data);

        var encrypted = SecretsProtector.EncryptBytes(data, "backup-pass");
        var restored = SecretsProtector.DecryptBytes(encrypted, "backup-pass");

        Assert.Equal(data, restored);
    }

    [Fact]
    public void ExtractDbPassword_ParsesConnectionString()
    {
        var password = SecretsProtector.ExtractDbPassword(
            "Host=127.0.0.1;Port=5433;Database=abr_db;Username=postgres;Password=MyStr0ng!Pass");
        Assert.Equal("MyStr0ng!Pass", password);
    }

    [Fact]
    public void GenerateDbPassword_ReturnsMinimumLength()
    {
        var password = SecretsProtector.GenerateDbPassword();
        Assert.True(password.Length >= 32);
    }
}

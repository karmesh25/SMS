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

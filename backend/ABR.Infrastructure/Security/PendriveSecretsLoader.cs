using System.Text.Json;
using ABR.Application.Common;

namespace ABR.Infrastructure.Security;

public static class PendriveSecretsLoader
{
    public const string MasterPasswordEnvVar = "ABR_MASTER_PASSWORD";
    public const string SecretsFileName = "secrets.enc";

    public static string ResolveSecretsPath()
    {
        var apiRoot = AppContext.BaseDirectory;
        var configRelative = Path.GetFullPath(Path.Combine(apiRoot, "..", "config", SecretsFileName));
        if (File.Exists(configRelative))
            return configRelative;

        var localConfig = Path.Combine(apiRoot, "config", SecretsFileName);
        return localConfig;
    }

    public static bool SecretsFileExists() => File.Exists(ResolveSecretsPath());

    public static IReadOnlyDictionary<string, string?> LoadConfigurationOverrides()
    {
        var secretsPath = ResolveSecretsPath();
        if (!File.Exists(secretsPath))
            return new Dictionary<string, string?>();

        var masterPassword = Environment.GetEnvironmentVariable(MasterPasswordEnvVar);
        if (string.IsNullOrWhiteSpace(masterPassword))
        {
            throw new InvalidOperationException(
                "Encrypted secrets file found but master password was not provided. " +
                $"Set environment variable {MasterPasswordEnvVar} (via START.bat).");
        }

        var encrypted = File.ReadAllBytes(secretsPath);
        var json = SecretsProtector.Decrypt(encrypted, masterPassword);
        var secrets = SecretsProtector.Deserialize(json);
        return SecretsProtector.ToConfigurationMap(secrets);
    }

    public static PendriveSecrets LoadSecrets(string masterPassword)
    {
        var secretsPath = ResolveSecretsPath();
        if (!File.Exists(secretsPath))
            throw new FileNotFoundException("Secrets file not found.", secretsPath);

        var encrypted = File.ReadAllBytes(secretsPath);
        var json = SecretsProtector.Decrypt(encrypted, masterPassword);
        return SecretsProtector.Deserialize(json);
    }

    public static void SaveSecrets(PendriveSecrets secrets, string masterPassword, string outputPath)
    {
        var json = SecretsProtector.Serialize(secrets);
        var encrypted = SecretsProtector.Encrypt(json, masterPassword);
        var directory = Path.GetDirectoryName(outputPath);
        if (!string.IsNullOrWhiteSpace(directory))
            Directory.CreateDirectory(directory);
        File.WriteAllBytes(outputPath, encrypted);
    }
}

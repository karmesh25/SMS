using System.Text.Json;
using ABR.Application.Common;
using ABR.Infrastructure.Security;

if (args.Length == 0)
{
    PrintUsage();
    return 1;
}

try
{
    return args[0].ToLowerInvariant() switch
    {
        "generate" => RunGenerate(args),
        "verify" => RunVerify(args),
        "dump-password" => RunDumpPassword(args),
        "write-setup-password" => RunWriteSetupPassword(args),
        _ => UnknownCommand()
    };
}
catch (Exception ex)
{
    Console.Error.WriteLine(ex.Message);
    return 1;
}

static int RunGenerate(string[] args)
{
    var masterPassword = GetRequired(args, "--master-password");
    var output = Path.GetFullPath(GetRequired(args, "--output"));
    var dbPassword = SecretsProtector.GenerateDbPassword();
    var secrets = SecretsProtector.GenerateNew(dbPassword);
    PendriveSecretsLoader.SaveSecrets(secrets, masterPassword, output);

    var setupPasswordPath = GetOptional(args, "--setup-password-file");
    if (!string.IsNullOrWhiteSpace(setupPasswordPath))
    {
        var fullPath = Path.GetFullPath(setupPasswordPath);
        var directory = Path.GetDirectoryName(fullPath);
        if (!string.IsNullOrWhiteSpace(directory))
            Directory.CreateDirectory(directory);
        File.WriteAllText(fullPath, dbPassword);
    }

    Console.WriteLine("Secrets file created.");
    return 0;
}

static int RunVerify(string[] args)
{
    var masterPassword = GetRequired(args, "--master-password");
    var secretsPath = GetOptional(args, "--secrets");
    if (string.IsNullOrWhiteSpace(secretsPath))
    {
        _ = PendriveSecretsLoader.LoadSecrets(masterPassword);
        return 0;
    }

    var encrypted = File.ReadAllBytes(Path.GetFullPath(secretsPath));
    var json = SecretsProtector.Decrypt(encrypted, masterPassword);
    _ = SecretsProtector.Deserialize(json);
    return 0;
}

static int RunDumpPassword(string[] args)
{
    var masterPassword = GetRequired(args, "--master-password");
    var secretsPath = GetOptional(args, "--secrets");
    var secrets = LoadSecrets(masterPassword, secretsPath);
    var password = SecretsProtector.ExtractDbPassword(secrets.ConnectionStrings.DefaultConnection);
    if (string.IsNullOrWhiteSpace(password))
        throw new InvalidOperationException("Database password not found in secrets.");

    Console.Write(password);
    return 0;
}

static int RunWriteSetupPassword(string[] args)
{
    var masterPassword = GetRequired(args, "--master-password");
    var output = Path.GetFullPath(GetRequired(args, "--output"));
    var secretsPath = GetOptional(args, "--secrets");
    var secrets = LoadSecrets(masterPassword, secretsPath);
    var password = SecretsProtector.ExtractDbPassword(secrets.ConnectionStrings.DefaultConnection);
    if (string.IsNullOrWhiteSpace(password))
        throw new InvalidOperationException("Database password not found in secrets.");

    File.WriteAllText(output, password);
    return 0;
}

static PendriveSecrets LoadSecrets(string masterPassword, string? secretsPath)
{
    if (string.IsNullOrWhiteSpace(secretsPath))
        return PendriveSecretsLoader.LoadSecrets(masterPassword);

    var encrypted = File.ReadAllBytes(Path.GetFullPath(secretsPath));
    var json = SecretsProtector.Decrypt(encrypted, masterPassword);
    return SecretsProtector.Deserialize(json);
}

static string GetRequired(string[] args, string name)
{
    for (var i = 0; i < args.Length - 1; i++)
    {
        if (string.Equals(args[i], name, StringComparison.OrdinalIgnoreCase))
            return args[i + 1];
    }

    throw new ArgumentException($"Missing required argument: {name}");
}

static string? GetOptional(string[] args, string name)
{
    for (var i = 0; i < args.Length - 1; i++)
    {
        if (string.Equals(args[i], name, StringComparison.OrdinalIgnoreCase))
            return args[i + 1];
    }

    return null;
}

static int UnknownCommand()
{
    PrintUsage();
    return 1;
}

static void PrintUsage()
{
    Console.WriteLine("""
        ABR.Secrets - pendrive secrets utility

        generate --master-password <pwd> --output <path> [--setup-password-file <path>]
        verify --master-password <pwd> [--secrets <path>]
        dump-password --master-password <pwd> [--secrets <path>]
        write-setup-password --master-password <pwd> --output <path> [--secrets <path>]
        """);
}

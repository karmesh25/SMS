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
        "encrypt-file" => RunEncryptFile(args),
        "decrypt-file" => RunDecryptFile(args),
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
    var masterPassword = GetMasterPassword(args);
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
    var masterPassword = GetMasterPassword(args);
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
    var masterPassword = GetMasterPassword(args);
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
    var masterPassword = GetMasterPassword(args);
    var output = Path.GetFullPath(GetRequired(args, "--output"));
    var secretsPath = GetOptional(args, "--secrets");
    var secrets = LoadSecrets(masterPassword, secretsPath);
    var password = SecretsProtector.ExtractDbPassword(secrets.ConnectionStrings.DefaultConnection);
    if (string.IsNullOrWhiteSpace(password))
        throw new InvalidOperationException("Database password not found in secrets.");

    File.WriteAllText(output, password);
    return 0;
}

static int RunEncryptFile(string[] args)
{
    var masterPassword = GetMasterPassword(args);
    var input = Path.GetFullPath(GetRequired(args, "--in"));
    var output = Path.GetFullPath(GetRequired(args, "--out"));

    var plain = File.ReadAllBytes(input);
    var encrypted = SecretsProtector.EncryptBytes(plain, masterPassword);
    File.WriteAllBytes(output, encrypted);
    Console.WriteLine("File encrypted.");
    return 0;
}

static int RunDecryptFile(string[] args)
{
    var masterPassword = GetMasterPassword(args);
    var input = Path.GetFullPath(GetRequired(args, "--in"));
    var output = Path.GetFullPath(GetRequired(args, "--out"));

    var encrypted = File.ReadAllBytes(input);
    var plain = SecretsProtector.DecryptBytes(encrypted, masterPassword);
    File.WriteAllBytes(output, plain);
    Console.WriteLine("File decrypted.");
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

// Resolve the master password without exposing it on the process command line:
//   1. --master-password <value>   (explicit; for manual/dev use)
//   2. --master-password -         (read one line from stdin)
//   3. ABR_MASTER_PASSWORD env var (preferred from the launcher scripts —
//      not shown in the command line and safe for any special characters)
static string GetMasterPassword(string[] args)
{
    var value = GetOptional(args, "--master-password");

    if (string.Equals(value, "-", StringComparison.Ordinal))
    {
        var line = Console.In.ReadLine();
        if (!string.IsNullOrEmpty(line))
            return line;
        throw new ArgumentException("Master password was not provided on stdin.");
    }

    if (!string.IsNullOrEmpty(value))
        return value;

    var env = Environment.GetEnvironmentVariable(PendriveSecretsLoader.MasterPasswordEnvVar);
    if (!string.IsNullOrEmpty(env))
        return env;

    throw new ArgumentException(
        "Master password required: pass --master-password <pwd>, use --master-password - to read stdin, " +
        $"or set the {PendriveSecretsLoader.MasterPasswordEnvVar} environment variable.");
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

        Master password is read from --master-password <pwd>, or "--master-password -"
        (stdin), or the ABR_MASTER_PASSWORD environment variable (preferred).

        generate [--master-password <pwd>] --output <path> [--setup-password-file <path>]
        verify [--master-password <pwd>] [--secrets <path>]
        dump-password [--master-password <pwd>] [--secrets <path>]
        write-setup-password [--master-password <pwd>] --output <path> [--secrets <path>]
        encrypt-file [--master-password <pwd>] --in <path> --out <path>
        decrypt-file [--master-password <pwd>] --in <path> --out <path>
        """);
}

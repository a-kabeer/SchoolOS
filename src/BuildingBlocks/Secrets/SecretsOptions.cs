using SchoolOS.BuildingBlocks.SharedKernel;

namespace SchoolOS.BuildingBlocks.Secrets;

public enum SecretProviderKind
{
    None = 0,
    HashiCorp = 1,
    AzureKeyVault = 2,
    Aws = 3,
    Gcp = 4,
}

public sealed class SecretsOptions
{
    public const string SectionName = "Secrets";
    public const int DefaultCacheTtlMinutes = 10;

    public string? Provider { get; set; }
    public int CacheTtlMinutes { get; set; } = DefaultCacheTtlMinutes;

    public HashiCorpVaultOptions HashiCorp { get; set; } = new();
    public AzureKeyVaultOptions AzureKeyVault { get; set; } = new();
    public AwsSecretsManagerOptions Aws { get; set; } = new();
    public GcpSecretManagerOptions Gcp { get; set; } = new();

    public SecretProviderKind? Kind =>
        string.IsNullOrWhiteSpace(Provider) ? SecretProviderKind.None
        : EnumNames.TryParse<SecretProviderKind>(Provider, out var kind) && kind != SecretProviderKind.None ? kind
        : null;
}

public enum HashiCorpAuthMethod
{
    Token = 1,
    AppRole = 2,
}

public sealed class HashiCorpVaultOptions
{
    public string? Address { get; set; }
    public string MountPoint { get; set; } = "secret";
    public HashiCorpAuthMethod AuthMethod { get; set; } = HashiCorpAuthMethod.Token;
    public string? Token { get; set; }
    public string? RoleId { get; set; }
    public string? SecretId { get; set; }
    public string AppRoleMountPoint { get; set; } = "approle";
}

public sealed class AzureKeyVaultOptions
{
    public string? VaultUri { get; set; }
}

public sealed class AwsSecretsManagerOptions
{
    public string? Region { get; set; }
}

public sealed class GcpSecretManagerOptions
{
    public string? ProjectId { get; set; }
}

namespace Relovia.Api.Features.Documents;

public sealed class StorageSettings
{
    public const string SectionName = "Storage";

    /// <summary><c>Local</c> writes a private directory. <c>S3</c> uses an S3-compatible bucket.</summary>
    public string Provider { get; set; } = "Local";

    public LocalStorageSettings Local { get; set; } = new();

    public S3StorageSettings S3 { get; set; } = new();
}

public sealed class LocalStorageSettings
{
    /// <summary>Absolute path, or a path under the content root. Empty uses <c>data/files</c>.</summary>
    public string Root { get; set; } = "";
}

public sealed class S3StorageSettings
{
    public string ServiceUrl { get; set; } = "";

    public string Region { get; set; } = "";

    public string Bucket { get; set; } = "";

    public string AccessKey { get; set; } = "";

    public string SecretKey { get; set; } = "";
}

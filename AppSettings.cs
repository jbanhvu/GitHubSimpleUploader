namespace GitHubSimpleUploader;

public sealed class AppSettings
{
    public List<SavedRepository> Repositories { get; set; } = [];

    public string? LastRepositoryId { get; set; }
}

public sealed class SavedRepository
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");

    public string Name { get; set; } = string.Empty;

    public string ProjectFolder { get; set; } = string.Empty;

    public string RepoUrl { get; set; } = string.Empty;

    public string Branch { get; set; } = "main";

    public string CommitMessage { get; set; } = string.Empty;

    public string CloneFolderName { get; set; } = string.Empty;

    public override string ToString()
    {
        return string.IsNullOrWhiteSpace(Name) ? ProjectFolder : Name;
    }
}

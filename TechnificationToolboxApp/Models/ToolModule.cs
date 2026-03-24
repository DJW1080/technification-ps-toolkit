namespace TechnificationToolboxApp.Models;

public sealed class ToolModule
{
    public ToolModule(string id, string name, string version, string description, string scriptRelativePath, bool requiresAdministrator, string? nativePageTag = null)
    {
        Id = id;
        Name = name;
        Version = version;
        Description = description;
        ScriptRelativePath = scriptRelativePath;
        RequiresAdministrator = requiresAdministrator;
        NativePageTag = nativePageTag;
    }

    public string Id { get; }
    public string Name { get; }
    public string Version { get; }
    public string Description { get; }
    public string ScriptRelativePath { get; }
    public bool RequiresAdministrator { get; }
    public string? NativePageTag { get; }
    public bool HasNativePage => !string.IsNullOrWhiteSpace(NativePageTag);
    public string ExperienceLabel => HasNativePage ? "Native page" : "Legacy only";
    public string RunModeLabel => RequiresAdministrator ? "Admin preferred" : "Standard okay";
}

namespace BanchoNET.Models.Dtos;

public class ClientBuildVersions
{
    public int id { get; set; }
    public Builds[] builds { get; set; }
}

public class Builds
{
    public ChangelogEntries[] changelog_entries { get; set; }
    public string version { get; set; }
    
}

public class ChangelogEntries
{
    public bool major { get; set; }
}
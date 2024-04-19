using BanchoNET.Objects.Players;
using BanchoNET.Objects.Privileges;

namespace BanchoNET.Attributes;

[AttributeUsage(AttributeTargets.Method)]
public class CommandAttribute : Attribute
{
    public string Name { get; }
    public Privileges Privileges { get; }
    public string BriefDescription { get; }
    public string DetailedDescription { get; }
    public string[]? Aliases { get; }

    /// <summary>
    /// Registers a new command with the given name, description, and aliases. Be aware
    /// that name and aliases will be registered as lowercase no matter what. Methods
    /// using this attribute must have structure like this: <see cref="Method(Player, string, string[])"/>.
    /// </summary>
    /// <param name="name">Name by which command will be called</param>
    /// <param name="privileges">Privileges needed to use that command</param>
    /// <param name="briefDescription">Brief description of what command does</param>
    /// <param name="detailedDescription">Detailed description that extends brief in !help &lt;command&gt;</param>
    /// <param name="aliases">Other names associated that can run this command</param>
    public CommandAttribute(
        string name,
        Privileges privileges,
        string briefDescription,
        string detailedDescription = "",
        string[]? aliases = default)
    {
        Name = name;
        Privileges = privileges;
        BriefDescription = briefDescription;
        DetailedDescription = detailedDescription;
        Aliases = aliases;
    }

    private static string Method(Player player, string commandBase, params string[] args)
    {
        return "";
    }
}
namespace BanchoNET.Attributes;

[AttributeUsage(AttributeTargets.Method)]
public class CommandAttribute : Attribute
{
    public string Name { get; }
    public string BriefDescription { get; }
    public string DetailedDescription { get; }
    public string[]? Aliases { get; }

    /// <summary>
    /// Registers a new command with the given name, description, and aliases. Be aware
    /// that name and aliases will be registered as lowercase no matter what. Methods
    /// using this attributes must: be static, return a string that contains result of command
    /// execution, and accept <c>params string[]</c> as argument to access parameters.
    /// </summary>
    /// <param name="name">Name by which command will be called</param>
    /// <param name="briefDescription">Brief description of what command does</param>
    /// <param name="detailedDescription">Detailed description that extends brief in !help &lt;command&gt;</param>
    /// <param name="aliases">Other names associated that can run this command</param>
    public CommandAttribute(
        string name,
        string briefDescription,
        string detailedDescription = "",
        string[]? aliases = default)
    {
        Name = name;
        BriefDescription = briefDescription;
        DetailedDescription = detailedDescription;
        Aliases = aliases;
    }
}
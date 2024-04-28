using BanchoNET.Models;
using BanchoNET.Objects.Privileges;
using BanchoNET.Utils;

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
    /// using this attribute must have structure like this: <see cref="MethodString(string[])"/>
    /// or <see cref="MethodTuple(string[])"/>.
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
        
        //Fix for multiplayer detailed description (it looked kinda awkward without prefix)
        DetailedDescription = detailedDescription.Replace("mp", $"{AppSettings.CommandPrefix}mp");
        
        Aliases = aliases;
    }
    
    private Task<string> MethodString(params string[] args)
    {
        return Task.FromResult("");
    }

    private Task<(bool, string)> MethodTuple(params string[] args)
    {
        return Task.FromResult((true, ""));
    }
}
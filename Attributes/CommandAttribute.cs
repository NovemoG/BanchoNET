using System.Text.RegularExpressions;
using BanchoNET.Objects.Privileges;
using BanchoNET.Utils;

namespace BanchoNET.Attributes;

[AttributeUsage(AttributeTargets.Method)]
public class CommandAttribute : Attribute
{
    public string Name { get; }
    public PlayerPrivileges Privileges { get; }
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
        PlayerPrivileges privileges,
        string briefDescription,
        string detailedDescription = "",
        string[]? aliases = null)
    {
        Name = name;
        Privileges = privileges;
        
        // Fix description syntaxes
        var syntaxRegex = new Regex(@"\bSyntax: \b");
        BriefDescription = syntaxRegex.Replace(briefDescription, $"Syntax: {AppSettings.CommandPrefix}");
        
        // Fix multiplayer detailed description
        if (name == "mp")
        {
            var mpRegex = new Regex(@"\nmp\b");
            
            DetailedDescription = mpRegex.Replace(detailedDescription, $"\n{AppSettings.CommandPrefix}mp");
        }
        else DetailedDescription = detailedDescription;
        
        Aliases = aliases;
    }
    
    private Task<string> MethodString(string[] args)
    {
        return Task.FromResult("");
    }

    private Task<(bool, string)> MethodTuple(string[] args)
    {
        return Task.FromResult((true, ""));
    }
}
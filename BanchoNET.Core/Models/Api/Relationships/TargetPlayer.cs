using BanchoNET.Core.Models.Api.Player;

namespace BanchoNET.Core.Models.Api.Relationships;

public class TargetPlayer : BasicApiPlayer
{
    public Group[] Groups { get; set; } = [];
    public Statistics Statistics { get; set; } = new();
    public int SupportLevel { get; set; }
}
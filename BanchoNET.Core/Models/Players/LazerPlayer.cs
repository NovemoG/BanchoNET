using BanchoNET.Core.Models.Api.Player;

namespace BanchoNET.Core.Models.Players;

public class LazerPlayer
{
    public required ApiPlayer Player { get; init; }
    public int[] Friends { get; set; } = [];
}
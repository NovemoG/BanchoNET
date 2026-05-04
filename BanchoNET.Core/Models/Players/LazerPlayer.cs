using BanchoNET.Core.Models.Api.Player;
using BanchoNET.Core.Models.Beatmaps;

namespace BanchoNET.Core.Models.Players;

public class LazerPlayer
{
    public required ApiPlayer Player { get; init; }
    public int[] Friends { get; set; } = [];
    
    public Beatmap? LastPlayedBeatmap { get; set; }
    public int LastPlayedBeatmapExitIndex { get; set; }
}
namespace BanchoNET.Core.Models.Api.Player;

public class RankHistory
{
    public string Mode { get; set; } = "osu"; //TODO same as playmode
    public int[] Data { get; set; } = [];
}
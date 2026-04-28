using BanchoNET.Core.Models.Api.Player;

namespace BanchoNET.Core.Models.Api;

public class RankingsResponse
{
    public Cursor Cursor { get; set; } = new();
    public string CursorString { get; set; } = string.Empty;
    public List<Statistics> Ranking { get; set; } = [];
    public int Total { get; set; }
}

public class Cursor
{
    public int Page { get; set; }
}
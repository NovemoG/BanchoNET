using BanchoNET.Core.Models.Api.Player;

namespace BanchoNET.Core.Models.Api;

public class SearchResultResponse
{
    public List<BasicApiPlayer> Data { get; set; } = [];
    public int Total { get; set; }
}
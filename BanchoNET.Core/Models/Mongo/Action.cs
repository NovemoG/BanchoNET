namespace BanchoNET.Core.Models.Mongo;

public enum Action : byte
{
    MatchCreated,
    MatchDisbanded,
    Joined,
    Left,
    HostChanged,
}
namespace BanchoNET.Models.Mongo;

public enum Action : byte
{
    MatchCreated,
    MatchDisbanded,
    Joined,
    Left,
    HostChanged,
}
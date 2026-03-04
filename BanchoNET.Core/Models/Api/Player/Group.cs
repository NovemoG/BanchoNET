namespace BanchoNET.Core.Models.Api.Player;

public class Group
{
    public required string Colour { get; set; }
    public bool HasListing { get; set; }
    public bool HasPlaymodes { get; set; }
    public int Id { get; set; }
    public required string Identifier { get; set; }
    public bool IsProbationary { get; set; }
    public required string Name { get; set; }
    public required string ShortName { get; set; }
    public string[]? Playmodes { get; set; }
}
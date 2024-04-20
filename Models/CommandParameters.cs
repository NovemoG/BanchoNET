using BanchoNET.Objects.Players;

namespace BanchoNET.Models;

public class CommandParameters
{
    public required Player Player { get; init; }
    public string? CommandBase { get; init; }
}
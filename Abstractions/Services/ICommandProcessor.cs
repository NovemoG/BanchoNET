using BanchoNET.Objects.Channels;
using BanchoNET.Objects.Players;

namespace BanchoNET.Abstractions.Services;

public interface ICommandProcessor
{
    Task<(bool ToPlayer, string Response)> Execute(string command, Player player, Channel? channel = null);
}
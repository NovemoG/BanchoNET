using BanchoNET.Core.Models.Channels;
using BanchoNET.Core.Models.Players;

namespace BanchoNET.Core.Abstractions.Services;

public interface ICommandProcessor
{
    Task<(bool ToPlayer, string Response)> Execute(string command, Player player, Channel? channel = null);
}
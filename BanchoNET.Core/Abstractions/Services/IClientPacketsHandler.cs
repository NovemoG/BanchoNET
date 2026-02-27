using BanchoNET.Core.Models.Players;

namespace BanchoNET.Core.Abstractions.Services;

public interface IClientPacketsHandler
{
    Task ReadPackets(Stream stream, User player);
}
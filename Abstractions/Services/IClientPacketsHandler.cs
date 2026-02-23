using BanchoNET.Objects.Players;

namespace BanchoNET.Abstractions.Services;

public interface IClientPacketsHandler
{
    Task ReadPackets(Stream stream, Player player);
}
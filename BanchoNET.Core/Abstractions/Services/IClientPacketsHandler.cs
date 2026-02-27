using BanchoNET.Core.Models.Users;

namespace BanchoNET.Core.Abstractions.Services;

public interface IClientPacketsHandler
{
    Task ReadPackets(Stream stream, User player);
}
using BanchoNET.Core.Models.Users;

namespace BanchoNET.Core.Abstractions.Bancho.Coordinators;

public interface IPlayerCoordinator : ICoordinator
{
    bool LogoutPlayer(User player);
}
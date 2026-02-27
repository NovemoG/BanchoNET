using BanchoNET.Core.Models.Players;

namespace BanchoNET.Core.Abstractions.Bancho.Coordinators;

public interface IPlayerCoordinator : ICoordinator
{
    bool LogoutPlayer(User player);

    public bool AddSpectator(
        User host,
        User target
    );
    public void RemoveSpectator(
        User host,
        User target
    );
}
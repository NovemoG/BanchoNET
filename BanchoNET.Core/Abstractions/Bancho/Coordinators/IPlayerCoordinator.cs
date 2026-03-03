using BanchoNET.Core.Models.Players;

namespace BanchoNET.Core.Abstractions.Bancho.Coordinators;

public interface IPlayerCoordinator : ICoordinator
{
    bool LogoutPlayer(Player player);

    public bool AddSpectator(
        Player host,
        Player target
    );
    public void RemoveSpectator(
        Player host,
        Player target
    );
}
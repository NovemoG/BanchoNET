using BanchoNET.Core.Models.Players;

namespace BanchoNET.Core.Abstractions.Bancho.Coordinators;

public interface IPlayerCoordinator : ICoordinator
{
    Task<bool> LogoutPlayer(Player player);

    public bool AddSpectator(
        Player host,
        Player target
    );
    public void RemoveSpectator(
        Player host,
        Player target
    );
}
using BanchoNET.Core.Abstractions.HubClients.Multiplayer;
using BanchoNET.Core.Abstractions.HubClients.Multiplayer.RankedPlay;

namespace BanchoNET.Core.Abstractions.HubClients;

public interface IRankedPlayClient
{
    Task RankedPlayCardAdded(
        int userId,
        RankedPlayCardItem card
    );

    Task RankedPlayCardRemoved(
        int userId,
        RankedPlayCardItem card
    );

    Task RankedPlayCardRevealed(
        RankedPlayCardItem card,
        MultiplayerPlaylistItem item
    );

    Task RankedPlayCardPlayed(
        RankedPlayCardItem card
    );
}
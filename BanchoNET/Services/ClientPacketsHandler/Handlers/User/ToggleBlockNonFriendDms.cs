using BanchoNET.Core.Models.Players;

namespace BanchoNET.Services.ClientPacketsHandler;

public partial class ClientPacketsHandler
{
    private async Task ToggleBlockNonFriendDms(Player player, BinaryReader br)
    {
        var value = br.ReadInt32();
        
        player.PmFriendsOnly = value == 1;
        player.LastActivityTime = DateTime.UtcNow;

        await players.UpdatePlayerPmSetting(player, player.PmFriendsOnly);
    }
}
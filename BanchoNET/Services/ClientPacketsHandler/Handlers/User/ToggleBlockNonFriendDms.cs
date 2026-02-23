using BanchoNET.Core.Models.Players;

namespace BanchoNET.Services.ClientPacketsHandler;

public partial class ClientPacketsHandler
{
    private Task ToggleBlockNonFriendDms(Player player, BinaryReader br)
    {
        var value = br.ReadInt32();
        
        player.PmFriendsOnly = value == 1;
        player.LastActivityTime = DateTime.Now;

        return Task.CompletedTask;
    }
}
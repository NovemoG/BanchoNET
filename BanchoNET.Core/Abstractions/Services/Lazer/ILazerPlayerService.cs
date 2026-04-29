using BanchoNET.Core.Models.Api.Player;
using BanchoNET.Core.Models.Players;

namespace BanchoNET.Core.Abstractions.Services.Lazer;

public interface ILazerPlayerService
{
    bool AddPlayer(ApiPlayer player);
    void AssignFriends(int userId, int[] friends);
    bool RemovePlayer(int userId);
    LazerPlayer? GetPlayer(int userId);
}
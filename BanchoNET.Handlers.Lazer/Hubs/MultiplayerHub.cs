using BanchoNET.Core.Abstractions.HubClients;
using BanchoNET.Core.Abstractions.HubClients.Multiplayer;
using BanchoNET.Core.Abstractions.HubClients.Multiplayer.Match;
using BanchoNET.Core.Abstractions.HubClients.Multiplayer.Matchmaking;
using BanchoNET.Core.Abstractions.HubClients.Multiplayer.MultiplayerRooms;
using BanchoNET.Core.Abstractions.HubClients.Multiplayer.RankedPlay;
using BanchoNET.Core.Models.Api;
using Novelog.Abstractions;

namespace BanchoNET.Handlers.Lazer.Hubs;

public class MultiplayerHub(ILogger logger) : BaseHub<IMultiplayerClient>(logger)
{
    #region RankedPlay

    public async Task DiscardCards(
        RankedPlayCardItem[] cards
    ) {
        
    }

    public async Task PlayCard(
        RankedPlayCardItem card
    ) {
        
    }

    #endregion

    #region Matchmaking

    public async Task<MatchmakingPool[]> GetMatchmakingPoolsOfType(
        MatchmakingPoolType type
    ) {
        return [];
    }

    public async Task MatchmakingJoinLobby() {
        
    }

    public async Task MatchmakingLeaveLobby() {
        
    }

    public async Task MatchmakingJoinQueue(
        int poolId
    ) {
        
    }

    public async Task MatchmakingLeaveQueue() {
        
    }

    public async Task MatchmakingAcceptInvitation() {
        
    }

    public async Task MatchmakingDeclineInvitation() {
        
    }

    public async Task MatchmakingToggleSelection(
        long playlistItemId
    ) {
        
    }

    public async Task MatchmakingSkipToNextStage() {
        
    }
    
    #endregion

    #region Lounge

    public async Task<MultiplayerRoom> CreateRoom(
        MultiplayerRoom room
    ) {
        return room;
    }

    public async Task<MultiplayerRoom> JoinRoom(
        long roomId
    ) {
        return new MultiplayerRoom();
    }

    public async Task<MultiplayerRoom> JoinRoomWithPassword(
        long roomId,
        string password
    ) {
        return new MultiplayerRoom();
    }

    #endregion

    #region Multiplayer

    public async Task LeaveRoom() {
        
    }

    public async Task TransferHost(
        int userId
    ) {
        
    }

    public async Task KickUser(
        int userId
    ) {
        
    }

    public async Task ChangeSettings(
        MultiplayerRoomSettings settings
    ) {
        
    }

    public async Task ChangeState(
        MultiplayerUserState newState
    ) {
        
    }

    public async Task ChangeBeatmapAvailability(
        BeatmapAvailability newBeatmapAvailability
    ) {
        
    }

    public async Task ChangeUserStyle(
        int? beatmapId,
        int? rulesetId
    ) {
        
    }

    public async Task ChangeUserMods(
        IEnumerable<ApiMod> newMods
    ) {
        
    }

    public async Task SendMatchRequest(
        MatchUserRequest request
    ) {
        
    }

    public async Task StartMatch() {
        
    }

    public async Task AbortMatch() {
        
    }

    public async Task AbortGameplay() {
        
    }

    public async Task AddPlaylistItem(
        MultiplayerPlaylistItem item
    ) {
        
    }

    public async Task EditPlaylistItem(
        MultiplayerPlaylistItem item
    ) {
        
    }

    public async Task RemovePlaylistItem(
        long playlistItemId
    ) {
        
    }

    public async Task VoteToSkipIntro() {
        
    }

    public async Task InvitePlayer(
        int userId
    ) {
        
    }

    #endregion
}
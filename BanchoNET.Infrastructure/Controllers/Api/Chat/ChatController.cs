using BanchoNET.Core.Abstractions.Bancho.Services;
using BanchoNET.Core.Abstractions.Repositories;
using BanchoNET.Core.Abstractions.Services;
using BanchoNET.Core.Abstractions.Services.Lazer;
using BanchoNET.Core.Attributes;
using BanchoNET.Core.Models.Api.Chat;
using BanchoNET.Core.Models.Auth;
using BanchoNET.Core.Models.Api.Player;
using BanchoNET.Core.Models.Channels;
using BanchoNET.Core.Utils.Extensions;
using Microsoft.AspNetCore.Mvc;

namespace BanchoNET.Infrastructure.Controllers.Api.Chat;

[Route("api/v2/chat")]
[RequireScope(OAuthScopes.ChatRead)]
public class ChatController(
    IPlayersRepository players,
    ILazerPlayerService playerService,
    IBeatmapHandler beatmaps,
    IChannelService channels,
    IMessagesRepository messages,
    INotifySocketManager notify
) : ApiControllerBase(players, playerService, beatmaps)
{
    [HttpPost("ack")]
    public ActionResult<ChatAckResponse> ChatAck(
        [FromForm] ChatAckRequest? request
    ) {
        if (!User.TryGetUserId(out var uid)) return Unauthorized();
        
        //TODO get silenced players/messages?

        return JsonSnake(new ChatAckResponse());
    }

    [HttpPost("new")]
    [RequireScope(OAuthScopes.ChatWrite)]
    public async Task<ActionResult<ChatNewResponse>> ChatNew(
        [FromForm] ChatNewRequest request
    ) {
        if (!User.TryGetUserId(out var uid)) return Unauthorized();

        var targetId = request.target_id;

        var exists = await Players.PlayerExists(targetId);
        if (!exists) return NotFound();

        var channel = await messages.GetOrAddPmChannel(uid, targetId, loadNav: true);
        var message = await messages.AddMessage(
            request.message,
            uid,
            channel.Id,
            request.is_action,
            loadNav: true
        );

        var channelMessage = new ChannelMessage(message, request.uuid);
        await notify.BroadcastPmMessage(channelMessage, uid, targetId);
        
        // also mark that message as read
        await messages.MarkMessagesAsRead(channel.Id, uid, message.Id);
        
        return JsonSnake(new ChatNewResponse
        {
            Channel = new ChatChannelExtended(channel, uid, message.Id),
            Message = channelMessage,
            NewChannelId = channel.Id
        });
    }

    [HttpGet("updates")]
    public async Task <ActionResult<ChatUpdatesResponse>> ChatUpdates(
        [FromQuery] long since,
        [FromQuery(Name = "includes[]")] string[] includes
    ) {
        if (!User.TryGetUserId(out var uid)) return Unauthorized();

        var channelList = (await messages.GetPmChannels(uid))
            .Select(c => new ChatChannelExtended(c, uid, c.ChannelPlayers.Single(cp => cp.PlayerId == uid).LastReadMessageId))
            .ToList();
        
        return JsonSnake(new ChatUpdatesResponse{ Presence = channelList });
    }

    [HttpGet("channels")]
    public ActionResult<List<ChatChannel>> ChatChannels() {
        var channelList = channels.Channels
            .Where(c => c.Type is ChannelType.Public or ChannelType.Announce)
            .Select(c => new ChatChannel(c))
            .ToList();
        
        return JsonSnake(channelList);
    }

    [HttpPost("channels")]
    [RequireScope(OAuthScopes.ChatWrite)]
    public async Task<ActionResult<ChatPostResponse>> ChatChannelsPost(
        [FromForm] ChatPostRequest request
    ) {
        if (!User.TryGetUserId(out var uid)) return Unauthorized();
        if (request.type != "PM") return BadRequest(); //TODO

        var targetId = (int)request.target_id;
        
        var exists = await Players.PlayerExists(targetId);
        if (!exists) return NotFound();

        var pmChannel = await messages.GetPmChannel(uid, targetId);
        if (pmChannel == null) return JsonSnake(new ChatPostResponse());

        var messagesList = await messages.GetChannelMessages(pmChannel.Id);
        return JsonSnake(new ChatPostResponse(pmChannel, uid, messagesList));
    }

    [HttpGet("channels/{channelId:long}")]
    public async Task<ActionResult<ChannelDetailsResponse>> GetChannelDetails(
        long channelId
    ) {
        if (!User.TryGetUserId(out var uid)) return Unauthorized();
        
        var channel = channels.GetChannel(channelId);
        if (channel != null)
        {
            return JsonSnake(new ChannelDetailsResponse
            {
                Channel = new ChatChannelExtended(channel)
            });
        }

        var pmChannel = await messages.GetPmChannel(channelId);
        if (pmChannel != null)
        {
            var lastReadMessageId = await messages.GetLastReadMessageId(channelId, uid);
            
            return JsonSnake(new ChannelDetailsResponse
            {
                Channel = new ChatChannelExtended(pmChannel, uid, lastReadMessageId),
                Users = pmChannel.ChannelPlayers.Select(p => new BasicApiPlayer(p.Player)).ToList()
            });
        }

        return NotFound();
    }

    [HttpGet("channels/{channelId:long}/messages")]
    public async Task<ActionResult<List<ChannelMessage>>> ChatMessages(
        long channelId
    ) {
        var messagesList = await messages.GetChannelMessages(channelId);
        var returnMessages = messagesList.Select(m => new ChannelMessage(m));
        
        return JsonSnake(returnMessages);
    }
    
    [HttpPost("channels/{channelId:long}/messages")]
    [RequireScope(OAuthScopes.ChatWrite)]
    public async Task<ActionResult<ChannelMessage>> ChatMessages(
        long channelId,
        [FromForm] ChatMessageRequest request
    ) {
        if (!User.TryGetUserId(out var uid)) return Unauthorized();
        
        var channel = channels.GetChannel(channelId);
        if (channel != null)
        {
            var message = await messages.AddMessage(
                request.message,
                uid,
                channelId,
                request.is_action,
                loadNav: true
            );
            
            var channelMessage = new ChannelMessage(message, request.uuid);
            await notify.BroadcastToChannel(channelId, channelMessage);
            
            // also mark channel as read
            await messages.MarkMessagesAsRead(channelId, uid, message.Id);
            
            return JsonSnake(channelMessage);
        }
        
        var targetId = await messages.GetTargetOfPmChannel(channelId, uid);
        if (targetId != null)
        {
            var message = await messages.AddMessage(
                request.message,
                uid,
                channelId,
                request.is_action,
                loadNav: true
            );
        
            var channelMessage = new ChannelMessage(message, request.uuid);
            await notify.BroadcastPmMessage(channelMessage, uid, targetId.Value);
            
            // also mark messages as read
            await messages.MarkMessagesAsRead(channelId, uid, message.Id);
        
            return JsonSnake(channelMessage);
        }

        return NotFound();
    }

    [HttpPut("channels/{channelId:long}/users/{userId:int}")]
    [RequireScope(OAuthScopes.ChatWrite)]
    public ActionResult<ChatChannel> PutUserToChannel(
        long channelId,
        int userId
    ) {
        if (!User.TryGetUserId(out var uid)) return Unauthorized();
        if (userId != uid) return BadRequest();
        
        var channel = channels.GetChannel(channelId);
        if (channel == null) return JsonSnake(new ChatChannel());
        
        notify.JoinBroadcast(channelId, userId);
        notify.SendAsync(userId, "chat.channel.join", new ChatChannelExtended(channel), CancellationToken.None);
        
        return new ChatChannel(channel);
    }

    [HttpPut("channels/{channelId:long}/mark-as-read/{messageId:long}")]
    public async Task MarkAsRead(
        long channelId,
        long messageId
    ) {
        if (!User.TryGetUserId(out var uid)) return;
        
        var channel = channels.GetChannel(channelId);
        if (channel != null) return;
        
        var lastMessageId = await messages.GetLastChannelMessageId(channelId);
        if (lastMessageId == null || messageId > lastMessageId) return;
        
        await messages.MarkMessagesAsRead(channelId, uid, messageId);
    }

    [HttpDelete("channels/{channelId:long}/users/{userId:int}")]
    [RequireScope(OAuthScopes.ChatWrite)]
    public void DeleteUserFromChannel(
        long channelId,
        int userId
    ) {
        if (!User.TryGetUserId(out var uid)) return;
        if (userId != uid) return;
        
        var channel = channels.GetChannel(channelId);
        if (channel == null) return;
        
        notify.LeaveBroadcast(channelId, uid);
        notify.SendAsync(userId, "chat.channel.part", new ChatChannelExtended(channel), CancellationToken.None);
    }
}
using BanchoNET.Core.Abstractions.Bancho.Services;
using BanchoNET.Core.Abstractions.Repositories;
using BanchoNET.Core.Abstractions.Services;
using BanchoNET.Core.Models.Api.Chat;
using BanchoNET.Core.Models.Api.Player;
using BanchoNET.Core.Models.Channels;
using BanchoNET.Core.Models.Notify;
using BanchoNET.Core.Utils.Extensions;
using Microsoft.AspNetCore.Mvc;

namespace BanchoNET.Infrastructure.Controllers.Api;

[Route("api/v2/chat")]
public class ChatController(
    IAuthService auth,
    IPlayersRepository players,
    IBeatmapsRepository beatmaps,
    IChannelService channels,
    IMessagesRepository messages,
    INotifySocketManager notify
) : ApiController(auth, players, beatmaps)
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
    public async Task<ActionResult<ChatNewResponse>> ChatNew(
        [FromForm] ChatNewRequest request
    ) {
        if (!User.TryGetUserId(out var uid)) return Unauthorized();

        var targetId = request.target_id;

        var exists = await Players.PlayerExists(targetId);
        if (!exists) return NotFound();

        var channel = await messages.GetOrAddPmChannel(uid, targetId);
        var message = await messages.AddMessage(
            request.message,
            uid,
            channel.Id,
            receiverId: targetId,
            read: false,
            request.is_action,
            loadNav: true
        );
        
        return JsonSnake(new ChatNewResponse
        {
            Channel = new ChatChannelExtended(channel, uid),
            Message = new ChannelMessage(message, request.uuid),
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
            .Select(c => new ChatChannelExtended(c, uid))
            .ToList();
        
        return JsonSnake(new ChatUpdatesResponse{ Presence = channelList });
    }

    [HttpGet("channels")]
    public ActionResult<List<ChatChannel>> ChatChannels() {
        if (!User.TryGetUserId(out _)) return Unauthorized();
        
        var channelList = channels.Channels
            .Where(c => c.Type is ChannelType.Public or ChannelType.Announce)
            .Select(c => new ChatChannel(c))
            .ToList();
        
        return JsonSnake(channelList);
    }

    [HttpPost("channels")]
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
            return JsonSnake(new ChannelDetailsResponse
            {
                Channel = new ChatChannelExtended(pmChannel, uid),
                Users = pmChannel.Players.Select(p => new BasicApiPlayer(p)).ToList()
            });
        }

        return NotFound();
    }

    [HttpGet("channels/{channelId:long}/messages")]
    public async Task<ActionResult<List<ChannelMessage>>> ChatMessages(
        long channelId
    ) {
        if (!User.TryGetUserId(out _)) return Unauthorized();
        
        var messagesList = await messages.GetChannelMessages(channelId);
        var returnMessages = messagesList.Select(m => new ChannelMessage(m));
        
        return JsonSnake(returnMessages);
    }
    
    [HttpPost("channels/{channelId:long}/messages")]
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
                receiverId: null,
                read: true,
                request.is_action,
                loadNav: true
            );
            
            var channelMessage = new ChannelMessage(message, request.uuid);
            await notify.BroadcastToChannel(channelId, channelMessage);
            
            return JsonSnake(channelMessage);
        }
        
        var targetId = await messages.GetTargetOfPmChannel(channelId, uid);
        if (targetId != null)
        {
            var message = await messages.AddMessage(
                request.message,
                uid,
                channelId,
                targetId.Value,
                read: false,
                request.is_action,
                loadNav: true
            );
        
            var channelMessage = new ChannelMessage(message, request.uuid);
            await notify.BroadcastPmMessage(channelMessage, uid, targetId.Value);
        
            return JsonSnake(channelMessage);
        }

        return NotFound();
    }

    [HttpPut("channels/{channelId:long}/users/{userId:int}")]
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
        if (!User.TryGetUserId(out _)) return;
        
        //TODO
    }

    [HttpDelete("channels/{channelId:long}/users/{userId:int}")]
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
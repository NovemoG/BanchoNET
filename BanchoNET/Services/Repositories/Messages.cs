using BanchoNET.Core.Abstractions.Repositories;
using BanchoNET.Core.Models.Channels;
using BanchoNET.Core.Models.Db;
using BanchoNET.Core.Models.Dtos;
using Microsoft.EntityFrameworkCore;

namespace BanchoNET.Services.Repositories;

public class MessagesRepository(BanchoDbContext dbContext) : IMessagesRepository
{
    public async Task<MessageDto?> GetMessage(long id)
    {
        return await dbContext.Messages
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.Id == id);
    }

    public async Task<ChannelDto> GetOrAddPmChannel(
        int senderId,
        int receiverId,
        bool loadNav = false
    ) {
        var channel = await dbContext.Channels
            .AsNoTracking()
            .SingleOrDefaultAsync(c =>
                c.Type == ChannelType.PM
                && c.ChannelPlayers.Any(p => p.PlayerId == senderId)
                && c.ChannelPlayers.Any(p => p.PlayerId == receiverId)
            );

        if (channel != null) return channel;

        channel = new ChannelDto
        {
            Name = "PM",
            Description = "",
            Type = ChannelType.PM,
            ChannelPlayers =
            {
                new ChannelPlayer { PlayerId = senderId, LastReadMessageId = null },
                new ChannelPlayer { PlayerId = receiverId, LastReadMessageId = null }
            }
        };
        
        dbContext.Channels.Add(channel);
        await dbContext.SaveChangesAsync();

        if (loadNav)
            await dbContext.Entry(channel)
                .Collection(c => c.ChannelPlayers)
                .Query()
                .Include(cp => cp.Player)
                .LoadAsync();

        return channel;
    }

    public async Task<List<ChannelDto>> GetPmChannels(
        int playerId
    ) {
        return await dbContext.Channels
            .AsNoTracking()
            .Include(c => c.ChannelPlayers)
            .ThenInclude(c => c.Player)
            .Where(c =>
                c.Type == ChannelType.PM
                && c.ChannelPlayers.Any(p => p.PlayerId == playerId)
            ).ToListAsync();
    }

    public async Task<int?> GetTargetOfPmChannel(
        long channelId,
        int senderId
    ) {
        return await dbContext.Channels
            .AsNoTracking()
            .Where(c =>
                c.Id == channelId
                && c.Type == ChannelType.PM)
            .Select(c => c.ChannelPlayers
                .Where(p => p.PlayerId != senderId)
                .Select(p => (int?)p.PlayerId)
                .FirstOrDefault())
            .FirstOrDefaultAsync();
    }

    public async Task<ChannelDto?> GetPmChannel(
        int senderId,
        int receiverId
    ) {
        return await dbContext.Channels
            .AsNoTracking()
            .Include(c => c.ChannelPlayers)
            .ThenInclude(cp => cp.Player)
            .Where(c =>
                c.Type == ChannelType.PM
                && c.ChannelPlayers.Any(p => p.PlayerId == senderId)
                && c.ChannelPlayers.Any(p => p.PlayerId == receiverId)
            ).FirstOrDefaultAsync();
    }
    
    public async Task<ChannelDto?> GetPmChannel(long id)
    {
        return await dbContext.Channels
            .AsNoTracking()
            .Include(c => c.ChannelPlayers)
            .ThenInclude(cp => cp.Player)
            .FirstOrDefaultAsync(c => c.Id == id);
    }

    public async Task<MessageDto> AddMessage(
        string message,
        int senderId,
        long channelId,
        bool isAction = false,
        bool loadNav = false
    ) {
        var newMessage = new MessageDto
        {
            SenderId = senderId,
            ChannelId = channelId,
            Message = message,
            IsAction = isAction,
            SentAt = DateTime.UtcNow
        };
        
        dbContext.Messages.Add(newMessage);
        await dbContext.SaveChangesAsync();
        
        await dbContext.Channels
            .Where(c => c.Id == channelId)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(c => c.LastMessageId, newMessage.Id));

        if (loadNav)
            await dbContext.Entry(newMessage)
                .Reference(m => m.Sender)
                .LoadAsync();

        return newMessage;
    }

    public async Task<List<MessageDto>> GetChannelMessages(
        long channelId
    ) {
        var messages = await dbContext.Messages
            .AsNoTracking()
            .Include(m => m.Sender)
            .Where(m => m.ChannelId == channelId)
            .OrderByDescending(m => m.SentAt)
            .Take(50)
            .ToListAsync();

        messages.Reverse();
        return messages;
    }

    public async Task<long?> GetLastChannelMessageId(
        long channelId
    ) {
        return await dbContext.Channels
            .AsNoTracking()
            .Where(c => c.Id == channelId)
            .Select(c => c.LastMessageId)
            .FirstOrDefaultAsync();
    }

    public async Task<long?> GetLastReadMessageId(
        long channelId,
        int userId
    ) {
        return await dbContext.ChannelPlayers
            .AsNoTracking()
            .Where(c => c.ChannelId == channelId && c.PlayerId == userId)
            .Select(c => c.LastReadMessageId)
            .FirstOrDefaultAsync();
    }
    
    public async Task<List<MessageDto>> GetUnreadMessages(int playerId)
    {
        return await dbContext.Messages
            .AsNoTracking()
            .Include(m => m.Sender)
            .Where(m =>
                dbContext.ChannelPlayers.Any(cp =>
                    cp.PlayerId == playerId
                    && cp.ChannelId == m.ChannelId
                    && (cp.LastReadMessageId == null || m.Id > cp.LastReadMessageId)))
            .ToListAsync();
    }

    public async Task MarkMessagesAsRead(
        long channelId,
        int userId,
        long messageId
    ) {
        await dbContext.ChannelPlayers
            .Where(c => c.ChannelId == channelId
                        && c.PlayerId == userId
                        && c.LastReadMessageId < messageId)
            .ExecuteUpdateAsync(p => p.SetProperty(c => c.LastReadMessageId, messageId));
    }
    
    public async Task DeleteMessage(long id)
    {
        await dbContext.Messages.Where(m => m.Id == id).ExecuteDeleteAsync();
    }
    
    public async Task DeletePlayerSentMessages(int playerId)
    {
        await dbContext.Messages.Where(m => m.SenderId == playerId).ExecuteDeleteAsync();
    }
}
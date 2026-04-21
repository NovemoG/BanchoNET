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
        int receiverId
    ) {
        var channel = await dbContext.Channels
            .AsNoTracking()
            .SingleOrDefaultAsync(c =>
                c.Type == ChannelType.PM
                && c.Players.Any(p => p.Id == senderId)
                && c.Players.Any(p => p.Id == receiverId)
            );

        if (channel != null) return channel;
        
        var p1 = new PlayerDto { Id = senderId };
        var p2 = new PlayerDto { Id = receiverId };
            
        dbContext.AttachRange(p1, p2);

        channel = new ChannelDto
        {
            Name = "PM",
            Description = "",
            Type = ChannelType.PM,
            Players = new List<PlayerDto> { p1, p2 }
        };
        
        dbContext.Channels.Add(channel);
        await dbContext.SaveChangesAsync();
        
        await dbContext.Entry(channel)
            .Collection(c => c.Players)
            .LoadAsync();

        return channel;
    }

    public async Task<List<ChannelDto>> GetPmChannels(
        int playerId
    ) {
        return await dbContext.Channels
            .AsNoTracking()
            .Include(c => c.Players)
            .Where(c =>
                c.Type == ChannelType.PM
                && c.Players.Any(p => p.Id == playerId)
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
            .Select(c => c.Players
                .Where(p => p.Id != senderId)
                .Select(p => (int?)p.Id)
                .FirstOrDefault())
            .FirstOrDefaultAsync();
    }

    public async Task<ChannelDto?> GetPmChannel(
        int senderId,
        int receiverId
    ) {
        return await dbContext.Channels
            .AsNoTracking()
            .Where(c =>
                c.Type == ChannelType.PM
                && c.Players.Any(p => p.Id == senderId)
                && c.Players.Any(p => p.Id == receiverId)
            ).FirstOrDefaultAsync();
    }
    
    public async Task<ChannelDto?> GetPmChannel(long id)
    {
        return await dbContext.Channels
            .AsNoTracking()
            .Include(c => c.Players)
            .FirstOrDefaultAsync(c => c.Id == id);
    }

    public async Task<MessageDto> AddMessage(
        string message,
        int senderId,
        long channelId,
        int? receiverId = null,
        bool read = false,
        bool isAction = false,
        bool loadNav = false
    ) {
        var newMessage = new MessageDto
        {
            SenderId = senderId,
            ReceiverId = receiverId,
            ChannelId = channelId,
            Message = message,
            IsAction = isAction,
            Read = read,
            SentAt = DateTime.UtcNow
        };
        
        dbContext.Messages.Add(newMessage);
        await dbContext.SaveChangesAsync();
        
        await dbContext.Channels
            .Where(c => c.Id == channelId)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(c => c.LastMessageId, newMessage.Id));

        if (loadNav)
            newMessage.Sender = await dbContext.Players
                .AsNoTracking()
                .FirstAsync(p => p.Id == senderId);
            /*await dbContext.Messages.Entry(newMessage)
                .Reference(m => m.Sender)
                .LoadAsync();*/

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
    
    public async Task<List<MessageDto>> GetUnreadMessages(int playerId)
    {
        return await dbContext.Messages
            .AsNoTracking()
            .Include(m => m.Sender)
            .Include(m => m.Receiver)
            .Where(m => m.ReceiverId == playerId && !m.Read)
            .ToListAsync();
    }
    
    public async Task MarkMessageAsRead(long id)
    {
        await dbContext.Messages.Where(m => m.Id == id)
            .ExecuteUpdateAsync(p => p.SetProperty(m => m.Read, true));
    }
    
    public async Task DeleteMessage(long id)
    {
        await dbContext.Messages.Where(m => m.Id == id).ExecuteDeleteAsync();
    }
    
    public async Task DeletePlayerReceivedMessages(int playerId)
    {
        await dbContext.Messages.Where(m => m.ReceiverId == playerId).ExecuteDeleteAsync();
    }
    
    public async Task DeletePlayerSentMessages(int playerId)
    {
        await dbContext.Messages.Where(m => m.SenderId == playerId).ExecuteDeleteAsync();
    }
}
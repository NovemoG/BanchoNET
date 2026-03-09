using System.Threading.Channels;
using BanchoNET.Core.Abstractions.Services;
using BanchoNET.Core.Models.Dtos;

namespace BanchoNET.Services.LobbyScoresQueue;

public class LobbyScoresQueue : ILobbyScoresQueue
{
    private readonly Channel<MatchScoreRequestDto> _channel = Channel.CreateUnbounded<MatchScoreRequestDto>(new UnboundedChannelOptions
    {
        SingleReader = false,
        SingleWriter = false
    });
    
    public async Task EnqueueJobAsync(MatchScoreRequestDto request)
    {
        await _channel.Writer.WriteAsync(request);
    }
    
    public async Task<MatchScoreRequestDto> ReadJobAsync(CancellationToken ct)
    {
        return await _channel.Reader.ReadAsync(ct);
    }
}
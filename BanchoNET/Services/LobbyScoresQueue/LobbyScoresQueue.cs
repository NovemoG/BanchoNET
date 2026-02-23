using System.Threading.Channels;
using BanchoNET.Core.Abstractions.Services;
using BanchoNET.Core.Models.Dtos;

namespace BanchoNET.Services.LobbyScoresQueue;

public class LobbyScoresQueue : ILobbyScoresQueue
{
    private readonly Channel<ScoreRequestDto> _channel = Channel.CreateUnbounded<ScoreRequestDto>(new UnboundedChannelOptions
    {
        SingleReader = false,
        SingleWriter = false
    });
    
    public async Task EnqueueJobAsync(ScoreRequestDto request)
    {
        await _channel.Writer.WriteAsync(request);
    }
    
    public async Task<ScoreRequestDto> ReadJobAsync(CancellationToken ct)
    {
        return await _channel.Reader.ReadAsync(ct);
    }
}
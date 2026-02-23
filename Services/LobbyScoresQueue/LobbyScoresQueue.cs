using System.Threading.Channels;
using BanchoNET.Abstractions.Services;
using BanchoNET.Models.Dtos;

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
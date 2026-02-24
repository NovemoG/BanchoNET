using BanchoNET.Core.Abstractions.Bancho.Services;
using BanchoNET.Core.Models.Multiplayer;
using Novelog.Abstractions;

namespace BanchoNET.Infrastructure.Bancho.Services;

public sealed class MultiplayerService(ILogger logger) : StatefulService<int, MultiplayerMatch>(logger), IMultiplayerService
{
    
}
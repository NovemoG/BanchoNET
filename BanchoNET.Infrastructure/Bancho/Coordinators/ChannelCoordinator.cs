using BanchoNET.Core.Abstractions.Bancho.Coordinators;
using BanchoNET.Core.Abstractions.Bancho.Services;
using Novelog.Abstractions;

namespace BanchoNET.Infrastructure.Bancho.Coordinators;

public class ChannelCoordinator(
    ILogger logger,
    IChannelService channels,
    IPlayerService players
) : IChannelCoordinator
{
    
}
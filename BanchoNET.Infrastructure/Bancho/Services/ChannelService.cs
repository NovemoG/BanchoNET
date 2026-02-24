using BanchoNET.Core.Abstractions.Bancho.Services;
using BanchoNET.Core.Models.Channels;
using Novelog.Abstractions;

namespace BanchoNET.Infrastructure.Bancho.Services;

public class ChannelService(ILogger logger) : StatefulService<string, Channel>(logger), IChannelService
{
    
}
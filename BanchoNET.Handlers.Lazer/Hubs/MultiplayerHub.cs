using BanchoNET.Core.Abstractions.HubClients;
using Novelog.Abstractions;

namespace BanchoNET.Handlers.Lazer.Hubs;

public class MultiplayerHub(ILogger logger) : BaseHub<IMultiplayerClient>(logger)
{
    
}
using BanchoNET.Core.Abstractions.HubClients;
using Novelog.Abstractions;

namespace BanchoNET.Handlers.Lazer.Hubs;

public class SpectatorHub(ILogger logger) : BaseHub<ISpectatorClient>(logger)
{
    
}
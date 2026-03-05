using BanchoNET.Core.Abstractions.HubClients;
using Novelog.Abstractions;

namespace BanchoNET.Handlers.Lazer.Hubs;

public class MetadataHub(ILogger logger) : BaseHub<IMetadataClient>(logger)
{
    
}
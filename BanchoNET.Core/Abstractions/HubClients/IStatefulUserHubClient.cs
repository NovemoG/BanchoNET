namespace BanchoNET.Core.Abstractions.HubClients;

public interface IStatefulUserHubClient
{
    Task DisconnectRequested();

    /// <summary>
    /// Tells the client this server is going away so it can migrate to another one.
    /// lazer treats it as a hint and defers reconnecting until the user is idle and not
    /// spectating.
    /// </summary>
    Task ServerShuttingDown();
}
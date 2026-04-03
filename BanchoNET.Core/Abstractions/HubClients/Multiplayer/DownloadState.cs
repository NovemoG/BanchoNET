namespace BanchoNET.Core.Abstractions.HubClients.Multiplayer;

public enum DownloadState
{
    Unknown,
    NotDownloaded,
    Downloading,
    Importing,
    LocallyAvailable
}
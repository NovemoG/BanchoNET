using System.Net;

namespace BanchoNET.Abstractions.Repositories;

public interface IClientsRepository
{
    Task InsertLoginData(int playerId, IPAddress ip, DateTime osuVersion, string stream);
    Task<bool> TryInsertClientHashes(
        int playerId,
        string osuPath,
        string adapters,
        string uninstall,
        string diskSerial,
        bool runningUnderWine = false);
}
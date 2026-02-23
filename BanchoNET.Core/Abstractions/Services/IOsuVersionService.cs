using BanchoNET.Core.Models;

namespace BanchoNET.Core.Abstractions.Services;

public interface IOsuVersionService : IInitiable
{
    Task FetchOsuVersion();
    OsuVersion GetLatestVersion(string stream);
}
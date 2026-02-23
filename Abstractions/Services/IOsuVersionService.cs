using BanchoNET.Objects;

namespace BanchoNET.Abstractions.Services;

public interface IOsuVersionService : IInitiable
{
    Task FetchOsuVersion();
    OsuVersion GetLatestVersion(string stream);
}
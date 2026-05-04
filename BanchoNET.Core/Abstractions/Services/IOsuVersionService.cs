using BanchoNET.Core.Models.Stable;

namespace BanchoNET.Core.Abstractions.Services;

public interface IOsuVersionService
{
    OsuVersion GetLatestVersion(string stream);
}
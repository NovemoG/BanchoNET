using BanchoNET.Core.Models;

namespace BanchoNET.Core.Abstractions.Services;

public interface IOsuVersionService
{
    OsuVersion GetLatestVersion(string stream);
}
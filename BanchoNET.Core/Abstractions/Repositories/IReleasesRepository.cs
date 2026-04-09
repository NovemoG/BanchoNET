using BanchoNET.Core.Models;
using BanchoNET.Core.Models.Dtos;

namespace BanchoNET.Core.Abstractions.Repositories;

public interface IReleasesRepository
{
    Task InsertRelease(
        bool prerelease,
        VelopackAsset full,
        VelopackAsset? delta = null
    );

    Task<List<ReleaseDto>> GetReleases(
        int count,
        int offset
    );
}
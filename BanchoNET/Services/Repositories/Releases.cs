using BanchoNET.Core.Abstractions.Repositories;
using BanchoNET.Core.Models;
using BanchoNET.Core.Models.Db;
using BanchoNET.Core.Models.Dtos;
using Microsoft.EntityFrameworkCore;

namespace BanchoNET.Services.Repositories;

public class ReleasesRepository(BanchoDbContext dbContext) : IReleasesRepository
{
    public async Task InsertRelease(
        bool prerelease,
        VelopackAsset full,
        VelopackAsset? delta = null
    ) {
        dbContext.Releases.Add(new ReleaseDto
        {
            Version = full.Version,
            Type = full.Type,
            FileName = full.FileName,
            SHA1 = full.SHA1,
            SHA256 = full.SHA256,
            Size = full.Size,
            Prerelease = prerelease,
        });
        
        if (delta != null)
        {
            dbContext.Releases.Add(new ReleaseDto
            {
                Version = delta.Version,
                Type = delta.Type,
                FileName = delta.FileName,
                SHA1 = delta.SHA1,
                SHA256 = delta.SHA256,
                Size = delta.Size,
                Prerelease = prerelease,
            });
        }
        
        await dbContext.SaveChangesAsync();
    }

    public async Task<List<ReleaseDto>> GetReleases(
        int count,
        int offset
    ) { 
        return await dbContext.Releases
            .OrderByDescending(r => r.PublishedAt)
            .Skip(offset)
            .Take(count)
            .ToListAsync();
    }
}
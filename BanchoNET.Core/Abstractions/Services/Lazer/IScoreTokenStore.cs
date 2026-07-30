using BanchoNET.Core.Models.Api;

namespace BanchoNET.Core.Abstractions.Services.Lazer;

public interface IScoreTokenStore
{
    Task<long> NextTokenId();

    Task Store(
        PendingScore pending
    );

    Task<PendingScore?> Get(
        long tokenId
    );

    Task Remove(
        long tokenId
    );
}
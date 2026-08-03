using Microsoft.AspNetCore.Http;

namespace BanchoNET.Core.Abstractions.Services;

public enum ProfileImageKind
{
    Avatar,
    Cover
}

public readonly record struct ProfileImageResult(
    bool Success,
    string? Extension,
    string? Error
) {
    public static ProfileImageResult Ok(string extension) => new(true, extension, null);
    public static ProfileImageResult Fail(string error) => new(false, null, error);
}

public interface IProfileImageService
{
    Task<ProfileImageResult> Store(int playerId, IFormFile file, ProfileImageKind kind);
}
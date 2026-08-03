using BanchoNET.Core.Abstractions.Services;
using BanchoNET.Core.Utils;
using Microsoft.AspNetCore.Http;

namespace BanchoNET.Infrastructure.Services;

public class ProfileImageService : IProfileImageService
{
    public const long AvatarMaxBytes = 10 * 1024 * 1024;
    public const long CoverMaxBytes = 20 * 1024 * 1024;

    private const int AvatarMaxDimension = 4096;
    private const int CoverMaxWidth = 4000;
    private const int CoverMaxHeight = 1200;

    public async Task<ProfileImageResult> Store(
        int playerId,
        IFormFile file,
        ProfileImageKind kind
    ) {
        var maxBytes = kind == ProfileImageKind.Avatar ? AvatarMaxBytes : CoverMaxBytes;

        if (file.Length == 0)
            return ProfileImageResult.Fail("is empty");

        if (file.Length > maxBytes)
            return ProfileImageResult.Fail($"must be at most {maxBytes / (1024 * 1024)}MB");
        
        var buffer = new byte[file.Length];
        await using (var stream = file.OpenReadStream())
        {
            var read = 0;
            while (read < buffer.Length)
            {
                var chunk = await stream.ReadAsync(buffer.AsMemory(read));
                if (chunk == 0) break;
                read += chunk;
            }

            if (read != buffer.Length)
                return ProfileImageResult.Fail("could not be read");
        }

        if (!ImageFormatSniffer.TrySniff(buffer, out var image))
            return ProfileImageResult.Fail("must be a png, jpeg, gif or webp image");

        if (image.Width <= 0 || image.Height <= 0)
            return ProfileImageResult.Fail("has invalid dimensions");

        var (maxWidth, maxHeight) = kind == ProfileImageKind.Avatar
            ? (AvatarMaxDimension, AvatarMaxDimension)
            : (CoverMaxWidth, CoverMaxHeight);

        if (image.Width > maxWidth || image.Height > maxHeight)
            return ProfileImageResult.Fail($"must be at most {maxWidth}x{maxHeight}");

        WriteAtomically(playerId, kind, image.Extension, buffer);

        return ProfileImageResult.Ok(image.Extension);
    }
    
    private static void WriteAtomically(
        int playerId,
        ProfileImageKind kind,
        string extension,
        byte[] content
    ) {
        var temp = Storage.GetTempUploadPath(extension);

        try
        {
            File.WriteAllBytes(temp, content);

            var existing = kind == ProfileImageKind.Avatar
                ? Storage.GetAvatarFiles(playerId)
                : Storage.GetProfileCoverFiles(playerId);

            foreach (var old in existing)
                File.Delete(old);

            var destination = kind == ProfileImageKind.Avatar
                ? Storage.GetAvatarPath(playerId, extension)
                : Storage.GetProfileCoverPath(playerId, extension);

            File.Move(temp, destination, overwrite: true);
        }
        finally
        {
            if (File.Exists(temp)) File.Delete(temp);
        }
    }
}
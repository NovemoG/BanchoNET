using BanchoNET.Core.Abstractions.Services;
using BanchoNET.Core.Utils;
using Microsoft.AspNetCore.DataProtection;

namespace BanchoNET.Infrastructure.Services;

public sealed class OsuSessionCookieService(
    IDataProtectionProvider protection,
    string path
) : IOsuSessionCookieService
{
    private readonly IDataProtector _protector = protection.CreateProtector("BanchoNET.OsuSessionCookie.v1");
    private readonly SemaphoreSlim _storeGate = new(1, 1);

    public async Task Load() {
        if (!File.Exists(path)) return;

        var raw = _protector.Unprotect(await File.ReadAllTextAsync(path));
        var (value, expiresAt) = OsuSessionCookie.Parse(raw);
        OsuSessionCookie.Apply(new OsuSessionCookieValue(value, expiresAt));
    }

    public async Task Update(
        string? raw
    ) {
        var (value, expiresAt) = OsuSessionCookie.Parse(raw);
        var encrypted = _protector.Protect(OsuSessionCookie.Format(value, expiresAt));

        await _storeGate.WaitAsync();
        try
        {
            var temporaryPath = path + ".tmp";
            await File.WriteAllTextAsync(temporaryPath, encrypted);
            
            if (!OperatingSystem.IsWindows())
                File.SetUnixFileMode(temporaryPath, UnixFileMode.UserRead | UnixFileMode.UserWrite);
            
            File.Move(temporaryPath, path, overwrite: true);
            OsuSessionCookie.Apply(new OsuSessionCookieValue(value, expiresAt));
        }
        finally
        {
            _storeGate.Release();
        }
    }
    
    public Task Clear() => Update(string.Empty);
}
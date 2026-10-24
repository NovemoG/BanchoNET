namespace BanchoNET.Core.Abstractions.Services;

public interface IOsuSessionCookieService
{
    Task Load();
    Task Update(string? value);
    Task Clear();
}
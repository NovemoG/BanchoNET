using System.Net;
using BanchoNET.Objects.Players;

namespace BanchoNET.Abstractions.Services;

public interface IGeolocService
{
    Task<Geoloc?> GetGeoloc(IHeaderDictionary headers);
    IPAddress GetIp(IHeaderDictionary headers);
}
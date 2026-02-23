using System.Net;
using BanchoNET.Core.Models.Players;
using Microsoft.AspNetCore.Http;

namespace BanchoNET.Core.Abstractions.Services;

public interface IGeolocService
{
    Task<Geoloc?> GetGeoloc(IHeaderDictionary headers);
    IPAddress GetIp(IHeaderDictionary headers);
}
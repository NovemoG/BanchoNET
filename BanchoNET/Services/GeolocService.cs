using System.Net;
using System.Text.Json;
using BanchoNET.Core.Abstractions.Services;
using BanchoNET.Core.Models;
using BanchoNET.Core.Models.Players;
using BanchoNET.Core.Utils;

namespace BanchoNET.Services;

public class GeolocService(HttpClient httpClient) : IGeolocService
{
	public async Task<Geoloc?> GetGeoloc(IHeaderDictionary headers)
	{
		var geoloc = FromCloudflare(headers);
		if (geoloc != null) return geoloc.Value;
		
		geoloc = FromNginx(headers);
		if (geoloc != null) return geoloc.Value;
		
		return await FromIp(GetIp(headers));
	}
	
	public IPAddress GetIp(IHeaderDictionary headers)
	{
		var candidates = new[]
		{
			headers.TryGetValue("CF-Connecting-IP", out var cfIp) ? cfIp.ToString() : null,
			headers.TryGetValue("X-Forwarded-For", out var xff) ? xff.ToString().Split(',')[0].Trim() : null,
			headers.TryGetValue("X-Real-IP", out var xRealIp) ? xRealIp.ToString() : null
		};

		foreach (var candidate in candidates)
		{
			if (string.IsNullOrWhiteSpace(candidate))
				continue;

			if (IPAddress.TryParse(candidate, out var ip))
				return ip;
		}

		return IPAddress.None;
	}

	private static Geoloc? FromCloudflare(IHeaderDictionary headers)
	{
		if (!headers.TryGetValue("CF-IPLatitude", out var latitude) ||
		    !headers.TryGetValue("CF-IPLongitude", out var longitude) ||
		    !headers.TryGetValue("CF-IPCountry", out var country))
		{
			return null;
		}

		var countryCode = country.ToString().ToLower();
		
		return new Geoloc
		{
			Country = new GeolocCountry
			{
				Acronym = countryCode,
				Numeric = CountryMap.CountryCodes[countryCode],
			},
			Longitude = float.Parse(longitude!),
			Latitude = float.Parse(latitude!)
		};
	}
	
	private static Geoloc? FromNginx(IHeaderDictionary headers)
	{
		if (!headers.TryGetValue("X-Latitude", out var latitude) ||
		    !headers.TryGetValue("X-Longitude", out var longitude) ||
		    !headers.TryGetValue("X-Country-Code", out var country))
		{
			return null;
		}

		var countryCode = country.ToString().ToLower();
		
		return new Geoloc
		{
			Country = new GeolocCountry
			{
				Acronym = countryCode,
				Numeric = CountryMap.CountryCodes[countryCode],
			},
			Longitude = float.Parse(longitude!),
			Latitude = float.Parse(latitude!)
		};
	}

	private async Task<Geoloc?> FromIp(IPAddress ip)
	{
		var response = await httpClient.GetAsync($"http://ip-api.com/json/{ip.ToString()}");
		if (!response.IsSuccessStatusCode) return null;
		
		var content = await response.Content.ReadAsStringAsync();
		var model = JsonSerializer.Deserialize<GeolocModel>(content);
		if (model!.Success != "success") return null;
		
		var countryCode = model.CountryCode.ToLower();
		
		return new Geoloc
		{
			Country = new GeolocCountry
			{
				Acronym = countryCode,
				Numeric = CountryMap.CountryCodes[countryCode],
			},
			Longitude = model.Longitude,
			Latitude = model.Latitude
		};
	}
}
using System.Net;
using System.Text.Json;
using BanchoNET.Models;
using BanchoNET.Objects.Players;
using BanchoNET.Utils;

namespace BanchoNET.Services;

public class GeolocService(HttpClient httpClient)
{
	private readonly BanchoSession _session = BanchoSession.Instance;
	
	public async Task<Geoloc?> GetGeoloc(IHeaderDictionary headers)
	{
		var geoloc = FromCloudflare(headers);
		if (geoloc != null) return geoloc.Value;
		
		geoloc = FromNginx(headers);
		if (geoloc != null) return geoloc.Value;
		
		return await FromIp(GetIp(headers));
	}

	private Geoloc? FromCloudflare(IHeaderDictionary headers)
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
			Country = new Country
			{
				Acronym = countryCode,
				Numeric = CountryMap.CountryCodes[countryCode],
			},
			Longitude = float.Parse(longitude!),
			Latitude = float.Parse(latitude!)
		};
	}
	
	private Geoloc? FromNginx(IHeaderDictionary headers)
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
			Country = new Country
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
			Country = new Country
			{
				Acronym = countryCode,
				Numeric = CountryMap.CountryCodes[countryCode],
			},
			Longitude = model.Longitude,
			Latitude = model.Latitude
		};
	}

	private IPAddress GetIp(IHeaderDictionary headers)
	{
		var ipString = "";
		if (headers.TryGetValue("CF-Connecting-IP", out var cfIp))
			ipString = cfIp.ToString();
		else if (headers.TryGetValue("X-Forwarded-For", out var xff))
		{
			var forwarded = xff.ToString().Split(", ");
			
			if (forwarded.Length > 1)
				ipString = forwarded[0];
		}
		else
			ipString = headers["X-Real-IP"].ToString();
		
		var ip = _session.GetCachedIp(ipString);
		return ip ?? _session.CacheIp(ipString);
	}
}
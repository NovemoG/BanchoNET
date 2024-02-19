using System.Text.Json.Serialization;

namespace BanchoNET.Models;

public class GeolocModel
{
	[JsonPropertyName("status")] public string Success { get; set; }
	[JsonPropertyName("countryCode")] public string CountryCode { get; set; }
	[JsonPropertyName("lat")] public float Latitude { get; set; }
	[JsonPropertyName("lon")] public float Longitude { get; set; }
}
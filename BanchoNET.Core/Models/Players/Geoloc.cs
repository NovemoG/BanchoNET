namespace BanchoNET.Core.Models.Players;

public readonly record struct GeolocCountry(string Acronym, int Numeric);

public record struct Geoloc
{
	public float Longitude { get; init; }
	public float Latitude { get; init; }
	public GeolocCountry Country { get; init; } //TODO remove because CountryCode exists
}
﻿namespace BanchoNET.Objects;

public readonly record struct Country(string Acronym, int Numeric);

public record struct Geoloc
{
	public float Longitude { get; init; }
	public float Latitude { get; init; }
	public Country Country { get; init; }
}
namespace BanchoNET.Objects.Players;

public class PlayerPresence
{
	public int PlayerId { get; set; }
	public string Username { get; set; }
	public byte TimeZone { get; set; }
	public byte CountryId { get; set; }
	public byte Privileges { get; set; }
	public float Longitude { get; set; }
	public float Latitude { get; set; }
	public int Rank { get; set; }
}
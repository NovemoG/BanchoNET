namespace BanchoNET.Models;

public class Player
{
	public int Id { get; set; }
	public string Username { get; set; }
	public string PasswordHash { get; set; }
	public byte TimeZone { get; set; }
	public string Country { get; set; }
	public DateTime LastConnectionTime { get; set; }
	public int Privileges { get; set; }
	public bool Restricted { get; set; }
	public int RemainingSilence { get; set; }
	public List<int> Friends { get; set; }
	public bool BotClient { get; set; }
	public string Token { get; set; }
	
	//TODO get gamemode rank
	public int Rank { get; set; }
	
	//TODO Geoloc
	public float Longitude { get; set; }
	public float Latitude { get; set; }
}
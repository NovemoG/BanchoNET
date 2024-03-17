namespace BanchoNET.Objects;

public class OsuVersion
{
	public DateTime Date { get; set; }

	public static bool operator <(OsuVersion ver1, OsuVersion ver2)
	{
		return !(ver1 > ver2);
	}
	public static bool operator >(OsuVersion ver1, OsuVersion ver2)
	{
		return ver1.Date > ver2.Date || ver1.Date == ver2.Date && ver1.Revision > ver2.Revision;
	}

	public static bool operator ==(OsuVersion ver1, OsuVersion ver2)
	{
		return ver1.Date == ver2.Date && ver1.Revision == ver2.Revision;
	}
	
	public static bool operator !=(OsuVersion ver1, OsuVersion ver2)
	{
		return !(ver1 == ver2);
	}
	
	public int Revision { get; set; }
	public string Stream { get; set; }
}
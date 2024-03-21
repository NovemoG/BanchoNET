namespace BanchoNET.Objects;

public class OsuVersion
{
	public DateTime Date { get; init; }
	public int Revision { get; init; }
	public string Stream { get; init; }

	#region Comparison Operators

	public static bool operator >(OsuVersion ver1, OsuVersion ver2)
	{
		return ver1.Date > ver2.Date || ver1.Date == ver2.Date && ver1.Revision > ver2.Revision;
	}
	
	public static bool operator <(OsuVersion ver1, OsuVersion ver2)
	{
		return !(ver1 > ver2);
	}

	public static bool operator ==(OsuVersion ver1, OsuVersion ver2)
	{
		return ver1.Date == ver2.Date && ver1.Revision == ver2.Revision;
	}
	
	public static bool operator !=(OsuVersion ver1, OsuVersion ver2)
	{
		return !(ver1 == ver2);
	}

	#endregion
}
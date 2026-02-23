namespace BanchoNET.Core.Models;

public class OsuVersion
{
	public DateTime Date { get; init; }
	public int Revision { get; init; }
	public string Stream { get; init; } = null!;

	public static OsuVersion Parse(string stream, string version)
	{
		var split = version.Split('.');
		return new OsuVersion
		{
			Date = DateTime.ParseExact(split[0][..8], "yyyyMMdd", null),
			Revision = split.Length > 1 ? int.Parse(split[1]) : 0,
			Stream = stream
		};
	}

	public override string ToString()
	{
		return $"{Date:yyyyMMdd}.{Revision}";
	}

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
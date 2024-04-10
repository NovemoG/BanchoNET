namespace BanchoNET.Objects.Players;

public class LoginData
{
	public string Username { get; set; }
	public string PasswordMD5 { get; set; }
	public OsuVersion OsuVersion { get; set; }
	public byte TimeZone { get; set; }
	public bool DisplayCity { get; set; }
	public bool PmPrivate { get; set; }
	public string OsuPathMD5 { get; set; }
	public string AdaptersString { get; set; }
	public string AdaptersMD5 { get; set; }
	public string UninstallMD5 { get; set; }
	public string DiskSignatureMD5 { get; set; }
}
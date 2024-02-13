namespace BanchoNET.Utils;

public static class StringExtensions
{
	public static string ToLogin(this string username)
	{
		return Regexes.Whitespace.Replace(username, "").ToLower();
	}
	
	public static string MakeSafe(this string name)
	{
		return name.Replace(" ", "_").ToLower();
	}
	
	public static string CreateMD5(this string input)
	{
		var inputBytes = System.Text.Encoding.ASCII.GetBytes(input);
		var hash = System.Security.Cryptography.MD5.HashData(inputBytes);
        
		return Convert.ToHexString(hash).ToLower();
	}
}
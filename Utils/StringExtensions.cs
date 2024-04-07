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

	public static bool IsValidResponse(this string content)
	{
		return content.Length > 2;
	}
	
	public static string SplitNumber(this int number)
	{
		if (number < 1000) return number.ToString();

		var num = Regexes.NumSeparator.Replace(number.ToString(), ",$0");
		
		return num.Length % 4 == 0 ? num[1..] : num;
	}
	
	public static string CreateMD5(this string input)
	{
		var inputBytes = System.Text.Encoding.ASCII.GetBytes(input);
		var hash = System.Security.Cryptography.MD5.HashData(inputBytes);
        
		return Convert.ToHexString(hash).ToLower();
	}
}
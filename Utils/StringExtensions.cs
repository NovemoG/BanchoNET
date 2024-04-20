namespace BanchoNET.Utils;

public static class StringExtensions
{
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

	public static string FirstCharToUpper(this string input)
	{
		if (string.IsNullOrEmpty(input))
			return string.Empty;

		return string.Create(input.Length, input, static (chars, str) =>
		{
			chars[0] = char.ToUpperInvariant(str[0]);
			str.AsSpan(1).CopyTo(chars[1..]);
		});
	}
}
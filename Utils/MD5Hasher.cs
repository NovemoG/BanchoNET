namespace BanchoNET.Utils;

public static class MD5Hasher
{
	public static string CreateMD5(this string input)
    {
        var inputBytes = System.Text.Encoding.ASCII.GetBytes(input);
        var hash = System.Security.Cryptography.MD5.HashData(inputBytes);
        
        return Convert.ToHexString(hash).ToLower();
    }
}
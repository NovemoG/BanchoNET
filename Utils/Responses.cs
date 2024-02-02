namespace BanchoNET.Utils;

public static class Responses
{
	public static void ApplyHeaders(this HttpResponse response)
	{
		response.Headers.Connection = "keep-alive";
		response.Headers["cho-protocol"] = "19";
		response.Headers["cho-server"] = "utopia (https://github.com/NovemoG/BanchoNET)";
	}
}
using Microsoft.AspNetCore.Http;

namespace BanchoNET.Core.Utils.Extensions;

public static class ResponseExtensions
{
	public static void ApplyHeaders(this HttpResponse response)
	{
		response.Headers["Connection"] = "Keep-Alive";
		response.Headers["Keep-Alive"] = "timeout=5, max=1000";
		response.Headers["cho-protocol"] = "19";
		response.Headers["cho-server"] = "utopia (https://github.com/NovemoG/BanchoNET)";
	}
}
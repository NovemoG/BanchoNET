using System.Text;
using Microsoft.AspNetCore.Mvc;

namespace BanchoNET.Utils;

public static class Responses
{
	public static FileContentResult BytesContentResult(byte[] bytes)
	{
		return new FileContentResult(bytes, "application/octet-stream; charset=UTF-8");
	}
	
	public static FileContentResult BytesContentResult(string content)
	{
		return new FileContentResult(Encoding.UTF8.GetBytes(content), "application/octet-stream; charset=UTF-8");
	}
}
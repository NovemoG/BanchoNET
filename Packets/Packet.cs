using Microsoft.AspNetCore.Mvc;

namespace BanchoNET.Packets;

public class Packet : IDisposable
{
	protected readonly MemoryStream DataBuffer;

	protected Packet()
	{
		DataBuffer = new MemoryStream();
	}
	
	public byte[] GetContent()
	{
		return DataBuffer.ToArray();
	}

	public FileContentResult GetContentResult()
	{
		return new FileContentResult(DataBuffer.ToArray(), "application/octet-stream; charset=UTF-8");
	}
	
	public void Dispose()
	{
		Dispose(true);
		GC.SuppressFinalize(this);
	}

	protected virtual void Dispose(bool disposing)
	{
		if (disposing)
			DataBuffer.Dispose();
	}
}
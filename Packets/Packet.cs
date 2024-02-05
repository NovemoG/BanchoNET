using Microsoft.AspNetCore.Mvc;

namespace BanchoNET.Packets;

public class Packet : IDisposable
{
	protected readonly BinaryWriter BinaryWriter;
	protected readonly MemoryStream DataBuffer = new();

	public Packet()
	{
		BinaryWriter = new BinaryWriter(DataBuffer);
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
		DataBuffer.Dispose();
		BinaryWriter.Dispose();
		GC.SuppressFinalize(this);
	}
}
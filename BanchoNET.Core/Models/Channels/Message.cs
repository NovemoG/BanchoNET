namespace BanchoNET.Core.Models.Channels;

public class Message
{
	public required string Sender { get; init; }
	public required string Content { get; init; }
	public required string Destination { get; init; }
	public required int SenderId { get; init; }
}
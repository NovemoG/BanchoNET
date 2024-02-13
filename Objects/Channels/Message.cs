namespace BanchoNET.Objects.Channels;

public class Message
{
	public string Sender { get; init; }
	public string Content { get; init; }
	public string Destination { get; init; }
	public int SenderId { get; init; }
}
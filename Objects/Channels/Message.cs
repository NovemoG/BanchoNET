namespace BanchoNET.Objects.Channels;

public class Message
{
	public string Sender { get; set; }
	public string Content { get; set; }
	public string Destination { get; set; }
	public int SenderId { get; set; }
}
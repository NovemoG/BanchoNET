namespace BanchoNET.Core.Models.Api.Notifications;

public class Details
{
    public string Type { get; set; } = null!;
    public string Title { get; set; } = null!;
    public string Username { get; set; } = null!;
    public string CoverUrl { get; set; } = null!; //TODO
}
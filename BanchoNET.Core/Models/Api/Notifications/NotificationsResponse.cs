namespace BanchoNET.Core.Models.Api.Notifications;

public class NotificationsResponse
{
    public Notification[] Notifications { get; set; } = [];
    public Stack[] Stacks { get; set; } = [];
    public DateTimeOffset Timestamp { get; set; }
    public Type[] Types { get; set; } = [];
    public readonly string NotificationEndpoint = "wss://notify.ppy.sh"; //TODO
}
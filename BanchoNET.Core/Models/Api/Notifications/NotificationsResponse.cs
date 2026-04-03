using BanchoNET.Core.Utils;

namespace BanchoNET.Core.Models.Api.Notifications;

public class NotificationsResponse
{
    public Notification[] Notifications { get; set; } = [];
    public Stack[] Stacks { get; set; } = [];
    public DateTimeOffset Timestamp { get; set; }
    public Type[] Types { get; set; } = [];
    
    private static readonly string _notificationEndpoint = $"wss://notify.{AppSettings.Domain}/notify";
    public string NotificationEndpoint => _notificationEndpoint;
}
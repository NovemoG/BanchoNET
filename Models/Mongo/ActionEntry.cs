namespace BanchoNET.Models.Mongo;

public class ActionEntry
{
    public int PlayerId { get; set; }
    public DateTime Date { get; set; }
    public Action Action { get; set; }
}
namespace BanchoNET.Models.Mongo;

public class ScoreEntry
{
    public int PlayerId { get; set; }
    public int TotalScore { get; set; }
    public bool Failed { get; set; }
    public int Mods { get; set; } // same as lobby mods if not freemods
    public int MaxCombo { get; set; }
    public int Count300 { get; set; }
    public int Count100 { get; set; }
    public int Count50 { get; set; }
    public int CountMisses { get; set; }
    public float Accuracy { get; set; }
}
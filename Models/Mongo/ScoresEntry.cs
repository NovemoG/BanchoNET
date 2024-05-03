﻿namespace BanchoNET.Models.Mongo;

public class ScoresEntry
{
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    
    public byte GameMode { get; set; }
    public byte WinCondition { get; set; }
    public byte LobbyType { get; set; }
    public int LobbyMods { get; set; } // 0 if freemods
    
    public int BeatmapId { get; set; }
    public string BeatmapName { get; set; } = null!; // full name
    
    public List<ScoreEntry> Values { get; set; } = null!;
    public List<ActionEntry> Actions { get; set; } = null!; // Actions are displayed after the map finished
}
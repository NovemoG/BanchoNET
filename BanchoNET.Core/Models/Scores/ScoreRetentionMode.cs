namespace BanchoNET.Core.Models.Scores;

/// <summary>
/// What cleanup does with passed non-best scores once they age out. Failed scores are always
/// removed regardless, and scores linked to a multiplayer game are always kept.
/// </summary>
public enum ScoreRetentionMode
{
    Delete,
    Replays,
    Keep
}
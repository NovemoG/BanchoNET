namespace BanchoNET.Core.Models.Api.Maintenance;

/// <summary>
/// Result of deriving <c>Stats.PeakRank</c> from the recorded rank history.
/// <para>
/// Peak rank was never actually recorded — the guard compared against a default of 0, which
/// no real rank can beat — so every stored value is 0 and the history is the only source.
/// </para>
/// </summary>
public sealed class PeakRankRecomputeReport
{
    public bool DryRun { get; set; }

    /// <summary>Set when a previous run was recorded, in which case nothing was recomputed.</summary>
    public bool AlreadyApplied { get; set; }

    public DateTime? PreviouslyAppliedAt { get; set; }

    public DateTime RunAt { get; set; }

    /// <summary>Stats rows whose peak rank is missing or worse than the best recorded sample.</summary>
    public int RowsAffected { get; set; }

    /// <summary>
    /// Rows whose peak rank was cleared because the player has never played that mode.
    /// The legacy system seeded a rank at registration and sampled every account in the
    /// leaderboard regardless of play, so those ranks reflect nothing the player did.
    /// </summary>
    public int RowsCleared { get; set; }

    public List<string> Warnings { get; set; } = [];
}

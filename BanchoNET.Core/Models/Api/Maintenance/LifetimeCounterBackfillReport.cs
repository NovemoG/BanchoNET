namespace BanchoNET.Core.Models.Api.Maintenance;

/// <summary>
/// Result of reconstructing lifetime play count / replay view totals from the monthly
/// history that <c>ResetPlayersStats</c> used to zero out.
/// </summary>
public sealed class LifetimeCounterBackfillReport
{
    /// <summary>
    /// When true nothing was written and the numbers below are what <i>would</i> change.
    /// </summary>
    public bool DryRun { get; set; }

    /// <summary>
    /// Set when a previous apply already ran. Applying twice would double-count,
    /// so a second apply is refused unless explicitly forced.
    /// </summary>
    public bool AlreadyApplied { get; set; }

    public DateTime? PreviouslyAppliedAt { get; set; }

    public DateTime RunAt { get; set; }

    public List<ModeBackfillReport> Modes { get; set; } = [];

    public int StatsRowsScanned { get; set; }
    public int StatsRowsAffected { get; set; }
    public long PlayCountRecovered { get; set; }
    public long ReplayViewsRecovered { get; set; }

    /// <summary>
    /// Stats rows holding a non-zero counter but with no history document to recover from.
    /// These are players whose counters were reset without a snapshot ever being written
    /// (the monthly job skipped inactive players but reset them anyway) — unrecoverable.
    /// </summary>
    public int RowsMissingHistory { get; set; }

    public List<string> Warnings { get; set; } = [];
}

public sealed class ModeBackfillReport
{
    public byte Mode { get; set; }
    public int StatsRowsScanned { get; set; }
    public int StatsRowsAffected { get; set; }
    public long PlayCountRecovered { get; set; }
    public long ReplayViewsRecovered { get; set; }
    public int RowsMissingHistory { get; set; }
    public int DuplicateHistoryDocuments { get; set; }
}

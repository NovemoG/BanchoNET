namespace BanchoNET.Core.Models.Api.Maintenance;

/// <summary>
/// Result of importing the positional Mongo history arrays into the dated Postgres series.
/// <para>
/// The source arrays carry no timestamps, so dates are reconstructed by counting backwards
/// from the most recent bucket. That is best-effort by construction: it is correct only if
/// no sampling run was ever missed. Everything written after the import is properly dated.
/// </para>
/// </summary>
public sealed class HistoryImportReport
{
    public bool DryRun { get; set; }
    public bool AlreadyImported { get; set; }
    public DateTime? PreviouslyImportedAt { get; set; }
    public DateTime RunAt { get; set; }

    /// <summary>Bucket the newest daily sample was anchored to.</summary>
    public DateOnly DailyAnchor { get; set; }

    /// <summary>Bucket the newest monthly sample was anchored to.</summary>
    public DateOnly MonthlyAnchor { get; set; }

    public List<ModeImportReport> Modes { get; set; } = [];

    public int DocumentsRead { get; set; }
    public int SamplesGenerated { get; set; }
    public int SamplesInserted { get; set; }

    public List<string> Warnings { get; set; } = [];
}

public sealed class ModeImportReport
{
    public byte Mode { get; set; }

    public int RankDocuments { get; set; }
    public int PlayCountDocuments { get; set; }
    public int ReplayViewsDocuments { get; set; }

    public int RankSamples { get; set; }
    public int PlayCountSamples { get; set; }
    public int ReplayViewsSamples { get; set; }

    public int SamplesInserted { get; set; }

    /// <summary>
    /// Players holding more than one document for this mode. Nothing enforces uniqueness in
    /// Mongo; only the first document encountered is imported, the rest are skipped.
    /// </summary>
    public int DuplicatePlayers { get; set; }

    /// <summary>Documents referencing a player id that no longer exists in Postgres.</summary>
    public int OrphanedDocuments { get; set; }
}

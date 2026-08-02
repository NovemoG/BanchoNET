namespace BanchoNET.Core.Models.History;

/// <summary>
/// Bucket size of a history series. A sample's date is always normalised to the start of
/// its bucket, which is what makes a re-run of a sampling job a no-op.
/// </summary>
public enum HistoryGranularity : short
{
    /// <summary>One sample per day, dated at that day.</summary>
    Daily = 0,

    /// <summary>One sample per month, dated at the first of that month.</summary>
    Monthly = 1,
}
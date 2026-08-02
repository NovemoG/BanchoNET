namespace BanchoNET.Core.Models.History;

/// <summary>
/// Activity during a single month, dated at the first of that month.
/// </summary>
public readonly record struct MonthlyCount(
    DateOnly Month,
    int Count
);
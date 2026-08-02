namespace BanchoNET.Core.Abstractions.Services;

public interface IPlayerHistoryCollector
{
    /// <summary>
    /// Samples every tracked mode into <c>PlayerHistories</c>: rank and pp dated today, play count
    /// and replay views dated to the month they describe.
    /// <returns>The number of samples stored.</returns>
    /// </summary>
    Task<int> Collect(CancellationToken ct = default);
}
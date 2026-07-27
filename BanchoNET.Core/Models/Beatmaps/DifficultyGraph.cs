using System.Text.Json.Serialization;

namespace BanchoNET.Core.Models.Beatmaps;

public sealed class DifficultyGraph
{
    [JsonPropertyName("sectionLength")]
    public double SectionLength { get; init; }

    [JsonPropertyName("totalLength")]
    public double TotalLength { get; init; }

    [JsonPropertyName("maxValue")]
    public double MaxValue { get; init; }

    [JsonPropertyName("starRating")]
    public double StarRating { get; init; }

    [JsonPropertyName("maxCombo")]
    public int MaxCombo { get; init; }

    [JsonPropertyName("sections")]
    public IReadOnlyList<DifficultyGraphSection> Sections { get; init; } = [];
}

public sealed class DifficultyGraphSection
{
    [JsonPropertyName("startTime")]
    public double StartTime { get; init; }

    [JsonPropertyName("endTime")]
    public double EndTime { get; init; }

    [JsonPropertyName("starRating")]
    public double StarRating { get; init; }
}

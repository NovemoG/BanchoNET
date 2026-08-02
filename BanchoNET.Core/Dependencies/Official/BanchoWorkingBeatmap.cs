#if OFFICIAL_PP
using osu.Framework.Audio.Track;
using osu.Framework.Graphics.Textures;
using osu.Game.Beatmaps;
using osu.Game.Beatmaps.Formats;
using osu.Game.IO;
using osu.Game.Skinning;

namespace BanchoNET.Core.Dependencies.Official;

internal sealed class BanchoWorkingBeatmap : WorkingBeatmap
{
    private readonly Beatmap beatmap;

    private BanchoWorkingBeatmap(
        Beatmap beatmap,
        int? beatmapId
    ) : base(beatmap.BeatmapInfo, null) {
        this.beatmap = beatmap;

        if (beatmapId.HasValue)
            beatmap.BeatmapInfo.OnlineID = beatmapId.Value;
    }

    public static BanchoWorkingBeatmap FromFile(
        string path,
        int? beatmapId = null
    ) {
        using var stream = File.OpenRead(path);
        using var reader = new LineBufferedReader(stream);

        var decoded = Decoder.GetDecoder<Beatmap>(reader).Decode(reader);

        decoded.BeatmapInfo.Ruleset = OfficialCalculator
            .RulesetFor(decoded.BeatmapInfo.Ruleset.OnlineID)
            .RulesetInfo;

        return new BanchoWorkingBeatmap(decoded, beatmapId);
    }

    protected override IBeatmap GetBeatmap() => beatmap;
    public override Texture? GetBackground() => null;
    protected override Track? GetBeatmapTrack() => null;
    protected override ISkin? GetSkin() => null;
    public override Stream? GetStream(string storagePath) => null;
}
#endif
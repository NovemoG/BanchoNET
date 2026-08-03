import type { BeatmapsetShowBeatmap } from "@/lib/beatmapset-types";
import type { Ruleset } from "@/lib/rankings";

type ParsedBeatmapsetHash = {
  beatmapId: number | null;
  mode: Ruleset | null;
};

const rulesets = new Set<Ruleset>(["osu", "taiko", "fruits", "mania"]);

export function parseBeatmapsetPageHash(hash: string): ParsedBeatmapsetHash {
  const value = hash.startsWith("#") ? hash.slice(1) : hash;
  const [rawMode, rawBeatmapId] = value.split("/");

  const mode = rulesets.has(rawMode as Ruleset) ? (rawMode as Ruleset) : null;
  const parsedBeatmapId = Number.parseInt(rawBeatmapId ?? "", 10);

  return {
    beatmapId: Number.isFinite(parsedBeatmapId) ? parsedBeatmapId : null,
    mode,
  };
}

export function buildBeatmapsetPageHash({
  beatmap,
  beatmapId,
  mode,
}: {
  beatmap?: Pick<BeatmapsetShowBeatmap, "id" | "mode">;
  beatmapId?: number;
  mode?: Ruleset;
}) {
  const resolvedMode = beatmap?.mode ?? mode;
  const resolvedBeatmapId = beatmap?.id ?? beatmapId;

  if (resolvedMode == null || resolvedBeatmapId == null) {
    return "";
  }

  return `#${resolvedMode}/${resolvedBeatmapId}`;
}

export function buildBeatmapsetPageHref({
  beatmap,
  beatmapId,
  beatmapsetId,
  mode,
}: {
  beatmap?: Pick<BeatmapsetShowBeatmap, "id" | "mode">;
  beatmapId?: number;
  beatmapsetId: number;
  mode?: Ruleset;
}) {
  return `/beatmapsets/${beatmapsetId}${buildBeatmapsetPageHash({ beatmap, beatmapId, mode })}`;
}

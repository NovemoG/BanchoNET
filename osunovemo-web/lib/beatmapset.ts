import "server-only";

import { cache } from "react";
import { OsuApiError, fetchOsuApi, fetchOsuApiText } from "@/lib/osu-api";
import type {
  BeatmapLeaderboardResponse,
  BeatmapLeaderboardScore,
  BeatmapsetShowData,
} from "@/lib/beatmapset-types";
import type { RankingType, Ruleset } from "@/lib/rankings";

const fetchBeatmapsetById = cache(async (beatmapsetId: number | string) => {
  return fetchOsuApi<BeatmapsetShowData>(`beatmapsets/${beatmapsetId}`);
});

const fetchBeatmapOsuById = cache(async (beatmapId: number | string) => {
  return fetchOsuApiText(`beatmaps/${beatmapId}/osu`);
});

export async function fetchBeatmapset(beatmapsetId: number | string) {
  return fetchBeatmapsetById(beatmapsetId);
}

export async function fetchBeatmapOsu(beatmapId: number | string) {
  return fetchBeatmapOsuById(beatmapId);
}

function normalizeScoreModSettingValue(value: boolean | number | string) {
  if (typeof value !== "string") {
    return value;
  }

  if (value === "true") {
    return true;
  }

  if (value === "false") {
    return false;
  }

  const numericValue = Number(value);
  return Number.isFinite(numericValue) ? numericValue : value;
}

function normalizeBeatmapLeaderboardScore(score: BeatmapLeaderboardScore): BeatmapLeaderboardScore {
  return {
    ...score,
    mods: score.mods?.map((mod) => ({
      ...mod,
      settings:
        mod.settings == null
          ? undefined
          : Object.fromEntries(
              Object.entries(mod.settings).map(([key, value]) => [
                key,
                normalizeScoreModSettingValue(value),
              ]),
            ),
    })) ?? [],
  };
}

export async function fetchBeatmapLeaderboard(
  beatmapId: number | string,
  mode: Ruleset,
  options?: {
    limit?: number;
    mods?: string[];
    type?: RankingType;
  },
) {
  const mods = [...new Set(options?.mods?.filter((mod) => mod.length > 0) ?? [])].sort();
  const response = await fetchOsuApi<BeatmapLeaderboardResponse>(
    `beatmaps/${beatmapId}/scores`,
    {
      limit: options?.limit ?? 50,
      mode,
      ...(mods.length > 0 ? { "mods[]": mods } : {}),
      type: options?.type ?? "global",
    },
  );

  return {
    ...response,
    scores: response.scores.map(normalizeBeatmapLeaderboardScore),
  };
}

export function isBeatmapsetNotFound(error: unknown) {
  return error instanceof OsuApiError && error.status === 404;
}

export function getBeatmapsetDisplayTitle(beatmapset: BeatmapsetShowData) {
  return beatmapset.title_unicode || beatmapset.title;
}

export function getBeatmapsetDisplayArtist(beatmapset: BeatmapsetShowData) {
  return beatmapset.artist_unicode || beatmapset.artist;
}

export function getSelectedBeatmap(
  beatmapset: BeatmapsetShowData,
  options?: {
    beatmapId?: number | null;
    mode?: Ruleset | null;
  },
) {
  const beatmapId = options?.beatmapId;
  const mode = options?.mode;

  if (beatmapId != null) {
    const exactMatch = beatmapset.beatmaps.find((beatmap) => beatmap.id === beatmapId);
    if (exactMatch != null) {
      return exactMatch;
    }
  }

  if (mode != null) {
    const modeMatch = beatmapset.beatmaps.find((beatmap) => beatmap.mode === mode && !beatmap.convert);
    if (modeMatch != null) {
      return modeMatch;
    }
  }

  return (
    beatmapset.beatmaps.find((beatmap) => !beatmap.convert) ??
    beatmapset.beatmaps[0] ??
    null
  );
}

export async function getSelectedBeatmapOsu(
  beatmapset: BeatmapsetShowData,
  options?: {
    beatmapId?: number | null;
    mode?: Ruleset | null;
  },
): Promise<string | null> {
  const beatmap = getSelectedBeatmap(beatmapset, options);

  if (beatmap == null) {
    return null;
  }

  return fetchBeatmapOsu(beatmap.id);
}

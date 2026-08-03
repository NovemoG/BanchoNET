import "server-only";

import { cache } from "react";
import { fetchOsuApi, type UserSummary } from "@/lib/osu-api";
import { normalizeScores, type Beatmap, type Beatmapset, type Score } from "@/lib/profile";
import { PAGE_SIZE, type Ruleset } from "@/lib/rankings";

type TopPlayBeatmap = Beatmap & {
  accuracy: number;
  ar: number;
  beatmapset_id: number;
  bpm: number;
  checksum: string;
  convert: boolean;
  count_circles: number;
  count_sliders: number;
  count_spinners: number;
  cs: number;
  deleted_at: string | null;
  drain: number;
  hit_length: number;
  is_scoreable: boolean;
  last_updated: string;
  mode_int: number;
  passcount: number;
  playcount: number;
  ranked: number;
  total_length: number;
  user_id: number;
};

type RawTopPlayScore = Omit<Score, "beatmap" | "beatmapset"> & {
  beatmap: TopPlayBeatmap;
  best_id: number | null;
  classic_total_score: number;
  current_user_attributes: {
    pin: {
      is_pinned: boolean;
      score_id: number;
    };
  };
  has_replay: boolean;
  is_perfect_combo: boolean;
  legacy_perfect: boolean;
  preserve: boolean;
  processed: boolean;
  ranked: boolean;
  replay: boolean;
  started_at: string;
  total_score_without_mods: number;
  type: string;
  user: UserSummary;
};

export type TopPlayScore = RawTopPlayScore & {
  beatmapset?: Beatmapset;
};

type TopPlaySummary = {
  totalEntries: number;
  totalPages: number;
};

// Only used when the count endpoint is unreachable, so the pager degrades to a single page rather
// than inventing pages that return nothing.
const FALLBACK_TOP_PLAYS_TOTAL_ENTRIES = 0;

const fetchTopPlaysPageByMode = cache(async (mode: Ruleset, page: number) => {
  if (!Number.isInteger(page) || page < 1) {
    return [];
  }

  return fetchOsuApi<RawTopPlayScore[]>(`rankings/top-plays/${mode}`, { page });
});

const fetchBeatmapsetById = cache(async (beatmapsetId: number) => {
  try {
    return await fetchOsuApi<Beatmapset>(`beatmapsets/${beatmapsetId}`);
  } catch {
    return null;
  }
});

export async function fetchTopPlaysSummary(mode: Ruleset): Promise<TopPlaySummary> {
  let totalEntries = FALLBACK_TOP_PLAYS_TOTAL_ENTRIES;

  try {
    const response = await fetchOsuApi<{ total?: number }>(`rankings/top-plays/${mode}/count`);

    if (typeof response.total === "number" && Number.isFinite(response.total)) {
      totalEntries = Math.max(0, response.total);
    }
  } catch {
    // Leave the fallback; a broken count must not take the whole page down.
  }

  return {
    totalEntries,
    totalPages: Math.ceil(totalEntries / PAGE_SIZE),
  };
}

export async function fetchTopPlaysPage(mode: Ruleset, page: number): Promise<TopPlayScore[]> {
  const scores = normalizeScores(await fetchTopPlaysPageByMode(mode, page));
  const beatmapsetIds = [...new Set(scores.map((score) => score.beatmap.beatmapset_id))];
  const beatmapsets = await Promise.all(
    beatmapsetIds.map(async (beatmapsetId) => [
      beatmapsetId,
      await fetchBeatmapsetById(beatmapsetId),
    ] as const),
  );
  const beatmapsetsById = new Map(beatmapsets);

  return scores.map((score) => ({
    ...score,
    beatmapset: beatmapsetsById.get(score.beatmap.beatmapset_id) ?? undefined,
  }));
}

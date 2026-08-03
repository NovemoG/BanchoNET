import type { NextRequest } from "next/server";
import { fetchBeatmapLeaderboard } from "@/lib/beatmapset";
import { OsuApiError } from "@/lib/osu-api";
import { rankingModes, rankingTypes, type RankingType, type Ruleset } from "@/lib/rankings";

function parseInteger(value: string | null, fallback: number) {
  if (value == null) {
    return fallback;
  }

  const parsed = Number.parseInt(value, 10);
  return Number.isFinite(parsed) ? parsed : fallback;
}

function parseMods(searchParams: URLSearchParams) {
  const rawMods = [...searchParams.getAll("mods[]"), ...searchParams.getAll("mods")];

  return [...new Set(rawMods.map((mod) => mod.trim().toUpperCase()).filter((mod) => mod.length > 0))];
}

export async function GET(
  request: NextRequest,
  context: RouteContext<"/api/beatmaps/[beatmap]/leaderboard">,
) {
  const { beatmap } = await context.params;
  const beatmapId = Number.parseInt(beatmap, 10);

  if (!Number.isFinite(beatmapId)) {
    return Response.json({ error: "Invalid beatmap id." }, { status: 400 });
  }

  const modeParam = request.nextUrl.searchParams.get("mode");
  if (modeParam == null || !rankingModes.includes(modeParam as Ruleset)) {
    return Response.json({ error: "Invalid ruleset." }, { status: 400 });
  }

  const typeParam = request.nextUrl.searchParams.get("type");
  if (typeParam != null && !rankingTypes.includes(typeParam as RankingType)) {
    return Response.json({ error: "Invalid ranking type." }, { status: 400 });
  }

  const limit = Math.max(1, Math.min(100, parseInteger(request.nextUrl.searchParams.get("limit"), 50)));
  const mods = parseMods(request.nextUrl.searchParams);
  const type = (typeParam as RankingType | null) ?? "global";

  try {
    const leaderboard = await fetchBeatmapLeaderboard(beatmapId, modeParam as Ruleset, {
      limit,
      mods,
      type,
    });
    return Response.json(leaderboard);
  } catch (error) {
    if (error instanceof OsuApiError) {
      // The API 404s for a beatmap it has never seen, which is most of the catalogue. That is
      // "no scores here", not a failure, so render an empty board rather than an error message.
      if (error.status === 404) {
        return Response.json({ score_count: 0, scores: [] });
      }

      return Response.json({ error: "Failed to load beatmap leaderboard." }, { status: error.status });
    }

    throw error;
  }
}

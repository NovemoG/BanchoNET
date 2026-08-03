import type { NextRequest } from "next/server";
import { fetchBeatmapDifficultyGraph } from "@/lib/beatmap-difficulty-graph";
import { OsuApiError } from "@/lib/osu-api";
import { rankingModes, type Ruleset } from "@/lib/rankings";

export const runtime = "nodejs";

export async function GET(
  request: NextRequest,
  context: RouteContext<"/api/beatmaps/[beatmap]/difficulty-graph">,
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

  try {
    const graph = await fetchBeatmapDifficultyGraph(beatmapId, modeParam as Ruleset);
    return Response.json(graph);
  } catch (error) {
    if (error instanceof OsuApiError) {
      return Response.json({ error: "Failed to load beatmap difficulty graph." }, { status: error.status });
    }

    return Response.json({ error: "Failed to build beatmap difficulty graph." }, { status: 500 });
  }
}

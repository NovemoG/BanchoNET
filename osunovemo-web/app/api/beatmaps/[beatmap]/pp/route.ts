import type { NextRequest } from "next/server";
import { calculateBeatmapPp } from "@/lib/beatmap-pp-calculator";
import type { BeatmapPpCalculatorRequest } from "@/lib/beatmap-pp-calculator-types";
import { OsuApiError } from "@/lib/osu-api";
import { rankingModes, type Ruleset } from "@/lib/rankings";

export const runtime = "nodejs";

function isRequestBody(value: unknown): value is Partial<BeatmapPpCalculatorRequest> {
  return typeof value === "object" && value !== null;
}

export async function POST(
  request: NextRequest,
  context: RouteContext<"/api/beatmaps/[beatmap]/pp">,
) {
  const { beatmap } = await context.params;
  const beatmapId = Number.parseInt(beatmap, 10);

  if (!Number.isFinite(beatmapId)) {
    return Response.json({ error: "Invalid beatmap id." }, { status: 400 });
  }

  let body: unknown;
  try {
    body = await request.json();
  } catch {
    return Response.json({ error: "Invalid request body." }, { status: 400 });
  }

  if (!isRequestBody(body) || body.mode == null || !rankingModes.includes(body.mode as Ruleset)) {
    return Response.json({ error: "Invalid ruleset." }, { status: 400 });
  }

  try {
    const result = await calculateBeatmapPp(beatmapId, {
      accuracy: body.accuracy,
      combo: body.combo,
      largeTickHits: body.largeTickHits,
      misses: body.misses,
      mode: body.mode as Ruleset,
      mods: body.mods,
      n50: body.n50,
      n100: body.n100,
      n300: body.n300,
      nGeki: body.nGeki,
      nKatu: body.nKatu,
      sliderEndHits: body.sliderEndHits,
    });

    return Response.json(result);
  } catch (error) {
    if (error instanceof OsuApiError) {
      return Response.json({ error: "Failed to load beatmap file." }, { status: error.status });
    }

    return Response.json({ error: "Failed to calculate performance." }, { status: 500 });
  }
}

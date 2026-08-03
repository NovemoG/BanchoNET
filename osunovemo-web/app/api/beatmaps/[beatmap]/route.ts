import {
  fetchBeatmapOsu,
  fetchBeatmapset,
  isBeatmapsetNotFound,
} from "@/lib/beatmapset";

export const dynamic = "force-dynamic";
export const runtime = "nodejs";

type BeatmapRouteContext = {
  params: Promise<{
    beatmap: string;
  }>;
};

function getBeatmapsetIdFromBeatmapFile(value: string) {
  const match = /^BeatmapSetID\s*:\s*(\d+)\s*$/im.exec(value);

  if (match == null) {
    return null;
  }

  const beatmapsetId = Number.parseInt(match[1], 10);

  return Number.isFinite(beatmapsetId) && beatmapsetId > 0 ? beatmapsetId : null;
}

export async function GET(_request: Request, context: BeatmapRouteContext) {
  const { beatmap } = await context.params;
  const beatmapId = Number.parseInt(beatmap, 10);

  if (!Number.isFinite(beatmapId)) {
    return Response.json({ error: "Invalid beatmap id." }, { status: 400 });
  }

  try {
    const beatmapFile = await fetchBeatmapOsu(beatmapId);
    const beatmapsetId = getBeatmapsetIdFromBeatmapFile(beatmapFile);

    if (beatmapsetId == null) {
      return Response.json({ error: "Beatmap not found." }, { status: 404 });
    }

    const beatmapset = await fetchBeatmapset(beatmapsetId);
    const beatmapData = beatmapset.beatmaps.find((candidate) => candidate.id === beatmapId);

    if (beatmapData == null) {
      return Response.json({ error: "Beatmap not found." }, { status: 404 });
    }

    return Response.json({
      beatmap: beatmapData,
      beatmapset,
    });
  } catch (error) {
    if (isBeatmapsetNotFound(error)) {
      return Response.json({ error: "Beatmap not found." }, { status: 404 });
    }

    throw error;
  }
}

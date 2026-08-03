import { fetchBeatmapset, isBeatmapsetNotFound } from "@/lib/beatmapset";

export const dynamic = "force-dynamic";
export const runtime = "nodejs";

type BeatmapsetRouteContext = {
  params: Promise<{
    beatmapset: string;
  }>;
};

export async function GET(_request: Request, context: BeatmapsetRouteContext) {
  const { beatmapset } = await context.params;

  if (!/^\d+$/.test(beatmapset)) {
    return Response.json({ error: "Invalid beatmapset id." }, { status: 400 });
  }

  try {
    return Response.json(await fetchBeatmapset(beatmapset));
  } catch (error) {
    if (isBeatmapsetNotFound(error)) {
      return Response.json({ error: "Beatmapset not found." }, { status: 404 });
    }

    throw error;
  }
}

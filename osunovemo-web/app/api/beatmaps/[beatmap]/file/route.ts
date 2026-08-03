import { fetchBeatmapOsu } from "@/lib/beatmapset";
import { OsuApiError } from "@/lib/osu-api";

export const runtime = "nodejs";

const textHeaders = {
  "Cache-Control": "no-store",
  "Content-Type": "text/plain; charset=utf-8",
};

export async function GET(
  _request: Request,
  context: RouteContext<"/api/beatmaps/[beatmap]/file">,
) {
  const { beatmap } = await context.params;
  const beatmapId = Number.parseInt(beatmap, 10);

  if (!Number.isFinite(beatmapId)) {
    return new Response("Invalid beatmap id.", { headers: textHeaders, status: 400 });
  }

  try {
    const beatmapFile = await fetchBeatmapOsu(beatmapId);
    return new Response(beatmapFile, { headers: textHeaders });
  } catch (error) {
    if (error instanceof OsuApiError) {
      return new Response("Failed to load beatmap file.", {
        headers: textHeaders,
        status: error.status,
      });
    }

    return new Response("Failed to load beatmap file.", {
      headers: textHeaders,
      status: 500,
    });
  }
}

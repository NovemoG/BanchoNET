import type { Metadata } from "next";
import { notFound } from "next/navigation";
import { BeatmapsetShowPage } from "@/components/beatmapsets-show/page";
import { buildBeatmapDifficultyGraphFromContent } from "@/lib/beatmap-difficulty-graph";
import {
  fetchBeatmapOsu,
  fetchBeatmapset,
  getBeatmapsetDisplayArtist,
  getBeatmapsetDisplayTitle,
  getSelectedBeatmap,
  isBeatmapsetNotFound,
} from "@/lib/beatmapset";

type BeatmapsetPageProps = {
  params: Promise<{ beatmapset: string }>;
  searchParams: Promise<Record<string, string | string[] | undefined>>;
};

function getSingleValue(value: string | string[] | undefined) {
  return Array.isArray(value) ? value[0] : value;
}

function parseBeatmapId(value: string | undefined) {
  if (value == null) {
    return null;
  }

  const parsed = Number.parseInt(value, 10);
  return Number.isFinite(parsed) ? parsed : null;
}

export async function generateMetadata({ params }: BeatmapsetPageProps): Promise<Metadata> {
  const { beatmapset } = await params;

  try {
    const data = await fetchBeatmapset(beatmapset);
    return {
      description: `Beatmapset page for ${getBeatmapsetDisplayArtist(data)} - ${getBeatmapsetDisplayTitle(data)}`,
      title: `${getBeatmapsetDisplayArtist(data)} - ${getBeatmapsetDisplayTitle(data)}`,
    };
  } catch {
    return {
      title: "Beatmapset",
    };
  }
}

async function loadBeatmapsetOrNotFound(beatmapset: string) {
  try {
    return await fetchBeatmapset(beatmapset);
  } catch (error) {
    if (isBeatmapsetNotFound(error)) {
      notFound();
    }

    throw error;
  }
}

export default async function BeatmapsetPage({
  params,
  searchParams,
}: BeatmapsetPageProps) {
  const { beatmapset } = await params;
  const resolvedSearchParams = await searchParams;
  const data = await loadBeatmapsetOrNotFound(beatmapset);
  const beatmapId = parseBeatmapId(getSingleValue(resolvedSearchParams.beatmap));
  const currentBeatmap = getSelectedBeatmap(data, { beatmapId });

  if (!currentBeatmap) {
    notFound();
  }

  let initialDifficultyGraph = null;
  let initialBeatmapFile = null;

  try {
    initialBeatmapFile = await fetchBeatmapOsu(currentBeatmap.id);
  } catch {
    initialBeatmapFile = null;
  }

  try {
    if (initialBeatmapFile != null) {
      initialDifficultyGraph = buildBeatmapDifficultyGraphFromContent(initialBeatmapFile, currentBeatmap.mode);
    }
  } catch {
    initialDifficultyGraph = null;
  }

  return (
    <BeatmapsetShowPage
      beatmapset={data}
      initialBeatmapId={currentBeatmap.id}
      initialBeatmapFile={initialBeatmapFile}
      initialDifficultyGraph={initialDifficultyGraph}
      initialDifficultyGraphMode={currentBeatmap.mode}
    />
  );
}

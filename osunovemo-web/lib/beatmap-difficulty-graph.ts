import "server-only";

import { cache } from "react";
import * as rosu from "rosu-pp-js";
import { fetchBeatmapOsu } from "@/lib/beatmapset";
import type { BeatmapDifficultyGraphResponse, BeatmapDifficultyGraphSegment } from "@/lib/beatmapset-types";
import type { Ruleset } from "@/lib/rankings";

const difficultyGraphRawSegmentCount = 50;
const difficultyGraphColors = [
  "rgb(102, 204, 255)",
  "rgb(68, 170, 221)",
  "rgb(17, 136, 170)",
  "rgb(48, 61, 71)",
] as const;

function getFallbackDifficultyGraph(): BeatmapDifficultyGraphResponse {
  return {
    rawSegmentCount: difficultyGraphRawSegmentCount,
    segments: [
      {
        color: difficultyGraphColors[0],
        height: 1,
        span: difficultyGraphRawSegmentCount,
      },
    ],
  };
}

function getRosuGameMode(mode: Ruleset) {
  switch (mode) {
    case "taiko":
      return rosu.GameMode.Taiko;
    case "fruits":
      return rosu.GameMode.Catch;
    case "mania":
      return rosu.GameMode.Mania;
    default:
      return rosu.GameMode.Osu;
  }
}

function mergeStrainChannels(channels: Array<Float64Array | undefined>) {
  const availableChannels = channels.filter((channel): channel is Float64Array => channel != null);

  if (availableChannels.length === 0) {
    return [];
  }

  const segmentCount = Math.max(...availableChannels.map((channel) => channel.length));

  return Array.from({ length: segmentCount }, (_, index) =>
    Math.sqrt(
      availableChannels.reduce((total, channel) => {
        const value = channel[index] ?? 0;
        return total + value * value;
      }, 0),
    ),
  );
}

function getStrainValues(mode: Ruleset, strains: rosu.Strains) {
  switch (mode) {
    case "taiko":
      return mergeStrainChannels([
        strains.color,
        strains.reading,
        strains.rhythm,
        strains.singleColorStamina,
        strains.stamina,
      ]);
    case "fruits":
      return Array.from(strains.movement ?? []);
    case "mania":
      return Array.from(strains.strains ?? []);
    default:
      return mergeStrainChannels([
        strains.aim,
        strains.flashlight,
        strains.speed,
      ]);
  }
}

function bucketStrainValues(values: number[]) {
  if (values.length === 0) {
    return Array.from({ length: difficultyGraphRawSegmentCount }, () => 0);
  }

  return Array.from({ length: difficultyGraphRawSegmentCount }, (_, index) => {
    const start = Math.floor((index / difficultyGraphRawSegmentCount) * values.length);
    const end = Math.max(
      start + 1,
      Math.floor(((index + 1) / difficultyGraphRawSegmentCount) * values.length),
    );

    let peak = 0;

    for (let sourceIndex = start; sourceIndex < Math.min(end, values.length); sourceIndex += 1) {
      peak = Math.max(peak, values[sourceIndex] ?? 0);
    }

    return peak;
  });
}

function getDifficultyLevel(normalizedValue: number) {
  if (normalizedValue >= 0.75) {
    return 3;
  }

  if (normalizedValue >= 0.5) {
    return 2;
  }

  if (normalizedValue >= 0.25) {
    return 1;
  }

  return 0;
}

function buildDifficultyGraphSegments(rawValues: number[]): BeatmapDifficultyGraphSegment[] {
  const maxValue = Math.max(1, ...rawValues);
  const normalizedValues = rawValues.map((value) => value / maxValue);
  const segments: BeatmapDifficultyGraphSegment[] = [];

  for (const normalizedValue of normalizedValues) {
    const level = getDifficultyLevel(normalizedValue);
    const previousSegment = segments[segments.length - 1];

    if (previousSegment != null && previousSegment.color === difficultyGraphColors[level]) {
      previousSegment.span += 1;
      continue;
    }

    segments.push({
      color: difficultyGraphColors[level],
      height: 1,
      span: 1,
    });
  }

  return segments;
}

export function buildBeatmapDifficultyGraphFromContent(
  beatmapContent: string,
  mode: Ruleset,
): BeatmapDifficultyGraphResponse {
  let beatmap: rosu.Beatmap | undefined;
  let difficulty: rosu.Difficulty | undefined;
  let strains: rosu.Strains | undefined;

  try {
    beatmap = new rosu.Beatmap(beatmapContent);

    if (beatmap.isSuspicious()) {
      return getFallbackDifficultyGraph();
    }

    const targetMode = getRosuGameMode(mode);
    if (beatmap.mode !== targetMode) {
      beatmap.convert(targetMode);
    }

    difficulty = new rosu.Difficulty({ lazer: false });
    strains = difficulty.strains(beatmap);

    const strainValues = getStrainValues(mode, strains);
    const rawValues = bucketStrainValues(strainValues);

    return {
      rawSegmentCount: difficultyGraphRawSegmentCount,
      segments: buildDifficultyGraphSegments(rawValues),
    };
  } finally {
    strains?.free();
    difficulty?.free();
    beatmap?.free();
  }
}

export const fetchBeatmapDifficultyGraph = cache(
  async (beatmapId: number | string, mode: Ruleset): Promise<BeatmapDifficultyGraphResponse> => {
    const beatmapContent = await fetchBeatmapOsu(beatmapId);
    return buildBeatmapDifficultyGraphFromContent(beatmapContent, mode);
  },
);

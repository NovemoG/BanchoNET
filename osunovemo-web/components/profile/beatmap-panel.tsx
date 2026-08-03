import Link from "next/link";
import { Clock3, Gauge, Music2 } from "lucide-react";
import { buildBeatmapsetPageHref } from "@/lib/beatmapset-page-hash";
import { cn } from "@/lib/utils";
import type { Beatmap } from "@/lib/profile";

type BeatmapPanelProps = {
  beatmap: Beatmap;
  beatmapsetId: number;
};

const modeLabels: Record<Beatmap["mode"], string> = {
  fruits: "catch",
  mania: "mania",
  osu: "osu!",
  taiko: "taiko",
};

function formatTime(value: number | null | undefined) {
  if (value == null || value <= 0) {
    return null;
  }

  const minutes = Math.floor(value / 60);
  const seconds = value % 60;
  return `${minutes}:${seconds.toString().padStart(2, "0")}`;
}

function formatDecimal(value: number | null | undefined) {
  if (value == null || !Number.isFinite(value)) {
    return null;
  }

  return value.toFixed(value >= 10 ? 0 : 1);
}

function interpolateColor(start: string, end: string, factor: number) {
  const startValue = Number.parseInt(start.slice(1), 16);
  const endValue = Number.parseInt(end.slice(1), 16);
  const startRgb = {
    r: (startValue >> 16) & 255,
    g: (startValue >> 8) & 255,
    b: startValue & 255,
  };
  const endRgb = {
    r: (endValue >> 16) & 255,
    g: (endValue >> 8) & 255,
    b: endValue & 255,
  };
  const channel = (from: number, to: number) =>
    Math.round(from + (to - from) * factor)
      .toString(16)
      .padStart(2, "0");

  return `#${channel(startRgb.r, endRgb.r)}${channel(startRgb.g, endRgb.g)}${channel(startRgb.b, endRgb.b)}`;
}

function getStarRatingColor(starRating: number) {
  const stops: Array<[number, string]> = [
    [0, "#aaaaaa"],
    [0.1, "#aaaaaa"],
    [0.1, "#4290fb"],
    [1.25, "#4fc0ff"],
    [2, "#4fffd5"],
    [2.5, "#7cff4f"],
    [3.3, "#f6f05c"],
    [4.2, "#ff8068"],
    [4.9, "#ff4e6f"],
    [5.8, "#c645b8"],
    [6.7, "#6563de"],
    [7.7, "#18158e"],
    [9, "#000000"],
  ];
  const normalized = Math.round(starRating * 100) / 100;

  for (let index = 1; index < stops.length; index += 1) {
    const [previousThreshold, previousColor] = stops[index - 1];
    const [nextThreshold, nextColor] = stops[index];

    if (normalized <= nextThreshold) {
      const range = nextThreshold - previousThreshold;
      const factor = range === 0 ? 0 : (normalized - previousThreshold) / range;
      return interpolateColor(previousColor, nextColor, factor);
    }
  }

  return stops[stops.length - 1][1];
}

export function BeatmapPanel({ beatmap, beatmapsetId }: BeatmapPanelProps) {
  const starRating = Math.max(0, beatmap.difficulty_rating ?? 0);
  const starColor = getStarRatingColor(starRating);
  const duration = formatTime(beatmap.total_length);
  const bpm = formatDecimal(beatmap.bpm);
  const beatmapUrl = buildBeatmapsetPageHref({
    beatmapId: beatmap.id,
    beatmapsetId,
    mode: beatmap.mode,
  });

  return (
    <Link
      className="flex min-w-0 items-center gap-2 rounded-lg border border-white/6 bg-black/15 px-3 py-2 text-sm text-white/85 transition-colors hover:border-white/12 hover:bg-black/25"
      href={beatmapUrl}
    >
      <div className="flex min-w-0 flex-1 items-center gap-2 overflow-hidden">
        <span className="inline-flex h-5 w-5 shrink-0 items-center justify-center rounded-full bg-white/8 text-[10px] font-semibold uppercase text-white/70">
          <i className={`fa-extra-mode-${beatmap.mode}`} />
        </span>
        <span
          className={cn(
            "inline-flex shrink-0 items-center rounded-full px-2 py-0.5 text-[11px] font-semibold",
            starRating >= 6.5 ? "text-white" : "text-black/85",
          )}
          style={{ backgroundColor: starColor }}
        >
          ★ {starRating.toFixed(2)}
        </span>
        <span className="truncate font-medium text-white">{beatmap.version}</span>
      </div>

      <div className="hidden shrink-0 items-center gap-3 text-xs text-white/45 md:flex">
        <span className="inline-flex items-center gap-1">
          <Music2 className="h-3.5 w-3.5" />
          {modeLabels[beatmap.mode]}
        </span>
        {duration ? (
          <span className="inline-flex items-center gap-1">
            <Clock3 className="h-3.5 w-3.5" />
            {duration}
          </span>
        ) : null}
        {bpm ? (
          <span className="inline-flex items-center gap-1">
            <Gauge className="h-3.5 w-3.5" />
            {bpm} BPM
          </span>
        ) : null}
      </div>
    </Link>
  );
}

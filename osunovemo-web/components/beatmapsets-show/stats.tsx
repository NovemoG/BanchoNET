import { Gauge, Heart, Music2, Play, Radio, Timer } from "lucide-react";
import type { BeatmapsetShowBeatmap, BeatmapsetShowData } from "@/lib/beatmapset-types";
import { cn } from "@/lib/utils";

type BeatmapsetStatsProps = {
  beatmap: BeatmapsetShowBeatmap;
  beatmapset: BeatmapsetShowData;
};

const integerFormatter = new Intl.NumberFormat("en-US");
const decimalFormatter = new Intl.NumberFormat("en-US", {
  maximumFractionDigits: 2,
  minimumFractionDigits: 0,
});

function formatInteger(value: number | null | undefined) {
  return integerFormatter.format(Math.round(value ?? 0));
}

function formatDecimal(value: number | null | undefined) {
  return decimalFormatter.format(value ?? 0);
}

function formatTime(value: number | null | undefined) {
  if (value == null || value <= 0) {
    return "0:00";
  }

  const minutes = Math.floor(value / 60);
  const seconds = value % 60;
  return `${minutes}:${seconds.toString().padStart(2, "0")}`;
}

function buildBarWidth(value: number, total: number) {
  if (!Number.isFinite(value) || !Number.isFinite(total) || total <= 0) {
    return "0%";
  }

  return `${Math.max(0, Math.min(100, (value / total) * 100))}%`;
}

function getStatKeys(mode: BeatmapsetShowBeatmap["mode"]) {
  switch (mode) {
    case "mania":
      return ["cs", "drain", "accuracy", "difficulty_rating"] as const;
    case "taiko":
      return ["drain", "accuracy", "difficulty_rating"] as const;
    default:
      return ["cs", "drain", "accuracy", "ar", "difficulty_rating"] as const;
  }
}

function getStatLabel(
  key: ReturnType<typeof getStatKeys>[number],
  mode: BeatmapsetShowBeatmap["mode"],
) {
  if (key === "difficulty_rating") {
    return "Stars";
  }

  if (key === "accuracy" && mode === "mania") {
    return "OD";
  }

  if (key === "cs" && mode === "mania") {
    return "Keys";
  }

  return key.toUpperCase();
}

function getStatValue(
  beatmap: BeatmapsetShowBeatmap,
  key: ReturnType<typeof getStatKeys>[number],
) {
  return beatmap[key];
}

function getStatDisplayValue(
  beatmap: BeatmapsetShowBeatmap,
  key: ReturnType<typeof getStatKeys>[number],
) {
  const value = getStatValue(beatmap, key);
  return key === "difficulty_rating" ? `${formatDecimal(value)}★` : formatDecimal(value);
}

function RatingChart({ ratings }: { ratings: number[] }) {
  const maxValue = Math.max(1, ...ratings);

  return (
    <div className="flex h-18 items-end gap-1">
      {ratings.map((count, index) => (
        <div className="flex min-w-0 flex-1 flex-col items-center gap-1" key={`${index}-${count}`}>
          <div className="flex h-14 w-full items-end">
            <div
              className="w-full rounded-t-sm bg-osu-h1/75"
              style={{ height: buildBarWidth(count, maxValue) }}
            />
          </div>
          <span className="text-[10px] text-white/35">{index + 1}</span>
        </div>
      ))}
    </div>
  );
}

export function BeatmapsetStats({ beatmap, beatmapset }: BeatmapsetStatsProps) {
  const statKeys = getStatKeys(beatmap.mode);
  const ratings = beatmapset.ratings.slice(1);
  const negativeRatings = ratings.slice(0, 4).reduce((sum, value) => sum + value, 0);
  const positiveRatings = ratings.slice(4).reduce((sum, value) => sum + value, 0);
  const totalRatings = Math.max(1, negativeRatings + positiveRatings);
  const objectCount =
    beatmap.count_circles + beatmap.count_sliders + beatmap.count_spinners;

  return (
    <section className="rounded-xl border border-white/8 bg-osu-d5/90 p-4 shadow-[0_18px_36px_rgba(0,0,0,0.24)]">
      <div className="grid grid-cols-2 gap-2">
        <div className="rounded-lg bg-black/20 px-3 py-2">
          <div className="mb-1 flex items-center gap-1 text-[11px] uppercase tracking-[0.08em] text-white/45">
            <Music2 className="h-3.5 w-3.5" />
            Length
          </div>
          <div className="text-sm font-semibold text-white">{formatTime(beatmap.total_length)}</div>
        </div>
        <div className="rounded-lg bg-black/20 px-3 py-2">
          <div className="mb-1 flex items-center gap-1 text-[11px] uppercase tracking-[0.08em] text-white/45">
            <Gauge className="h-3.5 w-3.5" />
            BPM
          </div>
          <div className="text-sm font-semibold text-white">{formatDecimal(beatmap.bpm)}</div>
        </div>
        <div className="rounded-lg bg-black/20 px-3 py-2">
          <div className="mb-1 flex items-center gap-1 text-[11px] uppercase tracking-[0.08em] text-white/45">
            <Radio className="h-3.5 w-3.5" />
            Objects
          </div>
          <div className="text-sm font-semibold text-white">{formatInteger(objectCount)}</div>
        </div>
        <div className="rounded-lg bg-black/20 px-3 py-2">
          <div className="mb-1 flex items-center gap-1 text-[11px] uppercase tracking-[0.08em] text-white/45">
            <Timer className="h-3.5 w-3.5" />
            Max Combo
          </div>
          <div className="text-sm font-semibold text-white">
            {beatmap.max_combo != null ? `${formatInteger(beatmap.max_combo)}x` : "-"}
          </div>
        </div>
      </div>

      <div className="mt-4 overflow-hidden rounded-lg border border-white/6">
        <table className="w-full text-sm">
          <tbody>
            {statKeys.map((key) => {
              const value = getStatValue(beatmap, key);
              return (
                <tr className="border-t border-white/6 first:border-t-0" key={key}>
                  <th className="w-14 px-3 py-2 text-left text-[11px] font-semibold uppercase tracking-[0.08em] text-white/45">
                    {getStatLabel(key, beatmap.mode)}
                  </th>
                  <td className="px-2 py-2">
                    <div className="h-2 overflow-hidden rounded-full bg-white/8">
                      <div
                        className={cn(
                          "h-full rounded-full",
                          key === "difficulty_rating" ? "bg-osu-h1" : "bg-white/45",
                        )}
                        style={{ width: buildBarWidth(value, 10) }}
                      />
                    </div>
                  </td>
                  <td className="w-18 px-3 py-2 text-right font-semibold text-white">
                    {getStatDisplayValue(beatmap, key)}
                  </td>
                </tr>
              );
            })}
          </tbody>
        </table>
      </div>

      <div className="mt-4 rounded-lg bg-black/20 p-3">
        <div className="mb-2 flex items-center justify-between text-xs uppercase tracking-[0.08em] text-white/45">
          <span>User Rating</span>
          <span>{formatDecimal(beatmapset.rating)}</span>
        </div>
        <div className="h-2 overflow-hidden rounded-full bg-white/8">
          <div className="flex h-full">
            <div
              className="bg-white/30"
              style={{ width: buildBarWidth(negativeRatings, totalRatings) }}
            />
            <div
              className="bg-osu-h1"
              style={{ width: buildBarWidth(positiveRatings, totalRatings) }}
            />
          </div>
        </div>
        <div className="mt-1 flex justify-between text-xs text-white/45">
          <span>{formatInteger(negativeRatings)}</span>
          <span>{formatInteger(positiveRatings)}</span>
        </div>
      </div>

      <div className="mt-4 rounded-lg bg-black/20 p-3">
        <div className="mb-3 flex flex-wrap items-center gap-3 text-xs text-white/45">
          <span className="inline-flex items-center gap-1">
            <Play className="h-3.5 w-3.5" />
            {formatInteger(beatmapset.play_count)}
          </span>
          <span className="inline-flex items-center gap-1">
            <Heart className="h-3.5 w-3.5" />
            {formatInteger(beatmapset.favourite_count)}
          </span>
        </div>
        <RatingChart ratings={ratings} />
      </div>
    </section>
  );
}

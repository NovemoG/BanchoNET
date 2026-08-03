import { CalendarDays, CheckCircle2, Languages, ListMusic, Tag } from "lucide-react";
import type { BeatmapsetShowBeatmap, BeatmapsetShowData } from "@/lib/beatmapset-types";
import { cn } from "@/lib/utils";

type BeatmapsetInfoProps = {
  beatmap: BeatmapsetShowBeatmap;
  beatmapset: BeatmapsetShowData;
};

const integerFormatter = new Intl.NumberFormat("en-US");
const percentFormatter = new Intl.NumberFormat("en-US", {
  maximumFractionDigits: 1,
  minimumFractionDigits: 1,
});
const dateFormatter = new Intl.DateTimeFormat("en-US", {
  day: "numeric",
  month: "short",
  year: "numeric",
});

function formatInteger(value: number | null | undefined) {
  return integerFormatter.format(Math.round(value ?? 0));
}

function formatPercent(value: number) {
  return percentFormatter.format(value * 100);
}

function formatDate(value: string | null | undefined) {
  if (value == null) {
    return null;
  }

  const date = new Date(value);
  if (Number.isNaN(date.getTime())) {
    return null;
  }

  return dateFormatter.format(date);
}

function buildBarWidth(value: number, total: number) {
  if (!Number.isFinite(value) || !Number.isFinite(total) || total <= 0) {
    return "0%";
  }

  return `${Math.max(0, Math.min(100, (value / total) * 100))}%`;
}

function FailureChart({ beatmap }: { beatmap: BeatmapsetShowBeatmap }) {
  const exits = beatmap.failtimes.exit ?? [];
  const fails = beatmap.failtimes.fail ?? [];

  if (exits.length === 0 || exits.length !== fails.length) {
    return <div className="text-sm text-white/45">No fail data available.</div>;
  }

  const maxValue = Math.max(
    1,
    ...exits.map((exitValue, index) => exitValue + (fails[index] ?? 0)),
  );

  return (
    <div className="flex h-28 items-end gap-px overflow-hidden rounded-lg bg-black/15 p-2">
      {exits.map((exitValue, index) => {
        const failValue = fails[index] ?? 0;
        return (
          <div className="flex min-w-0 flex-1 flex-col justify-end" key={`${index}-${exitValue}-${failValue}`}>
            <div
              className="rounded-t-[2px] bg-[#cf6e7f]"
              style={{ height: buildBarWidth(failValue, maxValue) }}
            />
            <div
              className={cn("bg-osu-h1/75", failValue <= 0 && "rounded-t-[2px]")}
              style={{ height: buildBarWidth(exitValue, maxValue) }}
            />
          </div>
        );
      })}
    </div>
  );
}

function MetaList({
  items,
}: {
  items: Array<{ label: string; value: React.ReactNode }>;
}) {
  return (
    <dl className="grid gap-3 sm:grid-cols-2">
      {items.map((item) => (
        <div className="rounded-lg bg-black/15 px-3 py-2" key={item.label}>
          <dt className="mb-1 text-[11px] font-semibold uppercase tracking-[0.08em] text-white/45">
            {item.label}
          </dt>
          <dd className="text-sm text-white">{item.value}</dd>
        </div>
      ))}
    </dl>
  );
}

export function BeatmapsetInfo({ beatmap, beatmapset }: BeatmapsetInfoProps) {
  const description = beatmapset.description.description?.trim() ?? "";
  const tags = beatmapset.tags
    .split(/\s+/)
    .map((tag) => tag.trim())
    .filter(Boolean)
    .slice(0, 30);
  const successRate =
    beatmap.playcount > 0 ? beatmap.passcount / Math.max(1, beatmap.playcount) : 0;

  return (
    <div className="grid gap-4 lg:grid-cols-[minmax(0,1fr)_340px]">
      <section className="grid gap-4">
        <div className="rounded-xl border border-white/8 bg-osu-b4 p-4 shadow-[0_18px_36px_rgba(0,0,0,0.22)]">
          <div className="mb-3 flex items-center gap-2 text-sm font-semibold text-white">
            <ListMusic className="h-4 w-4 text-osu-h1" />
            Description
          </div>
          {description.length > 0 ? (
            <div
              className="prose prose-invert max-w-none text-sm prose-p:text-white/80 prose-a:text-osu-h1 prose-strong:text-white"
              dangerouslySetInnerHTML={{ __html: description }}
            />
          ) : (
            <div className="text-sm text-white/45">No description available.</div>
          )}
        </div>

        <div className="rounded-xl border border-white/8 bg-osu-b4 p-4 shadow-[0_18px_36px_rgba(0,0,0,0.22)]">
          <div className="mb-3 flex items-center gap-2 text-sm font-semibold text-white">
            <Languages className="h-4 w-4 text-osu-h1" />
            Metadata
          </div>
          <MetaList
            items={[
              { label: "Source", value: beatmapset.source || "Unknown" },
              { label: "Genre", value: beatmapset.genre.name || "Unknown" },
              { label: "Language", value: beatmapset.language.name || "Unknown" },
              { label: "Status", value: beatmapset.status },
              { label: "Submitted", value: formatDate(beatmapset.submitted_date) ?? "Unknown" },
              { label: "Updated", value: formatDate(beatmapset.last_updated) ?? "Unknown" },
              { label: "Ranked", value: formatDate(beatmapset.ranked_date) ?? "Unranked" },
              {
                label: "Nominations",
                value: beatmapset.nominations_summary != null
                  ? `${formatInteger(beatmapset.nominations_summary.current)}`
                  : "None",
              },
            ]}
          />

          {tags.length > 0 ? (
            <div className="mt-4">
              <div className="mb-2 flex items-center gap-2 text-sm font-semibold text-white">
                <Tag className="h-4 w-4 text-osu-h1" />
                Tags
              </div>
              <div className="flex flex-wrap gap-2">
                {tags.map((tag) => (
                  <span
                    className="rounded-full border border-white/8 bg-black/15 px-2.5 py-1 text-xs text-white/65"
                    key={tag}
                  >
                    {tag}
                  </span>
                ))}
              </div>
            </div>
          ) : null}

          {beatmapset.pack_tags.length > 0 ? (
            <div className="mt-4">
              <div className="mb-2 flex items-center gap-2 text-sm font-semibold text-white">
                <CalendarDays className="h-4 w-4 text-osu-h1" />
                Pack Tags
              </div>
              <div className="flex flex-wrap gap-2">
                {beatmapset.pack_tags.map((tag) => (
                  <span
                    className="rounded-full border border-white/8 bg-black/15 px-2.5 py-1 text-xs text-white/65"
                    key={tag}
                  >
                    {tag}
                  </span>
                ))}
              </div>
            </div>
          ) : null}
        </div>
      </section>

      <section className="rounded-xl border border-white/8 bg-osu-b4 p-4 shadow-[0_18px_36px_rgba(0,0,0,0.22)]">
        <div className="mb-3 flex items-center gap-2 text-sm font-semibold text-white">
          <CheckCircle2 className="h-4 w-4 text-osu-h1" />
          Success Rate
        </div>

        {beatmap.playcount > 0 ? (
          <>
            <div className="h-3 overflow-hidden rounded-full bg-black/20">
              <div
                className="h-full rounded-full bg-osu-h1"
                style={{ width: buildBarWidth(beatmap.passcount, Math.max(1, beatmap.playcount)) }}
              />
            </div>
            <div className="mt-2 text-sm text-white/70">
              {formatPercent(successRate)}% ({formatInteger(beatmap.passcount)} passes from{" "}
              {formatInteger(beatmap.playcount)} plays)
            </div>
          </>
        ) : (
          <div className="text-sm text-white/45">No score data available.</div>
        )}

        <div className="mt-5">
          <div className="mb-2 text-sm font-semibold text-white">Points of Failure</div>
          <FailureChart beatmap={beatmap} />
        </div>
      </section>
    </div>
  );
}

/* eslint-disable @next/next/no-img-element */
import Link from "next/link";
import {
  CheckCircle2,
  Film,
  Heart,
  Image as ImageIcon,
  Music2,
  Play,
  Radio,
  Star,
} from "lucide-react";
import { PageWrapper } from "@/components/page-wrapper";
import { BeatmapPicker } from "@/components/beatmapsets-show/beatmap-picker";
import { BeatmapsetStats } from "@/components/beatmapsets-show/stats";
import {
  getBeatmapsetDisplayArtist,
  getBeatmapsetDisplayTitle,
} from "@/lib/beatmapset";
import type { BeatmapsetShowBeatmap, BeatmapsetShowData } from "@/lib/beatmapset-types";
import { resolveAssetUrl } from "@/lib/osu-api-common";
import { cn } from "@/lib/utils";

type BeatmapsetHeaderProps = {
  beatmap: BeatmapsetShowBeatmap;
  beatmapset: BeatmapsetShowData;
};

const integerFormatter = new Intl.NumberFormat("en-US");

const statusClassNames: Record<string, string> = {
  approved: "bg-osu-green-3/20 text-osu-green-1 ring-osu-green-2/30",
  graveyard: "bg-white/8 text-white/65 ring-white/10",
  loved: "bg-pink-500/18 text-pink-200 ring-pink-300/20",
  pending: "bg-amber-500/18 text-amber-200 ring-amber-300/20",
  qualified: "bg-sky-500/18 text-sky-200 ring-sky-300/20",
  ranked: "bg-osu-h1/16 text-osu-h1 ring-osu-h1/25",
  wip: "bg-orange-500/18 text-orange-200 ring-orange-300/20",
};

function formatInteger(value: number | null | undefined) {
  return integerFormatter.format(Math.round(value ?? 0));
}

function getRequiredNominations(beatmapset: BeatmapsetShowData) {
  const mainRulesetCount = new Set(
    beatmapset.beatmaps.filter((beatmap) => !beatmap.convert).map((beatmap) => beatmap.mode),
  ).size;
  const requiredMeta = beatmapset.nominations_summary?.required_meta;

  if (requiredMeta == null || mainRulesetCount === 0) {
    return null;
  }

  return (
    requiredMeta.main_ruleset +
    requiredMeta.non_main_ruleset * Math.max(0, mainRulesetCount - 1)
  );
}

export function BeatmapsetHeader({ beatmap, beatmapset }: BeatmapsetHeaderProps) {
  const coverUrl = resolveAssetUrl(beatmapset.covers.cover);
  const previewUrl = resolveAssetUrl(beatmapset.preview_url);
  const title = getBeatmapsetDisplayTitle(beatmapset);
  const artist = getBeatmapsetDisplayArtist(beatmapset);
  const mapperName = beatmapset.user.username ?? beatmapset.creator;
  const mapperHref = beatmapset.user.id > 0 ? `/users/${beatmapset.user.id}` : `https://osu.ppy.sh/users/${beatmapset.user_id}`;
  const statusClasses =
    statusClassNames[beatmapset.status] ?? "bg-white/8 text-white/75 ring-white/10";
  const requiredNominations = getRequiredNominations(beatmapset);

  return (
    <header className="relative overflow-hidden">
      <div className="absolute inset-0 bg-osu-d6">
        <img alt={title} className="h-full w-full object-cover opacity-35" src={coverUrl} />
        <div className="absolute inset-0 bg-[linear-gradient(180deg,rgba(12,16,20,0.3),rgba(12,16,20,0.88))]" />
        <div className="absolute inset-0 backdrop-blur-[6px]" />
      </div>

      <PageWrapper className="relative py-18 md:py-26">
        <div className="grid gap-4 xl:grid-cols-[minmax(0,1fr)_340px]">
          <section className="rounded-2xl border border-white/8 bg-osu-d5/88 p-4 shadow-[0_18px_36px_rgba(0,0,0,0.26)] md:p-5">
            <div className="flex flex-wrap items-start justify-between gap-4">
              <div className="flex min-w-0 flex-1 flex-col gap-4">
                <BeatmapPicker
                  beatmaps={beatmapset.beatmaps}
                  beatmapsetId={beatmapset.id}
                  currentBeatmapId={beatmap.id}
                  currentMode={beatmap.mode}
                />

                <div>
                  <div className="mb-2 flex flex-wrap items-center gap-2">
                    <span
                      className={cn(
                        "inline-flex items-center rounded-full px-2 py-0.5 text-[11px] font-semibold ring-1",
                        statusClasses,
                      )}
                    >
                      {beatmap.status}
                    </span>
                    {beatmapset.video ? (
                      <span className="inline-flex items-center gap-1 rounded-full bg-white/8 px-2 py-0.5 text-[11px] font-semibold text-white/75 ring-1 ring-white/10">
                        <Film className="h-3 w-3" />
                        video
                      </span>
                    ) : null}
                    {beatmapset.storyboard ? (
                      <span className="inline-flex items-center gap-1 rounded-full bg-white/8 px-2 py-0.5 text-[11px] font-semibold text-white/75 ring-1 ring-white/10">
                        <ImageIcon className="h-3 w-3" />
                        storyboard
                      </span>
                    ) : null}
                  </div>

                  <h1 className="text-2xl font-semibold leading-tight text-white md:text-4xl">
                    {title}
                  </h1>
                  <div className="mt-1 text-base text-white/72 md:text-lg">
                    {artist}
                  </div>
                  <div className="mt-3 flex flex-wrap items-center gap-x-4 gap-y-2 text-sm text-white/58">
                    <span className="inline-flex items-center gap-2">
                      <Music2 className="h-4 w-4 text-osu-h1" />
                      mapped by{" "}
                      <Link className="font-semibold text-white/82 hover:text-white" href={mapperHref}>
                        {mapperName}
                      </Link>
                    </span>
                    <span className="inline-flex items-center gap-2">
                      <Star className="h-4 w-4 text-osu-h1" />
                      {beatmap.version} ({beatmap.difficulty_rating.toFixed(2)}★)
                    </span>
                  </div>
                </div>

                <div className="flex flex-wrap gap-2">
                  <a
                    className="inline-flex items-center gap-2 rounded-full bg-osu-h1 px-4 py-2 text-sm font-semibold text-osu-b6 transition-colors hover:bg-white"
                    href={previewUrl}
                    rel="noreferrer"
                    target="_blank"
                  >
                    <Play className="h-4 w-4 fill-current" />
                    Preview
                  </a>
                  <a
                    className="inline-flex items-center gap-2 rounded-full border border-white/12 bg-white/6 px-4 py-2 text-sm font-semibold text-white transition-colors hover:border-white/20 hover:bg-white/10"
                    href={`https://osu.ppy.sh/beatmapsets/${beatmapset.id}`}
                    rel="noreferrer"
                    target="_blank"
                  >
                    Open on osu!
                  </a>
                </div>
              </div>

              <div className="grid min-w-52 grid-cols-2 gap-2 text-sm md:min-w-64">
                <div className="rounded-lg bg-black/20 px-3 py-2">
                  <div className="mb-1 flex items-center gap-1 text-[11px] uppercase tracking-[0.08em] text-white/45">
                    <Play className="h-3.5 w-3.5" />
                    Plays
                  </div>
                  <div className="font-semibold text-white">{formatInteger(beatmapset.play_count)}</div>
                </div>
                <div className="rounded-lg bg-black/20 px-3 py-2">
                  <div className="mb-1 flex items-center gap-1 text-[11px] uppercase tracking-[0.08em] text-white/45">
                    <Heart className="h-3.5 w-3.5" />
                    Favourites
                  </div>
                  <div className="font-semibold text-white">{formatInteger(beatmapset.favourite_count)}</div>
                </div>
                <div className="rounded-lg bg-black/20 px-3 py-2">
                  <div className="mb-1 flex items-center gap-1 text-[11px] uppercase tracking-[0.08em] text-white/45">
                    <Radio className="h-3.5 w-3.5" />
                    Difficulties
                  </div>
                  <div className="font-semibold text-white">{formatInteger(beatmapset.version_count)}</div>
                </div>
                <div className="rounded-lg bg-black/20 px-3 py-2">
                  <div className="mb-1 flex items-center gap-1 text-[11px] uppercase tracking-[0.08em] text-white/45">
                    <CheckCircle2 className="h-3.5 w-3.5" />
                    Nominations
                  </div>
                  <div className="font-semibold text-white">
                    {beatmapset.nominations_summary != null
                      ? `${formatInteger(beatmapset.nominations_summary.current)}${requiredNominations != null ? ` / ${formatInteger(requiredNominations)}` : ""}`
                      : "-"}
                  </div>
                </div>
              </div>
            </div>
          </section>

          <BeatmapsetStats beatmap={beatmap} beatmapset={beatmapset} />
        </div>
      </PageWrapper>
    </header>
  );
}

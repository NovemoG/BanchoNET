import Link from "next/link";
import { Film, Heart, ImageIcon, Play } from "lucide-react";
import {
  BeatmapsetCoverBackground,
  BeatmapsetStatusBadge,
  formatCompactNumber,
  formatFullNumber,
  getDiffColor,
  getStatusTone,
  PanelStat,
  resolveCoverAsset,
} from "@/components/beatmapset-panels/shared";
import type { Ruleset } from "@/lib/rankings";
import { cn } from "@/lib/utils";

export type ChatBeatmapCardBeatmap = {
  artist: string;
  beatmapId: number;
  beatmapsetId: number;
  beatmapsetUrl: string;
  bpm?: number | null;
  coverUrl?: string | null;
  creator: string;
  creatorId?: number | null;
  difficultyRating: number;
  favouriteCount?: number | null;
  mode: Ruleset;
  playCount?: number | null;
  source?: string | null;
  status?: string | null;
  title: string;
  totalLength?: number | null;
  url: string;
  version: string;
  video?: boolean | null;
  storyboard?: boolean | null;
};

export type ChatBeatmapCardPayload = {
  beatmap: ChatBeatmapCardBeatmap;
  format: "card";
  type: "beatmap";
  version: 1;
};

const beatmapCardPayloadType = "beatmap";
const beatmapCardPayloadFormat = "card";

function isRecord(value: unknown): value is Record<string, unknown> {
  return typeof value === "object" && value !== null && !Array.isArray(value);
}

function readString(value: unknown) {
  return typeof value === "string" ? value : null;
}

function readNumber(value: unknown) {
  return typeof value === "number" && Number.isFinite(value) ? value : null;
}

function readBoolean(value: unknown) {
  return typeof value === "boolean" ? value : null;
}

function getBeatmapMode(value: unknown): Ruleset | null {
  return value === "osu" || value === "taiko" || value === "fruits" || value === "mania" ? value : null;
}

function normalizeBeatmapCardBeatmap(value: ChatBeatmapCardBeatmap): ChatBeatmapCardBeatmap {
  return {
    artist: value.artist,
    beatmapId: value.beatmapId,
    beatmapsetId: value.beatmapsetId,
    beatmapsetUrl: value.beatmapsetUrl,
    bpm: value.bpm ?? null,
    coverUrl: value.coverUrl ?? null,
    creator: value.creator,
    creatorId: value.creatorId ?? null,
    difficultyRating: value.difficultyRating,
    favouriteCount: value.favouriteCount ?? null,
    mode: value.mode,
    playCount: value.playCount ?? null,
    source: value.source ?? null,
    status: value.status ?? null,
    storyboard: value.storyboard ?? null,
    title: value.title,
    totalLength: value.totalLength ?? null,
    url: value.url,
    version: value.version,
    video: value.video ?? null,
  };
}

function readBeatmapCardBeatmap(value: unknown): ChatBeatmapCardBeatmap | null {
  if (!isRecord(value)) {
    return null;
  }

  const artist = readString(value.artist);
  const beatmapId = readNumber(value.beatmapId) ?? readNumber(value.beatmap_id);
  const beatmapsetId = readNumber(value.beatmapsetId) ?? readNumber(value.beatmapset_id);
  const beatmapsetUrl = readString(value.beatmapsetUrl) ?? readString(value.beatmapset_url);
  const creator = readString(value.creator);
  const difficultyRating = readNumber(value.difficultyRating) ?? readNumber(value.difficulty_rating);
  const mode = getBeatmapMode(value.mode);
  const title = readString(value.title);
  const url = readString(value.url);
  const version = readString(value.version);

  if (
    artist == null ||
    beatmapId == null ||
    beatmapsetId == null ||
    beatmapsetUrl == null ||
    creator == null ||
    difficultyRating == null ||
    mode == null ||
    title == null ||
    url == null ||
    version == null
  ) {
    return null;
  }

  return normalizeBeatmapCardBeatmap({
    artist,
    beatmapId,
    beatmapsetId,
    beatmapsetUrl,
    bpm: readNumber(value.bpm),
    coverUrl: readString(value.coverUrl) ?? readString(value.cover_url),
    creator,
    creatorId: readNumber(value.creatorId) ?? readNumber(value.creator_id),
    difficultyRating,
    favouriteCount: readNumber(value.favouriteCount) ?? readNumber(value.favourite_count),
    mode,
    playCount: readNumber(value.playCount) ?? readNumber(value.play_count),
    source: readString(value.source),
    status: readString(value.status),
    storyboard: readBoolean(value.storyboard),
    title,
    totalLength: readNumber(value.totalLength) ?? readNumber(value.total_length),
    url,
    version,
    video: readBoolean(value.video),
  });
}

export function serializeChatBeatmapCardPayload(beatmap: ChatBeatmapCardBeatmap) {
  return JSON.stringify({
    beatmap: normalizeBeatmapCardBeatmap(beatmap),
    format: beatmapCardPayloadFormat,
    type: beatmapCardPayloadType,
    version: 1,
  } satisfies ChatBeatmapCardPayload);
}

export function parseChatBeatmapCardPayload(value: string): ChatBeatmapCardPayload | null {
  if (!value.trimStart().startsWith("{")) {
    return null;
  }

  try {
    const parsed = JSON.parse(value) as unknown;

    if (!isRecord(parsed)) {
      return null;
    }

    if (
      parsed.type !== beatmapCardPayloadType ||
      parsed.format !== beatmapCardPayloadFormat ||
      parsed.version !== 1
    ) {
      return null;
    }

    const beatmap = readBeatmapCardBeatmap(parsed.beatmap);

    return beatmap == null
      ? null
      : {
          beatmap,
          format: beatmapCardPayloadFormat,
          type: beatmapCardPayloadType,
          version: 1,
        };
  } catch {
    return null;
  }
}

function formatLength(seconds?: number | null) {
  if (seconds == null || !Number.isFinite(seconds)) {
    return null;
  }

  const minutes = Math.floor(seconds / 60);
  const remainingSeconds = Math.max(0, Math.round(seconds - minutes * 60));

  return `${minutes}:${remainingSeconds.toString().padStart(2, "0")}`;
}

function BeatmapDifficultyStrip({
  beatmap,
}: {
  beatmap: ChatBeatmapCardBeatmap;
}) {
  return (
    <Link className="pointer-events-auto flex min-w-0 items-center text-white no-underline" href={beatmap.beatmapsetUrl}>
      <div className="mx-0.5 flex min-w-0 items-center">
        <div className="mr-0.5 flex text-sm" title={beatmap.mode}>
          <i className={`fa-extra-mode-${beatmap.mode}`} />
        </div>
        <div
          className="mr-1.5 h-3 w-2 shrink-0 rounded-full"
          style={{ backgroundColor: getDiffColor(beatmap.difficultyRating) }}
        />
        <div className="min-w-0 truncate text-xs font-semibold text-osu-c2">
          {beatmap.version}
        </div>
      </div>
    </Link>
  );
}

function ChatBeatmapMediaBadges({
  beatmap,
}: {
  beatmap: ChatBeatmapCardBeatmap;
}) {
  if (beatmap.video !== true && beatmap.storyboard !== true) {
    return null;
  }

  return (
    <>
      {beatmap.video === true ? (
        <div
          className="pointer-events-auto m-0 flex h-5 w-auto items-center justify-center rounded-full bg-osu-b6/70 px-2 text-white transition-all duration-150"
          title="Video"
        >
          <Film className="h-3.5 w-3.5" />
        </div>
      ) : null}
      {beatmap.storyboard === true ? (
        <div
          className="pointer-events-auto m-0 flex h-5 w-auto items-center justify-center rounded-full bg-osu-b6/70 px-2 text-white transition-all duration-150"
          title="Storyboard"
        >
          <ImageIcon className="h-3.5 w-3.5" />
        </div>
      ) : null}
    </>
  );
}

export function ChatBeatmapCard({
  payload,
  variant,
}: {
  payload: ChatBeatmapCardPayload;
  variant: "overlay" | "page";
}) {
  const { beatmap } = payload;
  const coverUrl = beatmap.coverUrl == null ? null : resolveCoverAsset(beatmap.coverUrl);
  const statusTone = getStatusTone(beatmap.status ?? "");
  const length = formatLength(beatmap.totalLength);
  const cardWidthClassName = variant === "overlay" ? "w-[260px]" : "w-[360px]";
  const titleClassName = variant === "overlay" ? "text-lg" : "text-xl";
  const creatorContent = (
    <>
      <span className="text-osu-c2">by </span>
      <span className="text-osu-l3">{beatmap.creator}</span>
    </>
  );

  return (
    <div
      className={cn(
        "group/panel relative max-w-full min-w-0 overflow-hidden rounded-xl bg-osu-b2 text-white",
        cardWidthClassName,
      )}
    >
      <div className="absolute inset-0 flex flex-col">
        <Link className="pointer-events-none z-1 block overflow-hidden rounded-lg bg-osu-b2 md:pointer-events-auto" href={beatmap.beatmapsetUrl}>
          <div className="relative aspect-[2.45/1] overflow-hidden bg-osu-b2">
            {coverUrl == null ? null : <BeatmapsetCoverBackground src={coverUrl} />}
            <div
              aria-hidden
              className="absolute inset-0 bg-[linear-gradient(180deg,rgba(0,0,0,0.1)_0%,rgba(0,0,0,0.38)_52%,rgba(0,0,0,0.8)_100%)] transition-opacity duration-150 md:group-hover/panel:opacity-0 md:group-focus-within/panel:opacity-0"
            />
            <div
              aria-hidden
              className="absolute inset-0 bg-[linear-gradient(180deg,rgba(0,0,0,0.3)_0%,rgba(0,0,0,0.72)_52%,rgba(0,0,0,1)_100%)] opacity-0 transition-opacity duration-150 md:group-hover/panel:opacity-100 md:group-focus-within/panel:opacity-100"
            />
          </div>
        </Link>
      </div>

      <div className="pointer-events-none relative z-10 flex h-full flex-col">
        <div className="relative aspect-[2.45/1] flex-none bg-transparent md:group-hover/panel:bg-osu-b6/10 md:group-focus-within/panel:bg-osu-b6/10">
          <div className="absolute left-2.5 top-2.5 flex flex-wrap items-center gap-1.5">
            <ChatBeatmapMediaBadges beatmap={beatmap} />
            {beatmap.status == null ? null : (
              <BeatmapsetStatusBadge
                className="min-h-0 px-2 py-0.75 text-xs leading-none"
                status={beatmap.status}
                statusTone={statusTone}
              />
            )}
          </div>

          <Link className="pointer-events-auto absolute inset-x-3 bottom-2.5 min-w-0 no-underline" href={beatmap.beatmapsetUrl}>
            <div
              className={cn(
                "overflow-hidden font-semibold leading-none text-white [display:-webkit-box] [-webkit-box-orient:vertical] [-webkit-line-clamp:2] [text-shadow:0_1px_0_rgba(0,0,0,0.3)]",
                titleClassName,
              )}
            >
              {beatmap.title}
            </div>
            <div className="truncate text-sm font-semibold text-white [text-shadow:0_1px_0_rgba(0,0,0,0.3)]">
              {beatmap.artist}
            </div>
          </Link>
        </div>

        <div className="relative h-19 flex-none overflow-hidden bg-osu-b2">
          <div className="relative flex h-full flex-col justify-between px-3 py-2">
            <div className="flex min-w-0 items-center gap-3 text-sm text-osu-c2">
              <div className="min-w-0 truncate font-semibold">
                {beatmap.creatorId == null ? (
                  creatorContent
                ) : (
                  <Link className="pointer-events-auto no-underline" href={`/users/${beatmap.creatorId}`}>
                    {creatorContent}
                  </Link>
                )}
              </div>

              <div className="ml-auto flex shrink-0 items-center gap-3 text-sm font-semibold">
                {beatmap.playCount == null ? null : (
                  <PanelStat
                    icon={<Play className="h-2.5 w-2.5 fill-current" />}
                    title={`${formatFullNumber(beatmap.playCount)} plays`}
                    value={formatCompactNumber(beatmap.playCount)}
                  />
                )}
                {beatmap.favouriteCount == null ? null : (
                  <PanelStat
                    icon={<Heart className="h-2.5 w-2.5" />}
                    title={`${formatFullNumber(beatmap.favouriteCount)} favourites`}
                    value={formatCompactNumber(beatmap.favouriteCount)}
                  />
                )}
              </div>
            </div>

            <div className="flex min-w-0 items-center gap-3 text-sm text-osu-c2">
              <div className="min-w-0 flex-1 truncate font-semibold">
                {beatmap.source || "\u00A0"}
              </div>

              <div className="ml-auto flex shrink-0 items-center gap-2.5 text-sm font-semibold">
                {beatmap.bpm == null ? null : <div>{Math.round(beatmap.bpm)} BPM</div>}
                {length == null ? null : <div>{length}</div>}
              </div>
            </div>

            <div className="pt-1">
              <BeatmapDifficultyStrip beatmap={beatmap} />
            </div>
          </div>
        </div>
      </div>
    </div>
  );
}

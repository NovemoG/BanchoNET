import Link from "next/link";
import {Film, Image as ImageIcon, Play} from "lucide-react";
import {cloneElement, isValidElement} from "react";
import type {ComponentPropsWithoutRef, MouseEvent as ReactMouseEvent, ReactNode, RefObject} from "react";
import {buildBeatmapsetPageHref} from "@/lib/beatmapset-page-hash";
import {resolveAssetUrl} from "@/lib/osu-api-common";
import type {Beatmap, Beatmapset} from "@/lib/profile";
import {cn} from "@/lib/utils";

export type BeatmapsetPanelSize = "cover" | "nano" | "mini" | "normal" | "extra" | "list";

export type BeatmapsetPanelProps = {
    beatmapset: Beatmapset;
    className?: string;
    size?: BeatmapsetPanelSize;
};

export type BeatmapsetPanelData = {
    artist: string;
    beatmapDotsCompact: boolean;
    canPlayPreview: boolean;
    canShowPopup: boolean;
    coverCardUrl: string;
    coverListUrl: string;
    coverSlimUrl: string;
    displayDate: string | null;
    displayDateValue: string | null;
    downloadLink: { title: string; url: string } | null;
    favouriteActive: boolean;
    groupedBeatmaps: Map<Beatmap["mode"], Beatmap[]>;
    mapperUrl: string;
    nominations: { current: number; required: number } | null;
    panelUrl: string;
    previewUrl: string;
    statusTone: {
        background: string;
        color: string;
    };
    title: string;
    visibleBeatmapGroups: Array<readonly [Beatmap["mode"], Beatmap[]]>;
};

type PreviewAudioProps = Pick<
    ComponentPropsWithoutRef<"audio">,
    "onEnded" | "onPause" | "onPlay" | "onTimeUpdate" | "preload" | "src"
>;

const compactFormatter = new Intl.NumberFormat("en-US", {
    maximumFractionDigits: 1,
    notation: "compact",
});
const fullNumberFormatter = new Intl.NumberFormat("en-US");
const dateFormatter = new Intl.DateTimeFormat("en-US", {
    day: "numeric",
    month: "short",
    year: "numeric",
});
const modeOrder: Array<Beatmap["mode"]> = ["osu", "taiko", "fruits", "mania"];

const statusLabelMap: Record<string, string> = {
    approved: "Approved",
    graveyard: "Graveyard",
    loved: "Loved",
    pending: "Pending",
    qualified: "Qualified",
    ranked: "Ranked",
    wip: "WIP",
};

const statusToneMap: Record<
    string,
    {
        background: string;
        color: string;
    }
> = {
    approved: {background: "#7ddf64", color: "hsl(var(--hsl-b3))"},
    graveyard: {background: "#000000", color: "hsl(var(--hsl-b1))"},
    loved: {background: "hsl(var(--hsl-h1))", color: "hsl(var(--hsl-b3))"},
    pending: {background: "#f7a21b", color: "hsl(var(--hsl-b3))"},
    qualified: {background: "#63b4ff", color: "hsl(var(--hsl-b3))"},
    ranked: {background: "#7ddf64", color: "hsl(var(--hsl-b3))"},
    wip: {background: "#ff842a", color: "hsl(var(--hsl-b3))"},
};

function floorToPrecision(value: number, precision: number) {
    const factor = 10 ** precision;
    return Math.floor(value * factor) / factor;
}

function interpolateColor(start: string, end: string, factor: number) {
    const startValue = Number.parseInt(start.slice(1), 16);
    const endValue = Number.parseInt(end.slice(1), 16);
    const startRgb = {
        b: startValue & 255,
        g: (startValue >> 8) & 255,
        r: (startValue >> 16) & 255,
    };
    const endRgb = {
        b: endValue & 255,
        g: (endValue >> 8) & 255,
        r: (endValue >> 16) & 255,
    };
    const channel = (from: number, to: number) =>
        Math.round(from + (to - from) * factor)
            .toString(16)
            .padStart(2, "0");

    return `#${channel(startRgb.r, endRgb.r)}${channel(startRgb.g, endRgb.g)}${channel(startRgb.b, endRgb.b)}`;
}

export function formatCompactNumber(value: number | null | undefined) {
    return compactFormatter.format(value ?? 0);
}

export function formatFullNumber(value: number | null | undefined) {
    return fullNumberFormatter.format(value ?? 0);
}

export function formatDate(value: string | null | undefined) {
    if (value == null) {
        return null;
    }

    const date = new Date(value);
    if (Number.isNaN(date.getTime())) {
        return null;
    }

    return dateFormatter.format(date);
}

export function formatStarRating(value: number) {
    return floorToPrecision(value, 2).toLocaleString("en-US", {
        maximumFractionDigits: 2,
        minimumFractionDigits: 2,
    });
}

export function getBeatmapsetArtist(beatmapset: Beatmapset) {
    return beatmapset.artist_unicode || beatmapset.artist;
}

export function getBeatmapsetTitle(beatmapset: Beatmapset) {
    return beatmapset.title_unicode || beatmapset.title;
}

export function getDiffColor(difficultyRating: number) {
    const stops: Array<[number, string]> = [
        [0.1, "#4290FB"],
        [1.25, "#4FC0FF"],
        [2, "#4FFFD5"],
        [2.5, "#7CFF4F"],
        [3.3, "#F6F05C"],
        [4.2, "#FF8068"],
        [4.9, "#FF4E6F"],
        [5.8, "#C645B8"],
        [6.7, "#6563DE"],
        [7.7, "#18158E"],
        [9, "#000000"],
    ];

    if (difficultyRating < 0.1) {
        return "#AAAAAA";
    }

    if (difficultyRating >= 9) {
        return "#000000";
    }

    for (let index = 1; index < stops.length; index += 1) {
        const [previousThreshold, previousColor] = stops[index - 1];
        const [nextThreshold, nextColor] = stops[index];

        if (difficultyRating <= nextThreshold) {
            const factor =
                (difficultyRating - previousThreshold) /
                (nextThreshold - previousThreshold);
            return interpolateColor(previousColor, nextColor, factor);
        }
    }

    return stops[stops.length - 1][1];
}

export function getDiffTextColor(difficultyRating: number) {
    if (difficultyRating < 6.5) {
        return "#000000";
    }

    if (difficultyRating < 9) {
        return "#F6F05C";
    }

    return "#6563DE";
}

export function getStatusLabel(status: string) {
    return statusLabelMap[status] ?? status.charAt(0).toUpperCase() + status.slice(1);
}

export function getStatusTone(status: string) {
    return (
        statusToneMap[status] ?? {
            background: "hsl(var(--hsl-b3))",
            color: "hsl(var(--hsl-c1))",
        }
    );
}

export function getDisplayDate(beatmapset: Beatmapset) {
    if (["approved", "loved", "qualified", "ranked"].includes(beatmapset.status)) {
        return beatmapset.ranked_date ?? beatmapset.last_updated ?? null;
    }

    return beatmapset.last_updated ?? beatmapset.ranked_date ?? null;
}

export function getSortedBeatmaps(beatmaps: Beatmapset["beatmaps"]) {
    return [...(beatmaps ?? [])].sort((left, right) => {
        if (left.mode === "mania" && right.mode === "mania") {
            return (
                (left.cs ?? 0) - (right.cs ?? 0) ||
                left.difficulty_rating - right.difficulty_rating
            );
        }

        return left.difficulty_rating - right.difficulty_rating;
    });
}

export function resolveCoverAsset(src: string) {
    if (src.startsWith("local:/")) {
        return src.replace("local:", "");
    }

    return resolveAssetUrl(src);
}

function groupBeatmaps(beatmaps: Beatmapset["beatmaps"]) {
    const grouped = new Map<Beatmap["mode"], Beatmap[]>();

    for (const mode of modeOrder) {
        grouped.set(
            mode,
            getSortedBeatmaps((beatmaps ?? []).filter((beatmap) => beatmap.mode === mode)),
        );
    }

    return grouped;
}

function getNominations(
    beatmapset: Beatmapset,
    groupedBeatmaps: Map<Beatmap["mode"], Beatmap[]>,
) {
    const summary = beatmapset.nominations_summary;
    if (summary == null) {
        return null;
    }

    const activeRulesets = [...groupedBeatmaps.values()].filter(
        (beatmaps) => beatmaps.length > 0,
    ).length;
    const required =
        summary.required_meta.main_ruleset +
        summary.required_meta.non_main_ruleset * Math.max(0, activeRulesets - 1);

    return {
        current: summary.current,
        required,
    };
}

function getDownloadLink(beatmapset: Beatmapset) {
    if (
        beatmapset.availability?.download_disabled ||
        beatmapset.availability?.more_information != null
    ) {
        return null;
    }

    return {
        title: beatmapset.video
            ? "Download beatmapset with video"
            : "Download beatmapset",
        url: `https://osu.ppy.sh/beatmapsets/${beatmapset.id}/download`,
    };
}

export function getBeatmapsetPanelData(beatmapset: Beatmapset): BeatmapsetPanelData {
    const groupedBeatmaps = groupBeatmaps(beatmapset.beatmaps);
    const visibleBeatmapGroups = modeOrder
        .map((mode) => [mode, groupedBeatmaps.get(mode) ?? []] as const)
        .filter(([, beatmaps]) => beatmaps.length > 0);

    return {
        artist: getBeatmapsetArtist(beatmapset),
        beatmapDotsCompact: (beatmapset.beatmaps?.length ?? 0) > 12,
        canPlayPreview: beatmapset.preview_url.length > 0,
        canShowPopup: visibleBeatmapGroups.length > 0,
        coverCardUrl: resolveCoverAsset(beatmapset.covers.card || beatmapset.covers.cover),
        coverListUrl: resolveCoverAsset(beatmapset.covers.list || beatmapset.covers.cover),
        coverSlimUrl: resolveCoverAsset(beatmapset.covers.slimcover || beatmapset.covers.cover || beatmapset.covers.card),
        displayDate: formatDate(getDisplayDate(beatmapset)),
        displayDateValue: getDisplayDate(beatmapset),
        downloadLink: getDownloadLink(beatmapset),
        favouriteActive: beatmapset.has_favourited === true,
        groupedBeatmaps,
        mapperUrl: `/users/${beatmapset.user_id}`,
        nominations: getNominations(beatmapset, groupedBeatmaps),
        panelUrl: `/beatmapsets/${beatmapset.id}`,
        previewUrl: beatmapset.preview_url.length > 0 ? resolveAssetUrl(beatmapset.preview_url) : "",
        statusTone: getStatusTone(beatmapset.status),
        title: getBeatmapsetTitle(beatmapset),
        visibleBeatmapGroups,
    };
}

export function BeatmapDifficultyPill({
    beatmap,
    className,
}: {
    beatmap: Beatmap;
    className?: string;
}) {
    const difficultyColor = getDiffColor(beatmap.difficulty_rating);
    const difficultyTextColor = getDiffTextColor(beatmap.difficulty_rating);

    return (
        <div
            className={cn(
                "inline-flex h-4.5 min-w-14 items-center justify-center rounded-full px-2 text-xs font-bold leading-none whitespace-nowrap",
                className,
            )}
            style={{
                backgroundColor: difficultyColor,
                color: difficultyTextColor,
            }}
        >
            ★ {formatStarRating(beatmap.difficulty_rating)}
        </div>
    );
}

export function BeatmapsetStatusBadge({
    className,
    status,
    statusTone,
}: {
    className?: string;
    status: string;
    statusTone: { background: string; color: string };
}) {
    return (
        <div
            className={cn(
                "inline-flex min-h-4.5 items-center justify-center rounded-full px-1.25 py-0.5 text-[0.8rem] leading-none font-extrabold uppercase whitespace-nowrap",
                className,
            )}
            style={{
                backgroundColor: statusTone.background,
                color: statusTone.color,
            }}
        >
            {getStatusLabel(status)}
        </div>
    );
}

export function BeatmapsetDifficultyPillList({
    beatmaps,
    className,
    href,
    onMouseEnter,
    onMouseLeave,
    pillClassName,
}: {
    beatmaps: Beatmap[];
    className?: string;
    href: string;
    onMouseEnter?: () => void;
    onMouseLeave?: () => void;
    pillClassName?: string;
}) {
    return (
        <Link
            prefetch={false}
            className={cn(
                "pointer-events-auto flex min-w-0 items-center gap-1 overflow-hidden text-white no-underline",
                className,
            )}
            href={href}
            onMouseEnter={onMouseEnter}
            onMouseLeave={onMouseLeave}
        >
            {beatmaps.map((beatmap) => (
                <BeatmapDifficultyPill beatmap={beatmap} className={pillClassName} key={beatmap.id}/>
            ))}
        </Link>
    );
}

export function BeatmapsetDifficultyStrip({
    beatmapDotsCompact,
    className,
    countClassName,
    dotClassName,
    itemClassName,
    modeClassName,
    onMouseEnter,
    onMouseLeave,
    panelUrl,
    showStatus = true,
    status,
    statusTone,
    statusPillClassName,
    visibleBeatmapGroups,
}: {
    beatmapDotsCompact: boolean;
    className?: string;
    countClassName?: string;
    dotClassName?: string;
    itemClassName?: string;
    modeClassName?: string;
    onMouseEnter?: () => void;
    onMouseLeave?: () => void;
    panelUrl: string;
    showStatus?: boolean;
    status: string;
    statusPillClassName?: string;
    statusTone: { background: string; color: string };
    visibleBeatmapGroups: BeatmapsetPanelData["visibleBeatmapGroups"];
}) {
    return (
        <Link
            prefetch={false}
            className={cn(
                "pointer-events-auto flex items-center text-white no-underline",
                className,
            )}
            href={panelUrl}
            onMouseEnter={onMouseEnter}
            onMouseLeave={onMouseLeave}
        >
            {showStatus ? (
                <BeatmapsetStatusBadge
                    className={statusPillClassName}
                    status={status}
                    statusTone={statusTone}
                />
            ) : null}

            {visibleBeatmapGroups.map(([mode, beatmaps]) => (
                <div className={cn("mx-0.75 flex items-center", itemClassName)} key={mode}>
                    <div className={cn("mr-0.5 flex text-sm", modeClassName)} title={mode}>
                        <i className={`fa-extra-mode-${mode}`}/>
                    </div>
                    {beatmapDotsCompact ? (
                        <div className={cn("font-semibold", countClassName)}>{beatmaps.length}</div>
                    ) : (
                        beatmaps.map((beatmap) => (
                            <div
                                className={cn("mr-px h-3 w-1.5 rounded-full", dotClassName)}
                                key={beatmap.id}
                                style={{
                                    backgroundColor: getDiffColor(beatmap.difficulty_rating),
                                }}
                            />
                        ))
                    )}
                </div>
            ))}
        </Link>
    );
}

export function BeatmapPopupRow({
    beatmap,
    beatmapsetId,
}: {
    beatmap: Beatmap;
    beatmapsetId: number;
}) {
    const href = buildBeatmapsetPageHref({
        beatmapId: beatmap.id,
        beatmapsetId,
        mode: beatmap.mode,
    });

    return (
        <Link className="block min-w-0 w-full text-white no-underline" href={href}>
            <div className="flex min-w-0 w-full items-center gap-2">
                <div className="inline-flex shrink-0 items-center justify-center text-white">
                    <div className={`fa-extra-mode-${beatmap.mode} text-sm`}/>
                </div>
                <BeatmapDifficultyPill beatmap={beatmap} className="shrink-0"/>
                <div className="min-w-0 flex-1 overflow-hidden text-xs text-white/88 md:text-sm">
                    <div className="truncate">{beatmap.version}</div>
                </div>
            </div>
        </Link>
    );
}

export function BeatmapPreviewButton({
    audioProps,
    audioRef,
    buttonClassName,
    canPlayPreview,
    isPlaying,
    playUiVisibilityClassName,
    progress,
    togglePreview,
}: {
    audioProps: PreviewAudioProps;
    audioRef: RefObject<HTMLAudioElement | null>;
    buttonClassName?: string;
    canPlayPreview: boolean;
    isPlaying: boolean;
    playUiVisibilityClassName: string;
    progress: number;
    togglePreview: (event: ReactMouseEvent<HTMLButtonElement>) => void | Promise<void>;
}) {
    const circleRadius = 18;
    const circleCircumference = 2 * Math.PI * circleRadius;

    if (!canPlayPreview) {
        return null;
    }

    return (
        <button
            aria-label={isPlaying ? "Pause preview" : "Play preview"}
            className={cn(
                "pointer-events-auto absolute inset-0 m-auto inline-flex h-10 w-10 items-center justify-center overflow-hidden rounded-full border-0 bg-black/55 text-white transition-colors hover:bg-black/70 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-osu-h1/80",
                playUiVisibilityClassName,
                buttonClassName,
            )}
            onClick={togglePreview}
            title={isPlaying ? "Pause preview" : "Play preview"}
            type="button"
        >
            <audio
                onEnded={audioProps.onEnded}
                onPause={audioProps.onPause}
                onPlay={audioProps.onPlay}
                onTimeUpdate={audioProps.onTimeUpdate}
                preload={audioProps.preload}
                ref={audioRef}
                src={audioProps.src}
            />
            <svg
                aria-hidden="true"
                className="pointer-events-none absolute inset-0 z-0 -rotate-90"
                viewBox="0 0 40 40"
            >
                <circle
                    cx="20"
                    cy="20"
                    fill="none"
                    r={circleRadius}
                    stroke="hsl(var(--hsl-h1))"
                    strokeDasharray={circleCircumference}
                    strokeDashoffset={circleCircumference * (1 - progress)}
                    strokeLinecap="round"
                    strokeWidth="2"
                    style={{transition: "stroke-dashoffset 150ms linear"}}
                />
            </svg>
            <div className="relative z-1 flex items-center justify-center">
                {isPlaying ? (
                    <svg
                        aria-hidden="true"
                        className="h-4.5 w-4.5 fill-current"
                        viewBox="0 0 20 20"
                    >
                        <rect height="13" rx="1.2" width="4" x="4.25" y="3.5"/>
                        <rect height="13" rx="1.2" width="4" x="11.75" y="3.5"/>
                    </svg>
                ) : (
                    <Play className="h-5 w-5 fill-current"/>
                )}
            </div>
        </button>
    );
}

export function BeatmapsetMediaBadges({
    badgeClassName,
    beatmapset,
    className,
    playUiVisibilityClassName,
}: {
    badgeClassName?: string;
    beatmapset: Beatmapset;
    className?: string;
    playUiVisibilityClassName: string;
}) {
    if (!beatmapset.video && !beatmapset.storyboard) {
        return null;
    }

    return (
        <div className={cn("absolute left-1 top-1 flex", className)}>
            {beatmapset.video && (
                <div
                    className={cn(
                        "pointer-events-auto m-px flex h-5 w-5 items-center justify-center rounded-full bg-osu-b6/50 text-white transition-all duration-150",
                        playUiVisibilityClassName,
                        badgeClassName,
                    )}
                    title="Video"
                >
                    <Film className="h-2.5 w-2.5"/>
                </div>
            )}
            {beatmapset.storyboard && (
                <div
                    className={cn(
                        "pointer-events-auto m-px flex h-5 w-5 items-center justify-center rounded-full bg-osu-b6/50 text-white transition-all duration-150",
                        playUiVisibilityClassName,
                        badgeClassName,
                    )}
                    title="Storyboard"
                >
                    <ImageIcon className="h-2.5 w-2.5"/>
                </div>
            )}
        </div>
    );
}

export function BeatmapsetCoverBackground({
    className,
    src,
}: {
    className?: string;
    src: string;
}) {
    return (
        <div
            className={cn("absolute inset-0 bg-cover bg-center", className)}
            style={{backgroundImage: `url("${src}")`}}
        />
    );
}

export function PanelStat({
    className,
    icon,
    label,
    title,
    value,
}: {
    className?: string;
    icon: ReactNode;
    label?: string;
    title?: string;
    value: string;
}) {
    const renderedIcon = isValidElement<{ className?: string }>(icon)
        ? cloneElement(icon, {
            className: cn(icon.props.className, "h-3 w-3 fill-current"),
        })
        : icon;

    return (
        <div className={cn("pointer-events-auto flex items-center font-semibold", className)} title={title}>
            <div className="mr-1 text-osu-c2">{renderedIcon}</div>
            {label ? <div className="mr-1">{label}</div> : null}
            <div>{value}</div>
        </div>
    );
}

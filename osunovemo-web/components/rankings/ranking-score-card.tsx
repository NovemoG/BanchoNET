/* eslint-disable @next/next/no-img-element */
import type {CSSProperties} from "react";
import Link from "next/link";
import {Heart} from "lucide-react";
import { buildBeatmapsetPageHref } from "@/lib/beatmapset-page-hash";
import {cn} from "@/lib/utils";
import {Tooltip, TooltipContent, TooltipTrigger} from "@/components/ui/tooltip";
import {resolveAssetUrl} from "@/lib/osu-api-common";
import type {Score, ScoreMod} from "@/lib/profile";
import type {Ruleset} from "@/lib/rankings";
import {
    getDisplayedMods,
    getExtendedContent,
    getExtendedFontSize,
    getModDefinition,
    getModTitle,
    modBadgeMask,
    modExtenderMask,
    modTypeColors,
    normalizeRank,
    rankAssets,
} from "@/lib/osu-score-card";
import type {TopPlayScore} from "@/lib/top-plays";

type RankingScoreCardProps = {
    highlightActive?: boolean;
    highlighted?: boolean;
    mode: Ruleset;
    position: number;
    score: TopPlayScore;
};

const integerFormatter = new Intl.NumberFormat("en-US");
const percentFormatter = new Intl.NumberFormat("en-US", {
    maximumFractionDigits: 2,
    minimumFractionDigits: 2,
});
const dateTimeFormatter = new Intl.DateTimeFormat("en-US", {
    day: "numeric",
    hour: "numeric",
    minute: "2-digit",
    month: "short",
    year: "numeric",
});
const relativeFormatter = new Intl.RelativeTimeFormat("en-US", {numeric: "auto"});

function formatInteger(value: number | null | undefined) {
    return integerFormatter.format(Math.round(value ?? 0));
}

function formatAccuracy(value: number | null | undefined) {
    return `${percentFormatter.format((value ?? 0) * 100)}%`;
}

function formatCombo(value: number | null | undefined) {
    return `${formatInteger(value)}x`;
}

function formatRelativeTime(value: string | null | undefined) {
    if (value == null) {
        return "unknown";
    }

    const date = new Date(value);
    if (Number.isNaN(date.getTime())) {
        return "unknown";
    }

    const seconds = Math.round((date.getTime() - Date.now()) / 1000);
    const units: Array<[Intl.RelativeTimeFormatUnit, number]> = [
        ["year", 60 * 60 * 24 * 365],
        ["month", 60 * 60 * 24 * 30],
        ["week", 60 * 60 * 24 * 7],
        ["day", 60 * 60 * 24],
        ["hour", 60 * 60],
        ["minute", 60],
    ];

    for (const [unit, scale] of units) {
        if (Math.abs(seconds) >= scale) {
            return relativeFormatter.format(Math.round(seconds / scale), unit);
        }
    }

    return relativeFormatter.format(seconds, "second");
}

function formatDateTime(value: string | null | undefined) {
    if (value == null) {
        return null;
    }

    const date = new Date(value);
    if (Number.isNaN(date.getTime())) {
        return null;
    }

    return dateTimeFormatter.format(date);
}

function RankBadge({rank, className}: { rank: string; className?: string }) {
    const normalizedRank = normalizeRank(rank);
    const asset = rankAssets[normalizedRank];

    if (asset) {
        return <img alt={rank} className={cn("h-5.5 w-11 object-contain", className)} src={asset}/>;
    }

    return (
        <div
            className={cn(
                "inline-flex min-w-11 items-center justify-center rounded-full bg-osu-b6 px-2 py-1 text-xs font-bold text-white",
                className,
            )}
        >
            {rank}
        </div>
    );
}

function ModCustomisedIndicator({
                                    backgroundColor,
                                    foregroundColor,
                                }: {
    backgroundColor: string;
    foregroundColor: string;
}) {
    return (
        <svg className="h-full w-full" viewBox="0 0 32 16">
            <circle cx="25.5996" cy="5.59961" fill={foregroundColor} r="4"/>
            <path
                d="M27.7676 6.15915L27.3683 5.92852C27.4086 5.71102 27.4086 5.4879 27.3683 5.2704L27.7676 5.03977C27.8136 5.01352 27.8342 4.95915 27.8192 4.90852C27.7151 4.57477 27.5379 4.2729 27.3064 4.02165C27.2708 3.98321 27.2126 3.97384 27.1676 4.00009L26.7683 4.23071C26.6004 4.08634 26.4073 3.97477 26.1983 3.90165V3.44134C26.1983 3.38884 26.1617 3.3429 26.1101 3.33165C25.7661 3.25477 25.4136 3.25852 25.0864 3.33165C25.0348 3.3429 24.9983 3.38884 24.9983 3.44134V3.90259C24.7901 3.97665 24.597 4.08821 24.4283 4.23165L24.0298 4.00102C23.9839 3.97477 23.9267 3.98321 23.8911 4.02259C23.6595 4.2729 23.4823 4.57477 23.3783 4.90946C23.3623 4.96009 23.3839 5.01446 23.4298 5.04071L23.8292 5.27134C23.7889 5.48884 23.7889 5.71196 23.8292 5.92946L23.4298 6.16009C23.3839 6.18634 23.3633 6.24071 23.3783 6.29134C23.4823 6.62509 23.6595 6.92696 23.8911 7.17821C23.9267 7.21665 23.9848 7.22603 24.0298 7.19978L24.4292 6.96915C24.597 7.11353 24.7901 7.22509 24.9992 7.29821V7.75946C24.9992 7.81196 25.0358 7.8579 25.0873 7.86915C25.4314 7.94603 25.7839 7.94228 26.1111 7.86915C26.1626 7.8579 26.1992 7.81196 26.1992 7.75946V7.29821C26.4073 7.22415 26.6004 7.11259 26.7692 6.96915L27.1686 7.19978C27.2145 7.22603 27.2717 7.21759 27.3073 7.17821C27.5389 6.9279 27.7161 6.62602 27.8201 6.29134C27.8342 6.23977 27.8136 6.1854 27.7676 6.15915ZM25.5983 6.34946C25.1848 6.34946 24.8483 6.0129 24.8483 5.59946C24.8483 5.18602 25.1848 4.84946 25.5983 4.84946C26.0117 4.84946 26.3483 5.18602 26.3483 5.59946C26.3483 6.0129 26.0117 6.34946 25.5983 6.34946Z"
                fill={backgroundColor}
            />
        </svg>
    );
}

function ModBadge({mod}: { mod: ScoreMod }) {
    const definition = getModDefinition(mod.acronym);
    const colors = modTypeColors[definition.type];
    const extendedContent = getExtendedContent(mod);
    const extendedFontSize = getExtendedFontSize(extendedContent);
    const settingsCount = Object.keys(mod.settings ?? {}).length;
    const hasSettings = settingsCount > 0;
    const title = hasSettings && (extendedContent === "" || settingsCount > 1)
        ? getModTitle(mod, definition)
        : undefined;
    const baseMaskStyle = {
        maskImage: `url("${modBadgeMask}")`,
        maskPosition: "center",
        maskRepeat: "no-repeat",
        maskSize: "contain",
        WebkitMaskImage: `url("${modBadgeMask}")`,
        WebkitMaskPosition: "center",
        WebkitMaskRepeat: "no-repeat",
        WebkitMaskSize: "contain",
    } satisfies CSSProperties;
    const iconMaskStyle = {
        maskImage: `url("/badges/mods/${definition.filename}")`,
        maskPosition: "center",
        maskRepeat: "no-repeat",
        maskSize: "contain",
        WebkitMaskImage: `url("/badges/mods/${definition.filename}")`,
        WebkitMaskPosition: "center",
        WebkitMaskRepeat: "no-repeat",
        WebkitMaskSize: "contain",
    } satisfies CSSProperties;
    const hasDedicatedIcon = definition.filename !== "mod-no-mod.svg" || mod.acronym === "NM";
    const extenderMaskStyle = {
        maskImage: `url("${modExtenderMask}")`,
        maskPosition: "center",
        maskRepeat: "no-repeat",
        maskSize: "contain",
        WebkitMaskImage: `url("${modExtenderMask}")`,
        WebkitMaskPosition: "center",
        WebkitMaskRepeat: "no-repeat",
        WebkitMaskSize: "contain",
    } satisfies CSSProperties;

    const content = (
        <div
            className={cn("relative h-7 shrink-0", extendedContent !== "" ? "w-22" : "w-10")}
        >
            {extendedContent !== "" ? (
                <div className="absolute top-0 left-[1.45rem] flex h-7 w-16.5 items-center justify-center">
                    <div
                        className="absolute inset-0"
                        style={{
                            ...extenderMaskStyle,
                            backgroundColor: colors.foreground,
                        }}
                    />
                    <div
                        className="absolute inset-0 flex items-center justify-center pl-2 leading-none font-bold"
                        style={{color: colors.background, fontSize: extendedFontSize}}
                    >
                        {extendedContent}
                    </div>
                </div>
            ) : null}

            <div className="relative z-10 h-7 w-10 shrink-0">
                <div
                    className="absolute inset-0"
                    style={{
                        ...baseMaskStyle,
                        backgroundColor: colors.background,
                    }}
                />
                {hasDedicatedIcon ? (
                    <div
                        className="absolute inset-px"
                        style={{
                            ...iconMaskStyle,
                            backgroundColor: colors.foreground,
                        }}
                    />
                ) : (
                    <div
                        className="absolute inset-0 flex items-center justify-center text-[10px] font-black text-(--mod-fg)"
                        style={{color: colors.foreground}}
                    >
                        {mod.acronym}
                    </div>
                )}

                {hasSettings ? (
                    <div className="pointer-events-none absolute left-0 -top-1.5 h-5 w-10">
                        <ModCustomisedIndicator
                            backgroundColor={colors.background}
                            foregroundColor={colors.foreground}
                        />
                    </div>
                ) : null}
            </div>
        </div>
    );

    if (title == null) {
        return content;
    }

    return (
        <Tooltip>
            <TooltipTrigger asChild>{content}</TooltipTrigger>
            <TooltipContent className="max-w-80 text-center" side="top" sideOffset={6}>
                {title}
            </TooltipContent>
        </Tooltip>
    );
}

function PpDisplay({score}: { score: Score }) {
    if (score.pp != null) {
        return (
            <>
                {formatInteger(score.pp)}
                <div className="ml-0.5 text-[0.9rem] font-medium text-osu-l3">pp</div>
            </>
        );
    }

    if (score.beatmap?.status === "loved") {
        return <Heart className="h-5 w-5 fill-current"/>;
    }

    return "-";
}

function getHitCount(score: TopPlayScore, key: string) {
    return score.statistics[key] ?? 0;
}

function UserIdentity({
    mode,
    score,
}: {
    mode: Ruleset;
    score: TopPlayScore;
}) {
    const {user} = score;

    return (
        <div className="flex min-w-40 gap-2.5 items-center text-sm">
            <img
                alt={user.username}
                className="size-10 shrink-0 rounded-sm object-cover"
                src={resolveAssetUrl(user.avatar_url)}
            />
            {user.team?.flag_url ? (
                <span className="contents text-[20px]">
                    <img
                        alt={user.team.name}
                        className="h-5 w-5 rounded-sm object-cover"
                        src={resolveAssetUrl(user.team.flag_url)}
                    />
                </span>
            ) : null}
            <Link
                className="flex items-start min-w-0"
                href={`/users/${user.id}/${mode}`}
                style={
                    user.profile_colour != null
                        ? ({color: user.profile_colour} as CSSProperties)
                        : undefined
                }
            >
                <span className="truncate">{user.username}</span>
            </Link>
        </div>
    );
}

export function RankingScoreCard({
    highlightActive = false,
    highlighted = false,
    mode,
    position,
    score,
}: RankingScoreCardProps) {
    const beatmapUrl =
        score.beatmap?.id != null && score.beatmap.beatmapset_id != null
            ? buildBeatmapsetPageHref({
                beatmap: score.beatmap,
                beatmapsetId: score.beatmap.beatmapset_id,
            })
            : score.beatmap?.url ?? `https://osu.ppy.sh/scores/${score.id}`;
    const beatmapTitle =
        score.beatmapset?.title_unicode || score.beatmapset?.title || "Unknown beatmap";
    const beatmapArtist =
        score.beatmapset?.artist_unicode || score.beatmapset?.artist || "Unknown artist";
    const beatmapVersion = score.beatmap?.version ?? "Unknown difficulty";
    const relativeTime = formatRelativeTime(score.ended_at);
    const absoluteTime = formatDateTime(score.ended_at);
    const displayedMods = getDisplayedMods(score);

    return (
        <div className="group relative pl-8 my-[2px]">
            <div
                aria-hidden="true"
                className="pointer-events-none absolute top-1/2 left-0 flex w-6 -translate-y-1/2 items-center justify-end text-base font-normal tabular-nums text-osu-f1 opacity-0 transition-opacity duration-150 group-hover:opacity-100 group-focus-within:opacity-100"
            >
                {position}
            </div>

            <a
                className={cn(
                    "block transition-opacity",
                    !score.passed && "opacity-75",
                    highlightActive && !highlighted && "opacity-55",
                )}
                href={beatmapUrl}
            >
                <div
                    className={cn(
                        "flex items-center gap-4 overflow-hidden rounded-lg bg-osu-b2 transition-colors group-hover:bg-osu-b1",
                        highlighted && "bg-osu-b1 ring-1 ring-osu-h1/25 shadow-[0_12px_28px_rgba(0,0,0,0.28)]",
                    )}>
                    <div
                        className={cn(
                            "relative flex gap-2 self-stretch bg-osu-b3 pl-2 pr-4 py-2 transition-colors after:absolute after:top-0 after:-right-2.5 after:h-full after:w-2.5 after:bg-osu-b3 after:content-[''] after:[clip-path:polygon(0%_0%,100%_50%,0%_100%)] after:transition-colors group-hover:bg-osu-b2 group-hover:after:bg-osu-b2",
                            highlighted && "bg-osu-b2 after:bg-osu-b2",
                        )}>
                        <UserIdentity mode={mode} score={score}/>
                    </div>

                    <div className="flex shrink-0 items-center justify-center self-stretch pl-2">
                        <RankBadge rank={score.rank}/>
                    </div>

                    <div className="flex min-w-0 flex-1 flex-col gap-2 py-2 lg:flex-row lg:items-center lg:gap-0">
                        <div className="min-w-0 flex-1 lg:pr-4">
                            <div className="truncate text-base leading-none font-semibold text-white">
                                <div className="inline">{beatmapTitle}</div>
                                <div className="ml-1 inline text-sm font-normal text-osu-f1">by {beatmapArtist}</div>
                            </div>
                            <div className="flex flex-wrap items-center gap-x-3 text-sm">
                                <div className="truncate text-osu-orange-3">{beatmapVersion}</div>
                                <div className="text-osu-f1" title={absoluteTime ?? undefined}>
                                    {relativeTime}
                                </div>
                                <div className={cn("text-osu-f1", score.is_perfect_combo && "text-osu-orange-3")}>
                                    {formatCombo(score.max_combo)}
                                </div>
                            </div>
                        </div>

                        <div className="ml-4 flex flex-wrap justify-end gap-1.5 self-stretch">
                            <div className="flex flex-wrap gap-px content-center">
                                {displayedMods.map((mod) => (
                                    <ModBadge key={`${mod.acronym}-${JSON.stringify(mod.settings ?? {})}`} mod={mod}/>
                                ))}
                            </div>
                        </div>

                        <div className="mr-4 flex min-w-28 flex-col gap-0.5 justify-center self-stretch">
                            <div className="flex w-full justify-end text-base font-semibold">
                                <div className="text-osu-orange-3 leading-none">{formatAccuracy(score.accuracy)}</div>
                            </div>
                            <div className="flex flex-wrap items-center justify-end gap-x-2 text-xs leading-none">
                                <div className="text-[#70b0d3]">{formatInteger(getHitCount(score, "great"))}</div>
                                <div className="text-[#99c05a]">{formatInteger(getHitCount(score, "ok"))}</div>
                                <div className="text-[#d7a14c]">{formatInteger(getHitCount(score, "meh"))}</div>
                                <div className="text-[#cf6e7f]">{formatInteger(getHitCount(score, "miss"))}</div>
                            </div>
                        </div>
                    </div>

                    <div
                        className={cn(
                            "relative flex min-w-32 items-center justify-center self-stretch bg-osu-b3 text-xl font-bold text-osu-h1 transition-colors before:absolute before:top-0 before:left-0 before:h-full before:w-2.5 before:bg-osu-b2 before:content-[''] before:[clip-path:polygon(0%_0%,100%_50%,0%_100%)] before:transition-colors group-hover:bg-osu-b2 group-hover:before:bg-osu-b1",
                            highlighted && "bg-osu-b2 before:bg-osu-b1",
                        )}>
                        <PpDisplay score={score}/>
                    </div>
                </div>
            </a>
        </div>
    );
}

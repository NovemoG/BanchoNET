/* eslint-disable @next/next/no-img-element */
"use client";

import {useEffect, useMemo, useRef, useState} from "react";
import Link from "next/link";
import {BadgeInfo, ChevronDown, ChevronUp, Ellipsis, ExternalLink, Globe, Play, Users,} from "lucide-react";
import { chatOverlayActionButtonClassName } from "@/components/chat/chat-conversation";
import { BeatmapCanvasPreview } from "@/components/beatmapsets-show/beatmap-preview";
import { BeatmapDifficultyGraph } from "@/components/beatmapsets-show/difficulty-graph";
import { BeatmapLeaderboard } from "@/components/beatmapsets-show/leaderboard";
import { BeatmapPpCalculator } from "@/components/beatmapsets-show/pp-calculator";
import { beatmapInfoSliderClassNames } from "@/components/beatmapsets-show/slider-theme";
import {Header} from "@/components/header";
import {PageWrapper} from "@/components/page-wrapper";
import {Button} from "@/components/ui/button";
import {buildBeatmapsetPageHash, parseBeatmapsetPageHash} from "@/lib/beatmapset-page-hash";
import type {BeatmapDifficultyGraphResponse, BeatmapsetShowBeatmap, BeatmapsetShowData} from "@/lib/beatmapset-types";
import {resolveAssetUrl} from "@/lib/osu-api-common";
import type {Ruleset} from "@/lib/rankings";
import {cn} from "@/lib/utils";

type BeatmapsetShowPageProps = {
    beatmapset: BeatmapsetShowData;
    initialDifficultyGraph?: BeatmapDifficultyGraphResponse | null;
    initialDifficultyGraphMode?: Ruleset | null;
    initialBeatmapId?: number | null;
    initialBeatmapFile?: string | null;
};

type BeatmapIntensitySegment = {
    color: string | null;
    span: number;
};

type BeatmapIntensityGraph = {
    rawSegmentCount: number;
    segments: BeatmapIntensitySegment[];
};

type DifficultyBandKey = "easy" | "normal" | "hard" | "insane" | "extra" | "extraplus";

const integerFormatter = new Intl.NumberFormat("en-US");
const decimalFormatter = new Intl.NumberFormat("en-US", {
    maximumFractionDigits: 2,
    minimumFractionDigits: 0,
});
const dateFormatter = new Intl.DateTimeFormat("en-US", {
    day: "numeric",
    month: "short",
    year: "numeric",
});

const availableRulesets: Ruleset[] = ["osu", "taiko", "fruits", "mania"];
const collapsedStorageKey = "beatmapset-cover-collapsed";
const infoCollapsedStorageKey = "beatmapset-info-collapsed";
const ppCollapsedStorageKey = "beatmapset-pp-collapsed";

function readCollapsedPreference(key: string) {
    if (typeof window === "undefined") {
        return false;
    }

    return window.localStorage.getItem(key) === "true";
}

const statusClassNames: Record<string, string> = {
    approved: "bg-osu-green-3 text-white",
    graveyard: "bg-white/18 text-white",
    loved: "bg-pink-500 text-white",
    pending: "bg-osu-orange-3 text-white",
    qualified: "bg-osu-h1 text-white",
    ranked: "bg-osu-green-1 text-black font-bold",
    wip: "bg-osu-orange-3 text-white",
};

const difficultyBands: Array<{
    colorRating: number;
    key: DifficultyBandKey;
    max: number;
    min: number;
}> = [
    {key: "easy", min: 0, max: 1.99, colorRating: 1},
    {key: "normal", min: 2, max: 2.69, colorRating: 2.3},
    {key: "hard", min: 2.7, max: 3.99, colorRating: 3.3},
    {key: "insane", min: 4, max: 5.29, colorRating: 4.6},
    {key: "extra", min: 5.3, max: 6.49, colorRating: 5.8},
    {key: "extraplus", min: 6.5, max: Number.POSITIVE_INFINITY, colorRating: 6.9},
];

function formatInteger(value: number | null | undefined) {
    return integerFormatter.format(Math.round(value ?? 0));
}

function formatDecimal(value: number | null | undefined) {
    return decimalFormatter.format(value ?? 0);
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

function formatDuration(value: number | null | undefined) {
    if (value == null || value <= 0) {
        return "0:00";
    }

    const minutes = Math.floor(value / 60);
    const seconds = value % 60;
    return `${minutes}:${seconds.toString().padStart(2, "0")}`;
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

function getBeatmapOwnerNames(beatmap: BeatmapsetShowBeatmap, beatmapset: BeatmapsetShowData) {
    const owners = beatmap.owners.map((owner) => owner.username).filter((value): value is string => value != null);
    if (owners.length > 0) {
        return owners.join(", ");
    }

    return beatmapset.user.username ?? beatmapset.creator;
}

function getRelatedUserName(beatmapset: BeatmapsetShowData, userId: number) {
    return beatmapset.related_users?.find((user) => user.id === userId)?.username ?? null;
}

function getNominatorNames(beatmapset: BeatmapsetShowData) {
    return beatmapset.current_nominations
        .map((nomination) => getRelatedUserName(beatmapset, nomination.user_id))
        .filter((value): value is string => value != null);
}

function getBeatmapsForMode(beatmapset: BeatmapsetShowData, mode: Ruleset) {
    return beatmapset.beatmaps
        .filter((beatmap) => beatmap.mode === mode)
        .sort((left, right) => left.difficulty_rating - right.difficulty_rating);
}

function getDifficultyBand(starRating: number): DifficultyBandKey {
    return difficultyBands.find((band) => starRating >= band.min && starRating <= band.max)?.key ?? "extraplus";
}

function getDifficultyBandGroups(beatmaps: BeatmapsetShowBeatmap[]) {
    const counts = new Map<DifficultyBandKey, number>();

    for (const beatmap of beatmaps) {
        const key = getDifficultyBand(beatmap.difficulty_rating);
        counts.set(key, (counts.get(key) ?? 0) + 1);
    }

    return difficultyBands
        .map((band) => ({
            color: getStarRatingColor(band.colorRating),
            count: counts.get(band.key) ?? 0,
            key: band.key,
        }))
        .filter((band) => band.count > 0);
}

function getModeEntries(beatmapset: BeatmapsetShowData) {
    return availableRulesets.map((mode) => {
        const beatmaps = beatmapset.beatmaps.filter((beatmap) => beatmap.mode === mode);
        const mainCount = beatmaps.filter((beatmap) => !beatmap.convert).length;

        return {
            count: mainCount > 0 ? mainCount : undefined,
            disabled: beatmaps.length === 0,
            hasMainBeatmap: mainCount > 0,
            mode,
        };
    });
}

function getDefaultBeatmap(beatmapset: BeatmapsetShowData, mode?: Ruleset | null) {
    if (mode != null) {
        const modeBeatmap =
            beatmapset.beatmaps.find((beatmap) => beatmap.mode === mode && !beatmap.convert) ??
            beatmapset.beatmaps.find((beatmap) => beatmap.mode === mode);

        if (modeBeatmap != null) {
            return modeBeatmap;
        }
    }

    return beatmapset.beatmaps.find((beatmap) => !beatmap.convert) ?? beatmapset.beatmaps[0] ?? null;
}

function getBeatmapBySelection(
    beatmapset: BeatmapsetShowData,
    options: { beatmapId?: number | null; mode?: Ruleset | null },
) {
    const {beatmapId, mode} = options;

    if (mode != null && beatmapId != null) {
        const byModeAndId = beatmapset.beatmaps.find((beatmap) => beatmap.id === beatmapId && beatmap.mode === mode);
        if (byModeAndId != null) {
            return byModeAndId;
        }
    }

    if (mode != null) {
        return getDefaultBeatmap(beatmapset, mode);
    }

    if (beatmapId != null) {
        const byId = beatmapset.beatmaps.find((beatmap) => beatmap.id === beatmapId);
        if (byId != null) {
            return byId;
        }
    }

    return getDefaultBeatmap(beatmapset, mode);
}

function mergeIntensitySegments(rawColors: Array<string | null>) {
    return rawColors.reduce<BeatmapIntensitySegment[]>((mergedSegments, color) => {
        const previousSegment = mergedSegments[mergedSegments.length - 1];

        if (previousSegment != null && previousSegment.color === color) {
            previousSegment.span += 1;
            return mergedSegments;
        }

        mergedSegments.push({
            color,
            span: 1,
        });
        return mergedSegments;
    }, []);
}

function buildFailSegments(beatmap: BeatmapsetShowBeatmap) {
    const exitValues = beatmap.failtimes.exit;
    const failValues = beatmap.failtimes.fail;
    const rawSegmentCount = Math.max(exitValues.length, failValues.length, 50);
    const rawColors = Array.from({length: rawSegmentCount}, (_, index) => {
        const exitSourceIndex = exitValues.length > 0 ? Math.floor((index / rawSegmentCount) * exitValues.length) : index;
        const failSourceIndex = failValues.length > 0 ? Math.floor((index / rawSegmentCount) * failValues.length) : index;
        const value = (exitValues[exitSourceIndex] ?? 0) + (failValues[failSourceIndex] ?? 0);
        const intensity = beatmap.playcount > 0 ? value / Math.max(1, beatmap.playcount) : 0;

        if (intensity <= 0) return null;
        if (intensity > 0.12) return "rgb(107, 46, 46)";
        if (intensity > 0.06) return "rgb(204, 51, 51)";
        if (intensity > 0.02) return "rgb(235, 71, 71)";
        return "rgb(255, 102, 102)";
    });

    return {
        rawSegmentCount,
        segments: mergeIntensitySegments(rawColors),
    } satisfies BeatmapIntensityGraph;
}

function getStatRows(beatmap: BeatmapsetShowBeatmap) {
    return beatmap.mode === "mania"
        ? [
            {key: "count_circles", label: "Circle Count", value: beatmap.count_circles, total: 2000},
            {key: "count_sliders", label: "Slider Count", value: beatmap.count_sliders, total: 1000},
            {key: "count_spinners", label: "Spinner Count", value: beatmap.count_spinners, total: 10},
            {key: "cs", label: "Keys", value: beatmap.cs, total: 10},
            {key: "drain", label: "HP Drain", value: beatmap.drain, total: 10},
            {key: "accuracy", label: "Accuracy", value: beatmap.accuracy, total: 10},
        ]
        : beatmap.mode === "taiko"
            ? [
                {key: "count_circles", label: "Circle Count", value: beatmap.count_circles, total: 2000},
                {key: "count_sliders", label: "Slider Count", value: beatmap.count_sliders, total: 1000},
                {key: "count_spinners", label: "Spinner Count", value: beatmap.count_spinners, total: 10},
                {key: "cs", label: "Circle Size", value: beatmap.cs, total: 10},
                {key: "drain", label: "HP Drain", value: beatmap.drain, total: 10},
                {key: "accuracy", label: "Accuracy", value: beatmap.accuracy, total: 10},
            ]
            : [
                {key: "count_circles", label: "Circle Count", value: beatmap.count_circles, total: 2000},
                {key: "count_sliders", label: "Slider Count", value: beatmap.count_sliders, total: 1000},
                {key: "count_spinners", label: "Spinner Count", value: beatmap.count_spinners, total: 10},
                {key: "cs", label: "Circle Size", value: beatmap.cs, total: 10},
                {key: "drain", label: "HP Drain", value: beatmap.drain, total: 10},
                {key: "accuracy", label: "Accuracy", value: beatmap.accuracy, total: 10},
                {key: "ar", label: "Approach Rate", value: beatmap.ar, total: 10},
            ];
}

function getVisibleTags(beatmapset: BeatmapsetShowData, expanded: boolean) {
    const values = beatmapset.tags.split(/\s+/).filter(Boolean);
    return expanded ? values : values.slice(0, 6);
}

function useBeatmapsetSelection(beatmapset: BeatmapsetShowData, initialBeatmapId?: number | null) {
    const fallbackBeatmap = useMemo(
        () => getBeatmapBySelection(beatmapset, {beatmapId: initialBeatmapId ?? null}),
        [beatmapset, initialBeatmapId],
    );
    const [selectedBeatmapId, setSelectedBeatmapId] = useState<number | null>(fallbackBeatmap?.id ?? null);
    const [selectedMode, setSelectedMode] = useState<Ruleset | null>(fallbackBeatmap?.mode ?? null);
    const [hoveredBeatmapId, setHoveredBeatmapId] = useState<number | null>(null);

    useEffect(() => {
        const applyHash = () => {
            const parsed = parseBeatmapsetPageHash(window.location.hash);
            const fromHash = getBeatmapBySelection(beatmapset, parsed);

            if (fromHash != null) {
                setSelectedBeatmapId(fromHash.id);
                setSelectedMode(fromHash.mode);
            }
        };

        applyHash();
        window.addEventListener("hashchange", applyHash);
        return () => {
            window.removeEventListener("hashchange", applyHash);
        };
    }, [beatmapset]);

    const selectedBeatmap = useMemo(
        () => getBeatmapBySelection(beatmapset, {beatmapId: selectedBeatmapId, mode: selectedMode}),
        [beatmapset, selectedBeatmapId, selectedMode],
    );
    const hoveredBeatmap = useMemo(
        () => beatmapset.beatmaps.find((beatmap) => beatmap.id === hoveredBeatmapId) ?? null,
        [beatmapset.beatmaps, hoveredBeatmapId],
    );

    const previewBeatmap = hoveredBeatmap ?? selectedBeatmap;

    const selectBeatmap = (beatmap: BeatmapsetShowBeatmap) => {
        setSelectedBeatmapId(beatmap.id);
        setSelectedMode(beatmap.mode);
        setHoveredBeatmapId(null);
        window.history.replaceState(null, "", `${window.location.pathname}${window.location.search}${buildBeatmapsetPageHash({beatmap})}`);
    };

    const selectMode = (mode: Ruleset) => {
        setSelectedMode(mode);
        setHoveredBeatmapId(null);
    };

    useEffect(() => {
        if (selectedBeatmap == null) {
            return;
        }

        window.history.replaceState(
            null,
            "",
            `${window.location.pathname}${window.location.search}${buildBeatmapsetPageHash({beatmap: selectedBeatmap})}`,
        );
    }, [selectedBeatmap]);

    return {
        previewBeatmap,
        selectBeatmap,
        selectMode,
        selectedBeatmap,
        selectedMode: selectedBeatmap?.mode ?? selectedMode,
        setHoveredBeatmapId,
    };
}

function GraphBar({
                      className,
                      remainderClassName,
                      valueClassName,
                      value,
                  }: {
    className?: string;
    remainderClassName?: string;
    value: number;
    valueClassName?: string;
}) {
    return (
        <div
            className={cn(
                "h-1.5 overflow-hidden rounded-full",
                beatmapInfoSliderClassNames.remainder,
                remainderClassName,
                className,
            )}
        >
            <div
                className={cn("h-full rounded-full", beatmapInfoSliderClassNames.value, valueClassName)}
                style={{width: `${Math.max(0, Math.min(100, value * 100))}%`}}
            />
        </div>
    );
}

function BeatmapAudioPreviewButton({previewUrl}: { previewUrl: string }) {
    const audioRef = useRef<HTMLAudioElement | null>(null);
    const [isPlaying, setIsPlaying] = useState(false);
    const [progress, setProgress] = useState(0);
    const circleRadius = 18;
    const circleCircumference = 2 * Math.PI * circleRadius;

    useEffect(() => {
        const audio = audioRef.current;

        if (audio != null) {
            audio.volume = 0.1;
        }

        return () => {
            if (audio == null) {
                return;
            }

            audio.pause();
            audio.currentTime = 0;
        };
    }, []);

    return (
        <button
            aria-label={isPlaying ? "Pause preview" : "Play preview"}
            className="relative inline-flex h-10 w-10 items-center justify-center overflow-hidden rounded-full bg-black/55 text-white transition-colors hover:bg-black/70 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-osu-h1/80"
            onClick={() => {
                const audio = audioRef.current;

                if (audio == null) {
                    return;
                }

                if (audio.paused) {
                    void audio.play().catch(() => {
                        setIsPlaying(false);
                    });
                    return;
                }

                audio.pause();
            }}
            title={isPlaying ? "Pause preview" : "Play preview"}
            type="button"
        >
            <audio
                onEnded={(event) => {
                    event.currentTarget.currentTime = 0;
                    setIsPlaying(false);
                    setProgress(0);
                }}
                onPause={() => setIsPlaying(false)}
                onPlay={() => setIsPlaying(true)}
                onTimeUpdate={(event) => {
                    const {currentTime, duration} = event.currentTarget;
                    setProgress(duration > 0 ? currentTime / duration : 0);
                }}
                preload="none"
                ref={audioRef}
                src={previewUrl}
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
                    <svg aria-hidden="true" className="h-[18px] w-[18px] fill-current" viewBox="0 0 20 20">
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

function HeaderNav() {
    const links = [
        {active: true, href: "#", label: "info"},
        {active: false, label: "comments"},
        {active: false, label: "modding"},
    ] as const;
    const activeLink = links.find((link) => link.active) ?? links[0];

    return (
        <>
            <ul className="relative hidden items-center gap-5 text-xs md:flex md:text-sm before:absolute before:bottom-0 before:left-0 before:right-0 before:h-px before:bg-osu-h1">
                {links.map((link) => (
                    <li className="relative flex" key={link.label}>
                        {"href" in link ? (
                            <Link
                                className={cn(
                                    "relative z-1 flex items-baseline gap-1.25 py-2.5 text-osu-c1 transition-colors before:absolute before:-bottom-0.5 before:left-0 before:hidden before:h-1.25 before:w-full before:scale-y-0 before:rounded-full before:bg-osu-h1 before:transition-transform hover:text-white md:py-4 md:before:block hover:before:scale-y-100",
                                    link.active && "font-semibold text-white before:scale-y-100",
                                )}
                                href={link.href}
                            >
                                <div>{link.label}</div>
                            </Link>
                        ) : (
                            <div className="relative z-1 flex items-baseline gap-1.25 py-2.5 text-osu-c1/45 md:py-4">
                                <div>{link.label}</div>
                            </div>
                        )}
                    </li>
                ))}
            </ul>

            <details className="group relative w-full text-xs md:hidden">
                <summary
                    className="relative block w-max max-w-full list-none py-2.5 text-white marker:hidden before:absolute before:-bottom-0.5 before:left-0 before:block before:h-1 before:w-full before:rounded-full before:bg-osu-h1 before:content-['']">
                    {activeLink.label}
                    <div
                        className="absolute top-0 left-full flex h-full items-center pl-2.5 text-[0.8em] transition-transform group-open:rotate-180">
                        <ChevronDown className="h-4 w-4"/>
                    </div>
                </summary>
                <ul className="absolute inset-x-0 top-full z-101 -mx-4 hidden list-none bg-osu-d5 p-0 group-open:grid sm:-mx-6 lg:-mx-8">
                    {links.map((link) => (
                        <li key={link.label}>
                            {"href" in link ? (
                                <Link
                                    className={cn(
                                        "block px-4 py-2.5 text-white transition-colors hover:bg-osu-d3 sm:px-6 lg:px-8",
                                        link.active && "bg-osu-d4 font-semibold",
                                    )}
                                    href={link.href}
                                >
                                    {link.label}
                                </Link>
                            ) : (
                                <div className="block cursor-default px-4 py-2.5 text-white/45 sm:px-6 lg:px-8">
                                    {link.label}
                                </div>
                            )}
                        </li>
                    ))}
                </ul>
            </details>
        </>
    );
}

function RulesetSelector({
                             modeEntries,
                             onSelect,
                             selectedMode,
                         }: {
    modeEntries: Array<{ count?: number; disabled: boolean; hasMainBeatmap: boolean; mode: Ruleset }>;
    onSelect: (mode: Ruleset) => void;
    selectedMode: Ruleset | null;
}) {
    return (
        <ul className="absolute top-0 right-4 flex h-full items-center gap-x-5 gap-y-2.5 text-sm leading-normal sm:right-[50px]">
            {modeEntries.map((entry) => (
                <li key={entry.mode}>
                    {entry.disabled ? (
                        <span
                            className="flex items-center gap-1.5 bg-transparent p-0 text-[24px] [text-shadow:0_1px_2px_rgba(0,0,0,0.45)]"
                            title={entry.mode}
                        >
                            <div className={cn(`fa-extra-mode-${entry.mode}`, "text-[24px] leading-none text-white/25")}/>
                            {entry.count != null ? (
                                <span className="rounded-sm bg-osu-b6 px-1.5 py-1 text-[0.72em] leading-none font-bold text-osu-f1 shadow-none">
                                    {entry.count}
                                </span>
                            ) : null}
                            <div className="sr-only">{entry.mode}</div>
                        </span>
                    ) : (
                        <button
                            className={cn(
                                "group flex items-center gap-1.5 bg-transparent p-0 text-[24px] [text-shadow:0_1px_2px_rgba(0,0,0,0.45)]",
                            )}
                            onClick={() => onSelect(entry.mode)}
                            title={entry.mode}
                            type="button"
                        >
                            <div
                                className={cn(
                                    `fa-extra-mode-${entry.mode}`,
                                    "text-[24px] leading-none",
                                    entry.hasMainBeatmap ? "text-osu-h1 transition-colors group-hover:text-white" : "text-osu-l2 transition-colors group-hover:text-white",
                                    entry.mode === selectedMode && "text-white",
                                )}
                            />
                            {entry.count != null ? (
                                <span className="rounded-sm bg-osu-b6 px-1.5 py-1 text-[0.72em] leading-none font-bold text-osu-f1 shadow-none">
                                    {entry.count}
                                </span>
                            ) : null}
                            <div className="sr-only">{entry.mode}</div>
                        </button>
                    )}
                </li>
            ))}
        </ul>
    );
}

function RatingSpread({ratings}: { ratings: number[] }) {
    const values = ratings.slice(1);
    const maxValue = Math.max(1, ...values);

    return (
        <div className="mt-3 flex h-14 items-end gap-1">
            {values.map((value, index) => (
                <div className="flex min-w-0 flex-1 items-end" key={`${index}-${value}`}>
                    <div
                        className="w-full rounded-sm bg-osu-h1"
                        style={{height: value <= 0 ? "2px" : `${Math.max(8, (value / maxValue) * 100)}%`}}
                    />
                </div>
            ))}
        </div>
    );
}

function DifficultyInfo({
                            beatmap,
                            beatmapset,
                        }: {
    beatmap: BeatmapsetShowBeatmap;
    beatmapset: BeatmapsetShowData;
}) {
    return (
        <div className="flex min-w-0 items-center gap-2">
            <div className="inline-flex items-center justify-center text-white">
                <div className={`fa-extra-mode-${beatmap.mode} text-2xl`}/>
            </div>
            <div
                className="inline-flex h-6 min-w-14 items-center justify-center rounded-full px-2.5 text-sm font-bold leading-none whitespace-nowrap"
                style={{
                    backgroundColor: getStarRatingColor(beatmap.difficulty_rating),
                    color: beatmap.difficulty_rating >= 6.5 ? "#fff" : "#000",
                }}
            >
                ★ {beatmap.difficulty_rating.toFixed(2)}
            </div>
            <div className="min-w-0 text-sm text-white/88 md:text-base flex items-baseline">
                <div className="font-semibold truncate">{beatmap.version}</div>
                <div className="ml-2 text-white/62 text-sm shrink-0">mapped by</div>
                <div className="ml-1 font-semibold text-osu-h1 text-sm truncate">{getBeatmapOwnerNames(beatmap, beatmapset)}</div>
            </div>
        </div>
    );
}

function BeatmapCanvasPreviewCollapsed({
                                           artist,
                                           backgroundUrl,
                                           difficultyName,
                                           mode,
                                           onOpen,
                                           title,
                                       }: {
    artist: string;
    backgroundUrl: string;
    difficultyName: string;
    mode: Ruleset;
    onOpen: () => void;
    title: string;
}) {
    return (
        <div className="relative overflow-hidden rounded-2xl bg-osu-b4 text-white shadow-[0_12px_28px_rgba(0,0,0,0.28)]">
            <img
                alt=""
                className="absolute inset-0 h-full w-full scale-105 object-cover opacity-24 blur-sm"
                src={backgroundUrl}
            />
            <span className="absolute inset-0 bg-[linear-gradient(90deg,rgba(17,22,20,0.94),rgba(17,22,20,0.72),rgba(17,22,20,0.88))]"/>
            <span className="relative z-1 flex min-h-28 flex-wrap items-center gap-4 px-5 py-4">
                <button
                    aria-expanded={false}
                    aria-label="Open beatmap preview"
                    className={cn(chatOverlayActionButtonClassName, "self-center")}
                    onClick={onOpen}
                    title="Open preview"
                    type="button"
                >
                    <Play aria-hidden className="h-4 w-4 fill-current"/>
                    <span className="sr-only">Open preview</span>
                </button>
                <span className="min-w-0">
                    <span className="block text-xs font-semibold uppercase text-white/52">Beatmap preview</span>
                    <span className="mt-1 block line-clamp-1 text-lg font-bold text-white">
                        {artist} - {title}
                    </span>
                    <span className="mt-1 flex min-w-0 items-center gap-2 text-sm font-semibold text-white/70">
                        <span className={`fa-extra-mode-${mode} shrink-0 text-lg leading-none`}/>
                        <span className="truncate">{difficultyName}</span>
                    </span>
                </span>
            </span>
        </div>
    );
}

export function BeatmapsetShowPage({
    beatmapset,
    initialBeatmapId = null,
    initialBeatmapFile = null,
    initialDifficultyGraph = null,
    initialDifficultyGraphMode = null,
}: BeatmapsetShowPageProps) {
    const {
        previewBeatmap,
        selectBeatmap,
        selectMode,
        selectedBeatmap,
        selectedMode,
        setHoveredBeatmapId,
    } = useBeatmapsetSelection(beatmapset, initialBeatmapId);
    const [coverCollapsed, setCoverCollapsed] = useState(() => {
        if (typeof window === "undefined") {
            return false;
        }

        return window.localStorage.getItem(collapsedStorageKey) === "true";
    });
    const [infoCollapsed, setInfoCollapsed] = useState(() => readCollapsedPreference(infoCollapsedStorageKey));
    const [ppCollapsed, setPpCollapsed] = useState(() => readCollapsedPreference(ppCollapsedStorageKey));
    const [beatmapPreviewCollapsed, setBeatmapPreviewCollapsed] = useState(true);
    const [difficultyMenuOpen, setDifficultyMenuOpen] = useState(false);
    const [tagsExpanded, setTagsExpanded] = useState(false);

    const visibleBeatmaps = useMemo(
        () => (selectedMode != null ? getBeatmapsForMode(beatmapset, selectedMode) : []),
        [beatmapset, selectedMode],
    );
    const modeEntries = useMemo(() => getModeEntries(beatmapset), [beatmapset]);
    const failGraph = useMemo(
        () => (previewBeatmap != null ? buildFailSegments(previewBeatmap) : {rawSegmentCount: 50, segments: []}),
        [previewBeatmap],
    );
    const statRows = useMemo(
        () => (previewBeatmap != null ? getStatRows(previewBeatmap) : []),
        [previewBeatmap],
    );
    const difficultyBandGroups = useMemo(() => getDifficultyBandGroups(visibleBeatmaps), [visibleBeatmaps]);
    const visibleTags = useMemo(() => getVisibleTags(beatmapset, tagsExpanded), [beatmapset, tagsExpanded]);
    const nominatorNames = useMemo(() => getNominatorNames(beatmapset), [beatmapset]);
    const previewBackgroundUrl = resolveAssetUrl(beatmapset.covers.cover);
    const previewUrl = resolveAssetUrl(beatmapset.preview_url);
    const title = beatmapset.title_unicode || beatmapset.title;
    const artist = beatmapset.artist_unicode || beatmapset.artist;
    const statusClasses =
        statusClassNames[previewBeatmap?.status ?? beatmapset.status] ?? "bg-white/18 text-white";

    useEffect(() => {
        window.localStorage.setItem(collapsedStorageKey, String(coverCollapsed));
    }, [coverCollapsed]);

    useEffect(() => {
        window.localStorage.setItem(infoCollapsedStorageKey, String(infoCollapsed));
    }, [infoCollapsed]);

    useEffect(() => {
        window.localStorage.setItem(ppCollapsedStorageKey, String(ppCollapsed));
    }, [ppCollapsed]);

    if (selectedBeatmap == null || previewBeatmap == null) {
        return null;
    }

    const successRate =
        previewBeatmap.playcount > 0 ? previewBeatmap.passcount / Math.max(1, previewBeatmap.playcount) : 0;
    const hasSuccessRateData = previewBeatmap.playcount > 0;

    return (
        <div
            className="flex-1 bg-osu-b6 pb-10"
            style={{backgroundColor: "hsl(var(--hsl-b6))"}}
        >
            <Header
                background={(
                    <div className="absolute inset-0 overflow-hidden bg-osu-d5">
                        <div
                            className="absolute -inset-10 scale-110 opacity-25 blur-[50px]"
                            style={{
                                backgroundImage: `url('${resolveAssetUrl(beatmapset.covers.cover)}')`,
                                backgroundPosition: "center",
                                backgroundSize: "cover",
                            }}
                        />
                        <div
                            className="absolute inset-0 bg-[linear-gradient(to_right,hsl(var(--hsl-d5)),transparent_18%,transparent_82%,hsl(var(--hsl-d5)))]"/>
                    </div>
                )}
                bottom={(
                    <>
                        <HeaderNav/>
                        <RulesetSelector modeEntries={modeEntries} onSelect={selectMode} selectedMode={selectedMode}/>
                    </>
                )}
                bottomClassName="relative bg-osu-d4/95"
                icon={(
                    <div className="flex w-10 flex-none items-center justify-center self-stretch">
                        <BadgeInfo className="h-5 w-5 text-white"/>
                    </div>
                )}
                title={
                    <>
                        beatmap <div className="text-osu-h1 inline">info</div>
                    </>
                }
                topClassName="bg-osu-d5/92 text-osu-c1"
            />

            <div className="relative overflow-hidden">
                <PageWrapper className="relative py-0" modifiers="generic-compact">
                    <section className="overflow-hidden bg-osu-b5/96">
                        <div className="relative">
                            <div
                                className={cn(
                                    "relative overflow-hidden transition-[height] duration-200",
                                    coverCollapsed ? "h-14 md:h-18" : "h-58 md:h-78",
                                )}
                            >
                                <img
                                    alt={title}
                                    className="h-full w-full object-cover"
                                    src={resolveAssetUrl(beatmapset.covers.cover)}
                                />
                                <div
                                    className="absolute inset-0 bg-[linear-gradient(180deg,rgba(17,22,20,0.12),rgba(17,22,20,0.52))]"/>
                                <div className="absolute inset-x-0 bottom-0 flex items-end justify-between gap-3 p-4">
                                    <div className="flex items-center gap-2">
                                        <div
                                            className={cn("rounded-full px-5 py-2 text-sm font-black uppercase", statusClasses)}>
                                            {previewBeatmap.status}
                                        </div>
                                        {beatmapset.nsfw && (
                                            <div
                                                className="rounded-full bg-black/70 px-4 py-2 text-sm font-black uppercase text-white">
                                                explicit
                                            </div>
                                        )}
                                    </div>

                                    <div className="flex items-center gap-2">
                                        <BeatmapAudioPreviewButton previewUrl={previewUrl}/>
                                        <button
                                            className="inline-flex h-10 w-10 items-center justify-center rounded-full bg-black/55 text-white transition-colors hover:bg-black/70 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-osu-h1/80"
                                            onClick={() => setCoverCollapsed((value) => !value)}
                                            type="button"
                                        >
                                            {coverCollapsed ? <ChevronDown className="h-5 w-5"/> :
                                                <ChevronUp className="h-5 w-5"/>}
                                        </button>
                                    </div>
                                </div>
                            </div>
                        </div>

                        <div>
                            <div className="bg-osu-b4">
                                <div className="px-6 pt-3 pb-1">
                                    <div className="min-w-0 text-lg font-normal text-white md:text-2xl flex items-baseline gap-1.5">
                                        <div className="truncate">{title}</div>
                                        <div className="text-white/85 text-sm md:text-lg">by {artist}</div>
                                        <Button
                                            asChild
                                            className="h-5 w-5 self-start rounded-full bg-transparent p-0 text-white/65 shadow-none hover:bg-transparent hover:text-white focus-visible:bg-transparent active:bg-transparent md:h-4 md:w-4 mt-1.5 -ml-1"
                                            size="icon-xs"
                                            variant="ghost"
                                        >
                                            <a
                                                aria-label="Open beatmap on osu!"
                                                href={`https://osu.ppy.sh/beatmaps/${previewBeatmap.id}`}
                                                rel="noreferrer"
                                                target="_blank"
                                            >
                                                <ExternalLink className="h-3 w-3 md:h-3.5 md:w-3.5"/>
                                            </a>
                                        </Button>
                                    </div>
                                    <div className="text-sm text-white/65 flex items-baseline gap-1">
                                        created by <div
                                        className="font-semibold text-osu-h1">{beatmapset.user.username ?? beatmapset.creator}</div>
                                    </div>
                                </div>

                                <div className="px-6 pl-4 py-0">
                                    <div className="grid grid-cols-[2rem_1fr] items-center gap-1">
                                        <div className="relative">
                                            <Button
                                                aria-label="Select difficulty"
                                                className="rounded-full bg-transparent p-0 text-[16px] text-white/40 shadow-none hover:bg-transparent hover:text-white focus-visible:bg-transparent active:bg-transparent"
                                                onClick={() => setDifficultyMenuOpen((value) => !value)}
                                                type="button"
                                                size="icon"
                                                variant="ghost"
                                            >
                                                <div className="grid grid-cols-2 gap-px text-[0.75rem]">
                                                    <div className="fa-extra-mode-osu"/>
                                                    <div className="fa-extra-mode-taiko"/>
                                                    <div className="fa-extra-mode-fruits"/>
                                                    <div className="fa-extra-mode-mania"/>
                                                </div>
                                            </Button>

                                            {difficultyMenuOpen && (
                                                <div
                                                    className="absolute -left-3 top-full z-20 mt-2 min-w-64 rounded-xl bg-osu-b3 p-2">
                                                    {visibleBeatmaps.map((beatmap) => (
                                                        <button
                                                            className={cn(
                                                                "flex w-full rounded-lg px-3 py-2 text-left transition-colors",
                                                                selectedBeatmap.id === beatmap.id
                                                                    ? "bg-white/10"
                                                                    : "hover:bg-white/8",
                                                            )}
                                                            key={beatmap.id}
                                                            onClick={() => {
                                                                selectBeatmap(beatmap);
                                                                setDifficultyMenuOpen(false);
                                                            }}
                                                            type="button"
                                                        >
                                                            <DifficultyInfo beatmap={beatmap} beatmapset={beatmapset}/>
                                                        </button>
                                                    ))}
                                                </div>
                                            )}
                                        </div>

                                        <div className="min-w-0 flex flex-col">
                                            <div className="grid grid-flow-col auto-cols-[1.75rem] items-center">
                                                {visibleBeatmaps.map((beatmap) => (
                                                    <Button
                                                        aria-label={beatmap.version}
                                                        aria-pressed={selectedBeatmap.id === beatmap.id}
                                                        className={cn(
                                                            // w-full, not w-6: the grid track is 1.75rem while the button was 1.5rem,
                                                            // so a 4px dead zone sat between every pair. Sweeping across the row fell
                                                            // through those gaps, firing onMouseLeave and snapping the preview back to
                                                            // the selected difficulty. The underline below stays 1.5rem and centred.
                                                            "relative h-auto min-h-0 w-full min-w-0 rounded-full bg-transparent px-0 py-2 leading-none shadow-none hover:bg-transparent focus-visible:bg-transparent active:bg-transparent",
                                                            "before:absolute before:-bottom-0.5 before:left-1/2 before:block before:h-1 before:w-6 before:-translate-x-1/2 before:scale-y-0 before:rounded-full before:bg-current before:transition-transform before:content-[''] hover:before:scale-y-100",
                                                            selectedBeatmap.id === beatmap.id ? "opacity-100 before:scale-y-100" : "opacity-60 hover:opacity-100",
                                                        )}
                                                        key={beatmap.id}
                                                        onClick={() => selectBeatmap(beatmap)}
                                                        onMouseEnter={() => setHoveredBeatmapId(beatmap.id)}
                                                        onMouseLeave={() => setHoveredBeatmapId(null)}
                                                        style={{
                                                            color: getStarRatingColor(beatmap.difficulty_rating),
                                                        }}
                                                        title={beatmap.version}
                                                        type="button"
                                                        size="icon"
                                                        variant="ghost"
                                                    >
                                                        <div
                                                            className={`fa-extra-mode-${beatmap.mode} inline text-2xl leading-none`}/>
                                                    </Button>
                                                ))}
                                            </div>

                                            {difficultyBandGroups.length > 0 && (
                                                <div
                                                    className="grid grid-flow-col auto-cols-[1.5rem] items-center gap-1">
                                                    {difficultyBandGroups.map((group) => (
                                                        <div
                                                            className="block h-px w-full rounded-full"
                                                            key={group.key}
                                                            style={{
                                                                backgroundColor: group.color,
                                                                gridColumn: `span ${group.count} / span ${group.count}`,
                                                            }}
                                                        />
                                                    ))}
                                                </div>
                                            )}
                                        </div>
                                    </div>
                                </div>
                            </div>

                            <div className="bg-osu-b5">
                                <div className="px-6 py-3">
                                    <div className="flex flex-wrap items-center justify-between gap-2">
                                        <DifficultyInfo beatmap={previewBeatmap} beatmapset={beatmapset}/>

                                        <div
                                            className="rounded-full bg-black/18 px-4 py-1.5 text-sm font-semibold text-white/82">
                                            Length {formatDuration(previewBeatmap.hit_length)} &nbsp; BPM {formatDecimal(previewBeatmap.bpm)} &nbsp; Max Combo {previewBeatmap.max_combo == null ? "-" : `${formatInteger(previewBeatmap.max_combo)}x`}
                                        </div>
                                    </div>

                                    <BeatmapDifficultyGraph
                                        beatmapId={previewBeatmap.id}
                                        initialGraph={initialDifficultyGraph}
                                        initialGraphBeatmapId={initialBeatmapId}
                                        initialGraphMode={initialDifficultyGraphMode}
                                        mode={previewBeatmap.mode}
                                    />

                                    <div className="mt-1 overflow-hidden rounded-full bg-osu-b5">
                                        <div
                                            className="grid h-1.5 overflow-hidden rounded-full"
                                            style={{
                                                gridTemplateColumns: `repeat(${failGraph.rawSegmentCount}, minmax(0, 1fr))`,
                                            }}
                                        >
                                            {failGraph.segments.map((segment, index) => (
                                                <div
                                                    className={cn(
                                                        segment.color != null && failGraph.segments[index - 1]?.color == null && "rounded-l-full",
                                                        segment.color != null && failGraph.segments[index + 1]?.color == null && "rounded-r-full",
                                                    )}
                                                    key={`${segment.color ?? "empty"}-${segment.span}-${index}`}
                                                    style={{
                                                        backgroundColor: segment.color ?? "transparent",
                                                        gridColumn: `span ${segment.span} / span ${segment.span}`,
                                                    }}
                                                />
                                            ))}
                                        </div>
                                    </div>
                                </div>

                                {/* Collapsing stops at the difficulty and fail graphs above, which stay visible. */}
                                <div className="flex justify-center px-6 pt-1">
                                    <button
                                        aria-expanded={!infoCollapsed}
                                        className="inline-flex items-center gap-1 rounded-full px-3 py-1 text-xs text-white/60 transition-colors hover:bg-white/10 hover:text-white"
                                        onClick={() => setInfoCollapsed((value) => !value)}
                                        type="button"
                                    >
                                        {infoCollapsed ? "show details" : "hide details"}
                                        {infoCollapsed ? <ChevronDown className="h-4 w-4"/> : <ChevronUp className="h-4 w-4"/>}
                                    </button>
                                </div>

                                <div
                                    className={cn(
                                        "grid gap-0 px-6 py-5 pt-1 lg:grid-cols-[1.05fr_1fr_1fr]",
                                        infoCollapsed && "hidden",
                                    )}
                                >
                                    <div className="pb-4 lg:pb-0 lg:pr-6">
                                        <dl className="grid grid-cols-[max-content_1fr] gap-x-6 gap-y-1 text-sm">
                                            <dt className="font-semibold text-white/95">Creator</dt>
                                            <dd className="font-semibold text-osu-h1">{beatmapset.user.username ?? beatmapset.creator}</dd>
                                            <dt className="font-semibold text-white/95">Source</dt>
                                            <dd className="font-semibold text-osu-h1">{beatmapset.source || "-"}</dd>
                                            <dt className="font-semibold text-white/95">Genre</dt>
                                            <dd className="font-semibold text-osu-h1">{beatmapset.genre.name || "-"}</dd>
                                            <dt className="font-semibold text-white/95">Language</dt>
                                            <dd className="font-semibold text-osu-h1">{beatmapset.language.name || "-"}</dd>
                                            <dt className="font-semibold text-white/95">Tag</dt>
                                            <dd className="font-semibold text-osu-h1">
                                                <div className="flex flex-wrap items-center gap-1">
                                                    {visibleTags.map((tag) => (
                                                        <div key={tag}>{tag}</div>
                                                    ))}
                                                    {visibleTags.length < beatmapset.tags.split(/\s+/).filter(Boolean).length ? (
                                                        <button
                                                            className="inline-flex h-4 w-4 items-center justify-center rounded-full bg-white/10 text-white/70"
                                                            onClick={() => setTagsExpanded(true)}
                                                            type="button"
                                                        >
                                                            <Ellipsis className="h-3 w-3"/>
                                                        </button>
                                                    ) : null}
                                                </div>
                                            </dd>
                                            <dt className="font-semibold text-white/95">Nominators</dt>
                                            <dd className="font-semibold text-osu-h1">
                                                {nominatorNames.length > 0 ? nominatorNames.join(", ") : "-"}
                                            </dd>
                                            <dt className="font-semibold text-white/95">Submitted</dt>
                                            <dd className="font-semibold text-white">{formatDate(beatmapset.submitted_date) ?? "-"}</dd>
                                            <dt className="font-semibold text-white/95">Ranked</dt>
                                            <dd className="font-semibold text-white">{formatDate(beatmapset.ranked_date) ?? "-"}</dd>
                                        </dl>
                                    </div>

                                    <div className="py-4 lg:px-6 lg:py-0">
                                        <div className="space-y-1">
                                            {statRows.map((row) => (
                                                <div
                                                    className={cn(
                                                        "grid w-full grid-cols-[8.5rem_4.5rem_10rem] items-center gap-2 text-sm",
                                                        row.key === "cs" && "pt-6",
                                                    )}
                                                    key={row.key}>
                                                    <div
                                                        className="whitespace-nowrap font-semibold text-white/95">{row.label}</div>
                                                    <div
                                                        className="w-full text-right font-semibold tabular-nums text-white">
                                                        {row.key === "difficulty_rating" ? row.value.toFixed(2) : formatDecimal(row.value)}
                                                    </div>
                                                    <div
                                                        className={cn(
                                                            "h-1.5 w-full overflow-hidden rounded-full",
                                                            beatmapInfoSliderClassNames.remainder,
                                                        )}
                                                    >
                                                        <div
                                                            className={cn("h-full rounded-full", beatmapInfoSliderClassNames.value)}
                                                            style={{width: `${Math.max(0, Math.min(100, (row.value / row.total) * 100))}%`}}
                                                        />
                                                    </div>
                                                </div>
                                            ))}
                                        </div>
                                    </div>

                                    <div className="pt-4 lg:pl-6 lg:pt-0">
                                        <div className="space-y-4">
                                            <div>
                                                <div
                                                    className="mb-1 flex items-center justify-between text-sm font-semibold text-white/95">
                                                    <div>Success Rate</div>
                                                    <div>{formatDecimal(successRate * 100)}%</div>
                                                </div>
                                                <GraphBar
                                                    remainderClassName={hasSuccessRateData ? "bg-osu-red-1/75" : undefined}
                                                    value={successRate}
                                                    valueClassName={hasSuccessRateData ? "bg-osu-green-1/85" : undefined}
                                                />
                                                <div className="mt-1 text-xs text-white/52">
                                                    {formatInteger(previewBeatmap.passcount)} / {formatInteger(previewBeatmap.playcount)}
                                                </div>
                                            </div>

                                            <div>
                                                <div
                                                    className="mb-1 flex items-center justify-between text-sm font-semibold text-white/95">
                                                    <div>User Rating</div>
                                                    <div>{formatDecimal(beatmapset.rating)}</div>
                                                </div>
                                                <GraphBar value={Math.max(0, Math.min(1, beatmapset.rating / 100))}/>
                                            </div>

                                            <div>
                                                <div className="text-sm font-semibold text-white/95">Rating Spread</div>
                                                <RatingSpread ratings={beatmapset.ratings}/>
                                            </div>
                                        </div>
                                    </div>
                                </div>

                                <div className="flex flex-wrap items-center justify-between gap-4 bg-white/8 px-6 py-3">
                                    <div className="flex flex-wrap gap-6 text-sm">
                                        <div>
                                            <div className="font-semibold text-white">Total Play Count</div>
                                            <div className="mt-1 flex items-center gap-3 text-white/70">
                                                <div className="inline-flex items-center gap-1"><Globe
                                                    className="h-4 w-4"/>{formatInteger(beatmapset.play_count)}</div>
                                                <div className="inline-flex items-center gap-1"><Users
                                                    className="h-4 w-4"/>{formatInteger(beatmapset.favourite_count)}
                                                </div>
                                            </div>
                                        </div>
                                        <div>
                                            <div className="font-semibold text-white">Difficulty Play Count</div>
                                            <div className="mt-1 flex items-center gap-3 text-white/70">
                                                <div className="inline-flex items-center gap-1"><Globe
                                                    className="h-4 w-4"/>{formatInteger(previewBeatmap.playcount)}</div>
                                                <div className="inline-flex items-center gap-1"><Users
                                                    className="h-4 w-4"/>{formatInteger(previewBeatmap.passcount)}</div>
                                            </div>
                                        </div>
                                    </div>

                                    <div />
                                </div>
                            </div>
                        </div>
                    </section>
                    <div className="relative grid grid-cols-[minmax(0,1fr)] gap-2.5 py-2.5">
                        <section className="relative mx-0.5 lg:mx-2.5">
                            {beatmapPreviewCollapsed ? (
                                <BeatmapCanvasPreviewCollapsed
                                    artist={artist}
                                    backgroundUrl={previewBackgroundUrl}
                                    difficultyName={selectedBeatmap.version}
                                    mode={selectedBeatmap.mode}
                                    onOpen={() => setBeatmapPreviewCollapsed(false)}
                                    title={title}
                                />
                            ) : (
                                <BeatmapCanvasPreview
                                    autoPlay
                                    backgroundUrl={previewBackgroundUrl}
                                    beatmapId={selectedBeatmap.id}
                                    difficultyName={selectedBeatmap.version}
                                    fallbackArtist={beatmapset.artist_unicode || beatmapset.artist}
                                    fallbackCreator={beatmapset.user.username ?? beatmapset.creator}
                                    fallbackTitle={beatmapset.title_unicode || beatmapset.title}
                                    initialBeatmapFile={selectedBeatmap.id === initialBeatmapId ? initialBeatmapFile : null}
                                    key={`${selectedBeatmap.id}-${selectedBeatmap.mode}-preview`}
                                    mode={selectedBeatmap.mode}
                                    onCollapse={() => setBeatmapPreviewCollapsed(true)}
                                />
                            )}
                        </section>
                        <BeatmapPpCalculator
                            beatmap={selectedBeatmap}
                            collapsed={ppCollapsed}
                            key={`${selectedBeatmap.id}-${selectedBeatmap.mode}-pp`}
                            onToggleCollapsed={() => setPpCollapsed((value) => !value)}
                        />
                        <section className="relative mx-0.5 rounded-2xl bg-osu-b4 px-4.5 py-5 text-[0.95rem] shadow-[0_12px_28px_rgba(0,0,0,0.28)] lg:mx-2.5 lg:px-10">
                            <BeatmapLeaderboard
                                beatmapId={selectedBeatmap.id}
                                isConvert={selectedBeatmap.convert}
                                isScoreable={selectedBeatmap.is_scoreable}
                                key={`${selectedBeatmap.id}-${selectedBeatmap.mode}`}
                                mode={selectedBeatmap.mode}
                            />
                        </section>
                    </div>
                </PageWrapper>
            </div>
        </div>
    );
}

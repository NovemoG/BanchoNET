/* eslint-disable @next/next/no-img-element */
"use client";

import type {CSSProperties} from "react";
import {useMemo, useState} from "react";
import Link from "next/link";
import {LoaderCircle} from "lucide-react";
import useSWR from "swr";
import {HighlightPeriodSelector} from "@/components/highlight-period-selector";
import {CountryFlag} from "@/components/country-flag";
import {ModBadge} from "@/components/mod-badge";
import type {
    BeatmapLeaderboardResponse,
} from "@/lib/beatmapset-types";
import {resolveAssetUrl} from "@/lib/osu-api-common";
import {
    getModDefinition,
    normalizeRank,
    rankAssets,
} from "@/lib/osu-score-card";
import {
    formatAccuracy,
    formatInteger,
    type RankingType,
    type Ruleset,
} from "@/lib/rankings";
import {getScoreHighlightCutoff, isScoreHighlighted, scoreHighlightOptions} from "@/lib/score-highlight";
import {cn} from "@/lib/utils";

type BeatmapLeaderboardProps = {
    beatmapId: number;
    isConvert: boolean;
    isScoreable: boolean;
    mode: Ruleset;
};

const dateTimeFormatter = new Intl.DateTimeFormat("en-US", {
    day: "numeric",
    hour: "numeric",
    minute: "2-digit",
    month: "short",
    year: "numeric",
});
const relativeFormatter = new Intl.RelativeTimeFormat("en-US", {numeric: "auto"});
const leaderboardTypeOptions: Array<{
    label: string;
    value: RankingType;
}> = [
    {label: "Global Ranking", value: "global"},
    {label: "Country Ranking", value: "country"},
    {label: "Team Ranking", value: "team"},
];
const defaultLeaderboardMods = ["NM", "EZ", "NF", "HT", "HR", "SD", "PF", "DT", "NC", "HD", "FL"] as const;
const leaderboardModsByMode: Record<Ruleset, string[]> = {
    fruits: [...defaultLeaderboardMods],
    mania: ["NM", "EZ", "NF", "HT", "SD", "PF", "DT", "NC", "FI", "HD", "FL", "MR", "4K", "5K", "6K", "7K", "8K", "9K"],
    osu: [...defaultLeaderboardMods, "SO", "TD"],
    taiko: [...defaultLeaderboardMods],
};
const maniaConvertMods = new Set(["1K", "2K", "3K", "4K", "5K", "6K", "7K", "8K", "9K", "10K", "DS"]);
const emptyMessages: Record<RankingType, string> = {
    country: "No one from your country has set a score on this difficulty yet.",
    global: "No scores set on this difficulty yet.",
    team: "No one from your team has set a score on this difficulty yet.",
};

function getLeaderboardMods(mode: Ruleset, isConvert: boolean) {
    const mods = leaderboardModsByMode[mode];

    if (mode === "mania" && !isConvert) {
        return mods.filter((mod) => !maniaConvertMods.has(mod));
    }

    return mods;
}

function getLeaderboardHref(beatmapId: number, mode: Ruleset, rankingType: RankingType, enabledMods: string[]) {
    const searchParams = new URLSearchParams();
    searchParams.set("mode", mode);
    searchParams.set("type", rankingType);

    for (const mod of enabledMods) {
        searchParams.append("mods[]", mod);
    }

    return `/api/beatmaps/${beatmapId}/leaderboard?${searchParams.toString()}`;
}

function formatCombo(value: number | null | undefined) {
    return `${formatInteger(Math.round(value ?? 0))}x`;
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

function isToday(value: string | null | undefined) {
    if (value == null) {
        return false;
    }

    const date = new Date(value);
    if (Number.isNaN(date.getTime())) {
        return false;
    }

    const now = new Date();

    return (
        date.getFullYear() === now.getFullYear() &&
        date.getMonth() === now.getMonth() &&
        date.getDate() === now.getDate()
    );
}

function HeaderCell({
                        align = "left",
                        children,
                        className,
                    }: {
    align?: "center" | "left" | "right";
    children: React.ReactNode;
    className?: string;
}) {
    return (
        <th
            className={cn(
                "px-1.5 py-1.5 text-xs font-normal text-osu-f1",
                align === "center" && "text-center",
                align === "left" && "text-left",
                align === "right" && "text-right",
                className,
            )}
        >
            {children}
        </th>
    );
}

function BodyCell({
                      align = "left",
                      children,
                      className,
                      striped = false,
                  }: {
    align?: "center" | "left" | "right";
    children: React.ReactNode;
    className?: string;
    striped?: boolean;
}) {
    return (
        <td
            className={cn(
                striped ? "bg-osu-b3 group-hover/row:bg-osu-b2" : "bg-osu-b3 group-hover/row:bg-osu-b2",
                "px-1 py-1.5 text-sm text-white first:rounded-l-sm first:pl-2 last:rounded-r-sm last:pr-2",
                align === "center" && "text-center",
                align === "left" && "text-left",
                align === "right" && "text-right",
                className,
            )}
        >
            {children}
        </td>
    );
}

function RankBadge({rank}: { rank: string }) {
    const normalizedRank = normalizeRank(rank);
    const asset = rankAssets[normalizedRank];

    if (asset == null) {
        return <div className="text-sm font-bold text-white">{rank}</div>;
    }

    return <img alt={rank} className="mx-auto h-4.5 w-9 object-contain" src={asset}/>;
}

function LoadingRows() {
    return (
        <div className="flex items-center justify-center py-10 text-osu-f1">
            <LoaderCircle className="h-5 w-5 animate-spin"/>
        </div>
    );
}

function getHitCountCellClass(value: number | null | undefined) {
    return cn("px-0 tabular-nums", (value ?? 0) === 0 && "text-white/35");
}

function LeaderboardTypeTabs({
                                 currentValue,
                                 onSelect,
                             }: {
    currentValue: RankingType;
    onSelect: (value: RankingType) => void;
}) {
    return (
        <div
            className="relative flex justify-center gap-4 text-[13px] before:absolute before:bottom-0 before:left-0 before:h-px before:w-full before:bg-[linear-gradient(to_left,transparent_10%,hsl(var(--hsl-b1))_50%,transparent_90%)] sm:gap-5 sm:text-sm"
            role="tablist"
        >
            {leaderboardTypeOptions.map((option) => {
                const active = option.value === currentValue;

                return (
                    <button
                        aria-selected={active}
                        key={option.value}
                        role="tab"
                        className={cn(
                            "relative z-1 border-b-[5px] border-transparent px-0 py-[5px] font-semibold text-white transition-colors focus-visible:outline-none focus-visible:text-white",
                            active
                                ? "border-osu-h1 font-bold text-white"
                                : "text-white hover:text-osu-l1",
                        )}
                        onClick={() => onSelect(option.value)}
                        tabIndex={active ? 0 : -1}
                        type="button"
                    >
                        <span>{option.label}</span>
                    </button>
                );
            })}
        </div>
    );
}

function LeaderboardModSelector({
                                   enabledMods,
                                   isConvert,
                                   mode,
                                   onToggle,
                               }: {
    enabledMods: string[];
    isConvert: boolean;
    mode: Ruleset;
    onToggle: (mod: string) => void;
}) {
    const mods = getLeaderboardMods(mode, isConvert);
    const hasEnabledMods = enabledMods.length > 0;

    return (
        <div className="group/mods mx-auto mt-5 flex max-w-full w-max flex-wrap justify-center">
            {mods.map((mod) => {
                const active = enabledMods.includes(mod);
                const definition = getModDefinition(mod);

                return (
                    <button
                        aria-label={definition.name}
                        aria-pressed={active}
                        className={cn(
                            "m-[2px] cursor-pointer rounded-sm transition-opacity focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-osu-h1/80",
                            hasEnabledMods
                                ? active
                                    ? "opacity-100"
                                    : "opacity-50 hover:opacity-100"
                                : "opacity-100 group-hover/mods:opacity-50 hover:!opacity-100",
                        )}
                        key={mod}
                        onClick={() => onToggle(mod)}
                        title={definition.name}
                        type="button"
                    >
                        <ModBadge mod={{acronym: mod}}/>
                    </button>
                );
            })}
        </div>
    );
}

export function BeatmapLeaderboard({
                                       beatmapId,
                                       isConvert,
                                       isScoreable,
                                       mode,
                                   }: BeatmapLeaderboardProps) {
    const [highlightOptionIndex, setHighlightOptionIndex] = useState([0]);
    const [now] = useState(() => Date.now());
    const [rankingType, setRankingType] = useState<RankingType>("global");
    const [selectedMods, setSelectedMods] = useState<string[]>([]);
    const enabledMods = useMemo(() => [...selectedMods].sort(), [selectedMods]);
    const leaderboardHref = useMemo(
        () => getLeaderboardHref(beatmapId, mode, rankingType, enabledMods),
        [beatmapId, enabledMods, mode, rankingType],
    );
    const {
        data = null,
        error,
        isLoading,
    } = useSWR<BeatmapLeaderboardResponse>(leaderboardHref, {
        keepPreviousData: true,
    });
    const loading = isLoading && data == null;
    const errorMessage = error instanceof Error ? error.message : null;

    const selectedHighlightOption = useMemo(
        () => scoreHighlightOptions[highlightOptionIndex[0]] ?? scoreHighlightOptions[0],
        [highlightOptionIndex],
    );
    const highlightCutoff = useMemo(
        () => getScoreHighlightCutoff(selectedHighlightOption.days, now),
        [now, selectedHighlightOption.days],
    );
    const highlightedCount = useMemo(() => {
        if (data == null) {
            return 0;
        }

        if (highlightCutoff == null) {
            return data.scores.length;
        }

        return data.scores.reduce(
            (count, score) => (isScoreHighlighted(score.ended_at, highlightCutoff) ? count + 1 : count),
            0,
        );
    }, [data, highlightCutoff]);

    return (
        <div className="min-w-0">
            <div>
                <LeaderboardTypeTabs currentValue={rankingType} onSelect={setRankingType}/>

                {isScoreable ? (
                    <LeaderboardModSelector
                        enabledMods={enabledMods}
                        isConvert={isConvert}
                        mode={mode}
                        onToggle={(mod) =>
                            setSelectedMods((currentMods) =>
                                currentMods.includes(mod)
                                    ? currentMods.filter((currentMod) => currentMod !== mod)
                                    : [...currentMods, mod],
                            )
                        }
                    />
                ) : null}

                <div className="mt-2.5 flex justify-end">
                    <HighlightPeriodSelector
                        onSelectedIndexChange={setHighlightOptionIndex}
                        selectedIndex={highlightOptionIndex}
                    />
                </div>
            </div>

            <div className="mt-2.5">
                {loading ? <LoadingRows/> : null}

                {!loading && errorMessage != null ? (
                    <div className="px-2 py-8 text-center text-sm text-white/55">{errorMessage}</div>
                ) : null}

                {!loading && errorMessage == null && data != null && data.scores.length === 0 ? (
                    <div className="px-2 py-8 text-center text-sm text-white/55">{emptyMessages[rankingType]}</div>
                ) : null}

                {!loading && errorMessage == null && data != null && data.scores.length > 0 ? (
                    <div className="overflow-x-auto">
                        <table className="w-full min-w-[60rem] border-separate border-spacing-y-0.5 whitespace-nowrap">
                            <thead>
                            <tr>
                                <HeaderCell align="center" className="w-14">#</HeaderCell>
                                <HeaderCell align="center" className="w-14">Grade</HeaderCell>
                                <HeaderCell>Player</HeaderCell>
                                <HeaderCell className="px-0">Great</HeaderCell>
                                <HeaderCell className="px-0">Ok</HeaderCell>
                                <HeaderCell className="px-0">Meh</HeaderCell>
                                <HeaderCell className="px-0">Miss</HeaderCell>
                                <HeaderCell>Accuracy</HeaderCell>
                                <HeaderCell>Combo</HeaderCell>
                                <HeaderCell>PP</HeaderCell>
                                <HeaderCell>Time</HeaderCell>
                                <HeaderCell>Mods</HeaderCell>
                            </tr>
                            </thead>
                            <tbody>
                            {data.scores.map((score, index) => {
                                const highlighted = isScoreHighlighted(score.ended_at, highlightCutoff);

                                return (
                                    <tr
                                        className={cn(
                                            "group/row transition-opacity",
                                            highlightCutoff != null && !highlighted && "opacity-55",
                                        )}
                                        key={score.id}
                                    >
                                        <BodyCell align="center" className="font-semibold text-white/72">
                                            #{index + 1}
                                        </BodyCell>
                                        <BodyCell align="center">
                                            <RankBadge rank={score.rank}/>
                                        </BodyCell>
                                        <BodyCell>
                                            <div className="flex min-w-0 items-center gap-3">
                                                <img
                                                    alt={score.user.username ?? "User avatar"}
                                                    className="h-8 w-8 rounded-sm object-cover"
                                                    src={resolveAssetUrl(score.user.avatar_url)}
                                                />
                                                <CountryFlag
                                                    className="text-lg"
                                                    code={score.user.country_code}
                                                    name={score.user.country?.name}
                                                />
                                                <Link
                                                    className="min-w-0 truncate font-semibold text-white hover:text-osu-h1"
                                                    href={`/users/${score.user.id}/${mode}`}
                                                    style={
                                                        score.user.profile_colour != null
                                                            ? ({color: score.user.profile_colour} satisfies CSSProperties)
                                                            : undefined
                                                    }
                                                >
                                                    {score.user.username ?? `User ${score.user.id}`}
                                                </Link>
                                            </div>
                                        </BodyCell>
                                        <BodyCell className={getHitCountCellClass(score.statistics.great)}>
                                            {formatInteger(score.statistics.great ?? 0)}
                                        </BodyCell>
                                        <BodyCell className={getHitCountCellClass(score.statistics.ok)}>
                                            {formatInteger(score.statistics.ok ?? 0)}
                                        </BodyCell>
                                        <BodyCell className={getHitCountCellClass(score.statistics.meh)}>
                                            {formatInteger(score.statistics.meh ?? 0)}
                                        </BodyCell>
                                        <BodyCell className={getHitCountCellClass(score.statistics.miss)}>
                                            {formatInteger(score.statistics.miss ?? 0)}
                                        </BodyCell>
                                        <BodyCell className="font-semibold tabular-nums">
                                            {formatAccuracy(score.accuracy)}
                                        </BodyCell>
                                        <BodyCell
                                            className={cn(
                                                "tabular-nums",
                                                score.is_perfect_combo ? "text-osu-orange-3" : "text-white/45",
                                            )}
                                        >
                                            {formatCombo(score.max_combo)}
                                        </BodyCell>
                                        <BodyCell className="font-semibold tabular-nums text-osu-h1">
                                            {score.pp == null ? "-" : formatInteger(Math.round(score.pp))}
                                        </BodyCell>
                                        <BodyCell
                                            className={cn(
                                                isToday(score.ended_at)
                                                    ? "text-white"
                                                    : score.is_perfect_combo
                                                        ? "text-osu-orange-3"
                                                        : "text-white/45",
                                            )}
                                        >
                                            <div title={formatDateTime(score.ended_at) ?? undefined}>
                                                {formatRelativeTime(score.ended_at)}
                                            </div>
                                        </BodyCell>
                                        <BodyCell>
                                            {score.mods.length > 0 ? (
                                                <div className="flex flex-wrap items-center gap-px [&>*]:-mx-[1px]">
                                                    {score.mods.map((mod, modIndex) => (
                                                        <ModBadge key={`${score.id}-${mod.acronym}-${modIndex}`}
                                                                  mod={mod}/>
                                                    ))}
                                                </div>
                                            ) : (
                                                <div className="text-white/35">-</div>
                                            )}
                                        </BodyCell>
                                    </tr>
                                )
                            })}
                            </tbody>
                        </table>
                    </div>
                ) : null}

                {!loading && errorMessage == null && data != null && data.scores.length > 0 && highlightCutoff != null && highlightedCount === 0 ? (
                    <p className="mt-2 text-center text-xs text-osu-f1">No scores landed in the selected period.</p>
                ) : null}
            </div>
        </div>
    );
}

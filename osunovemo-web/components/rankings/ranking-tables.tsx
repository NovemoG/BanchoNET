import React, {CSSProperties, ReactNode} from "react";
import {CountryFlag} from "@/components/country-flag";
import {cn} from "@/lib/utils";
import {
    type CountryRankingEntry,
    type GlobalRankingEntry,
    resolveAssetUrl,
    type TeamRankingEntry,
} from "@/lib/osu-api-common";
import {
    type GlobalRankingSort,
    type Ruleset,
    type TeamRankingSort,
    formatAccuracy,
    formatInteger,
} from "@/lib/rankings";

function rankNumber(page: number, index: number) {
    return (page - 1) * 50 + index + 1;
}

const HeaderCell = ({className, label}: { className?: string; label?: string }) => (
    <th className={cn("px-1.25 py-1.5 text-center font-normal text-osu-f1", className)}>{label}</th>
);

const TableRow = ({className, children, isInactive = false}: {
    className?: string;
    children?: React.ReactNode,
    isInactive?: boolean
}) => (
    <tr className={cn("group/row", isInactive && "opacity-50", className)}>{children}</tr>
);

const TableCell = ({className, children, dimmed = false, main = false}: {
    className?: string;
    children?: React.ReactNode,
    dimmed?: boolean,
    main?: boolean
}) => (
    <td
        className={cn(
            "bg-osu-b4 px-1.25 py-1.5 group-hover/row:bg-osu-b3 first:rounded-l-sm first:pl-2.5 last:rounded-r-sm last:pr-2.5",
            dimmed && "text-osu-f1",
            main && "min-w-50 max-w-125",
            className,
        )}
    >
        {children}
    </td>
);

/**
 * Rendered inside the placement cell rather than as its own column. As a separate <td> it sat
 * between the placement and the user cell, so the gap before the country flag was wider on the
 * performance tab than on every other tab.
 */
const RankChange = ({value}: { value?: number | null }) => {
    if (value == null || value === 0) {
        return null;
    }

    const dropped = value > 0;

    return (
        <span className={cn("ml-1 whitespace-nowrap", dropped ? "text-osu-red-1" : "text-osu-green-1")}>
            {dropped ? "↓" : "↑"}
            {formatInteger(Math.abs(value))}
        </span>
    );
};

const MainUserCell = ({
                          entry,
                          mode,
                      }: {
    entry: GlobalRankingEntry;
    mode: Ruleset;
}) => (
    <div className="inline-grid min-w-0 w-fit grid-flow-col items-center gap-2.5">
        <CountryFlag
            className="text-[20px]"
            code={entry.user.country_code}
            name={entry.user.country?.name}
        />
        {entry.user.team?.flag_url ? (
            <div className="contents text-[20px]">
                {/* eslint-disable-next-line @next/next/no-img-element */}
                <img
                    alt={entry.user.team.name}
                    className="h-5 w-5 rounded-sm object-cover"
                    src={resolveAssetUrl(entry.user.team.flag_url)}
                />
            </div>
        ) : null}
        <a
            className="flex min-w-0 items-center gap-2.5"
            href={`/users/${entry.user.id}/${mode}`}
            style={
                entry.user.profile_colour
                    ? ({color: entry.user.profile_colour} as CSSProperties)
                    : undefined
            }
        >
            <div className="truncate">{entry.user.username}</div>
        </a>
    </div>
);

const MainCountryCell = ({entry}: { entry: CountryRankingEntry }) => (
    <div className="inline-grid min-w-0 w-fit grid-flow-col items-center gap-2.5">
        <CountryFlag className="text-[20px]" code={entry.code} name={entry.country.name}/>
        <div className="flex min-w-0 items-center gap-2.5">
            <div className="truncate">{entry.country.name}</div>
        </div>
    </div>
);

const MainTeamCell = ({entry}: { entry: TeamRankingEntry }) => (
    <div className="inline-grid min-w-0 w-fit grid-flow-col items-center gap-2.5">
        <div className="contents text-[20px]">
            {/* eslint-disable-next-line @next/next/no-img-element */}
            <img
                alt={entry.team.name}
                className="h-5 w-5 rounded-sm object-cover"
                src={resolveAssetUrl(entry.team.flag_url)}
            />
        </div>
        <div className="flex min-w-0 items-center gap-2.5">
            <div className="truncate">{entry.team.name}</div>
        </div>
    </div>
);

function TableShell({children}: { children: ReactNode }) {
    return (
        <table className="w-full border-separate border-spacing-y-0.75 whitespace-nowrap text-center">
            {children}
        </table>
    );
}

export function GlobalRankingTable({
                                       rankings,
                                       page,
                                       sort,
                                       showRankChange,
                                       mode,
                                   }: {
    rankings: GlobalRankingEntry[];
    page: number;
    sort: GlobalRankingSort;
    showRankChange: boolean;
    mode: Ruleset;
}) {
    return (
        <TableShell>
            <thead>
            <tr>
                <th/>
                <HeaderCell className="w-full"/>
                <HeaderCell className={cn(sort === "accuracy" && "text-osu-c1")} label="Accuracy"/>
                <HeaderCell label="Play Count"/>
                <HeaderCell className={cn(sort === "score" && "text-osu-c1")} label="Ranked Score"/>
                <HeaderCell className={cn(sort === "performance" && "text-osu-c1")} label="Performance"/>
                <HeaderCell className="px-2.5" label="SS"/>
                <HeaderCell className="px-2.5" label="S"/>
                <HeaderCell className="px-2.5" label="A"/>
            </tr>
            </thead>
            <tbody>
            {rankings.map((entry, index) => (
                <TableRow key={entry.user.id} isInactive={!entry.user.is_active}>
                    <TableCell className="whitespace-nowrap">
                        #{formatInteger(rankNumber(page, index))}
                        {showRankChange && <RankChange value={entry.rank_change_since_30_days}/>}
                    </TableCell>
                    <TableCell main={true} className="text-left">
                        <MainUserCell entry={entry} mode={mode}/>
                    </TableCell>
                    <TableCell dimmed={sort !== "accuracy"}>
                        {formatAccuracy(entry.accuracy)}
                    </TableCell>
                    <TableCell dimmed={true}>
                        {formatInteger(entry.play_count)}
                    </TableCell>
                    <TableCell dimmed={sort !== "score"}>
                        {formatInteger(Math.round(entry.ranked_score))}
                    </TableCell>
                    <TableCell dimmed={sort !== "performance"}>
                        {formatInteger(Math.round(entry.pp))}
                    </TableCell>
                    <TableCell dimmed={true}>
                        {formatInteger((entry.grade_counts.ss ?? 0) + (entry.grade_counts.ssh ?? 0))}
                    </TableCell>
                    <TableCell dimmed={true}>
                        {formatInteger((entry.grade_counts.s ?? 0) + (entry.grade_counts.sh ?? 0))}
                    </TableCell>
                    <TableCell dimmed={true}>
                        {formatInteger(entry.grade_counts.a ?? 0)}
                    </TableCell>
                </TableRow>
            ))}
            </tbody>
        </TableShell>
    );
}

export function CountryRankingTable({
                                        rankings,
                                        page,
                                    }: {
    rankings: CountryRankingEntry[];
    page: number;
}) {
    return (
        <TableShell>
            <thead>
            <tr>
                <th/>
                <HeaderCell className="w-full"/>
                <HeaderCell label="Active Users"/>
                <HeaderCell label="Play Count"/>
                <HeaderCell label="Ranked Score"/>
                <HeaderCell label="Average Score"/>
                <HeaderCell className="text-osu-c1" label="Performance"/>
            </tr>
            </thead>
            <tbody>
            {rankings.map((entry, index) => (
                <TableRow key={entry.code}>
                    <TableCell>
                        #{formatInteger(rankNumber(page, index))}
                    </TableCell>
                    <TableCell main={true} className="text-left">
                        <MainCountryCell entry={entry}/>
                    </TableCell>
                    <TableCell dimmed={true}>
                        {formatInteger(entry.active_users)}
                    </TableCell>
                    <TableCell dimmed={true}>
                        {formatInteger(entry.play_count)}
                    </TableCell>
                    <TableCell dimmed={true}>
                        {formatInteger(Math.round(entry.ranked_score))}
                    </TableCell>
                    <TableCell dimmed={true}>
                        {formatInteger(
                            Math.round(entry.ranked_score / Math.max(entry.active_users, 1)),
                        )}
                    </TableCell>
                    <TableCell>
                        {formatInteger(Math.round(entry.performance))}
                    </TableCell>
                </TableRow>
            ))}
            </tbody>
        </TableShell>
    );
}

export function TeamRankingTable({
                                     rankings,
                                     page,
                                     sort,
                                 }: {
    rankings: TeamRankingEntry[];
    page: number;
    sort: TeamRankingSort;
}) {
    return (
        <TableShell>
            <thead>
            <tr>
                <th/>
                <HeaderCell className="w-full"/>
                <HeaderCell label="Members"/>
                <HeaderCell label="Play Count"/>
                <HeaderCell className={cn(sort === "score" && "text-osu-c1")} label="Ranked Score"/>
                <HeaderCell label="Average Score"/>
                <HeaderCell className={cn(sort === "performance" && "text-osu-c1")} label="Performance"/>
            </tr>
            </thead>
            <tbody>
            {rankings.map((entry, index) => (
                <TableRow key={entry.team.id}>
                    <TableCell>#{formatInteger(rankNumber(page, index))}</TableCell>
                    <TableCell main={true} className="text-left">
                        <MainTeamCell entry={entry}/>
                    </TableCell>
                    <TableCell dimmed={true}>{formatInteger(entry.member_count)}</TableCell>
                    <TableCell dimmed={true}>{formatInteger(entry.play_count)}</TableCell>
                    <TableCell dimmed={sort !== "score"}>
                        {formatInteger(Math.round(entry.ranked_score))}
                    </TableCell>
                    <TableCell dimmed={true}>
                        {formatInteger(
                            Math.round(entry.ranked_score / Math.max(entry.member_count, 1)),
                        )}
                    </TableCell>
                    <TableCell dimmed={sort !== "performance"}>
                        {formatInteger(Math.round(entry.performance))}
                    </TableCell>
                </TableRow>
            ))}
            </tbody>
        </TableShell>
    );
}

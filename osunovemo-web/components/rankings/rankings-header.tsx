import Image from "next/image";
import Link from "next/link";
import {ChevronDown} from "lucide-react";
import {Header} from "@/components/header";
import {
    buildRankingsHref,
    DEFAULT_FILTER,
    getRankingTypeLabel,
    normalizeSortForType,
    parseRankingState,
    rankingModes,
    type RankingSearchState,
} from "@/lib/rankings";
import {cn} from "@/lib/utils";

const desktopNavLinkClassName =
    "relative z-1 flex items-baseline gap-1.25 py-2.5 text-osu-c1 transition-colors before:absolute before:-bottom-0.5 before:left-0 before:hidden before:h-1.25 before:w-full before:scale-y-0 before:rounded-full before:bg-osu-h1 before:transition-transform hover:text-white md:py-4 md:before:block hover:before:scale-y-100";
const mobileNavMenuClassName =
    "absolute inset-x-0 top-full z-[101] -mx-4 hidden list-none bg-osu-d5 p-0 group-open:grid sm:-mx-6 lg:-mx-8";
const mobileNavToggleClassName =
    "relative block w-max max-w-full list-none py-2.5 text-white marker:hidden before:absolute before:-bottom-0.5 before:left-0 before:block before:h-1 before:w-full before:rounded-full before:bg-osu-h1 before:content-['']";
const gameModeLinkClassName =
    "group flex items-center gap-1 p-0 text-[20px] [text-shadow:0_1px_2px_rgba(0,0,0,0.45)]";

const rankingHeaderTypes = ["global", "country", "top-plays", "team"] as const;

type RankingHeaderType = (typeof rankingHeaderTypes)[number];

type RankingsHeaderProps = {
    mode: (typeof rankingModes)[number];
    state: ReturnType<typeof parseRankingState>;
    type: RankingHeaderType;
};

function getHeaderTypeLabel(type: RankingHeaderType) {
    return type === "top-plays" ? "top plays" : getRankingTypeLabel(type);
}

function buildModeHref(
    state: RankingSearchState,
    mode: (typeof rankingModes)[number],
    type: RankingHeaderType,
) {
    if (type === "top-plays") {
        return `/rankings/top-plays/${mode}`;
    }

    return buildRankingsHref({
        ...state,
        mode,
        page: 1,
        variant: mode === state.mode ? state.variant : null,
    });
}

function buildTypeHref(
    state: RankingSearchState,
    type: RankingHeaderType,
) {
    if (type === "top-plays") {
        return `/rankings/top-plays/${state.mode}`;
    }

    if (type === "global") {
        return buildRankingsHref({
            ...state,
            filter: state.type === "global" ? state.filter : DEFAULT_FILTER,
            page: 1,
            type,
        });
    }

    return buildRankingsHref({
        ...state,
        country: null,
        filter: DEFAULT_FILTER,
        page: 1,
        sort: normalizeSortForType(type, state.sort),
        type,
        variant: null,
    });
}

function HeaderNav({
                       links,
                       mobileLabel,
                   }: {
    links: Array<{ active: boolean; href: string; label: string }>;
    mobileLabel: string;
}) {
    return (
        <>
            <ul className="relative hidden items-center gap-5 text-xs md:flex md:text-sm before:absolute before:bottom-0 before:left-0 before:right-0 before:h-px before:bg-osu-h1">
                {links.map((link) => (
                    <li className="relative flex" key={link.href}>
                        <Link
                            className={cn(
                                desktopNavLinkClassName,
                                link.active && "font-semibold text-white before:scale-y-100",
                            )}
                            href={link.href}
                        >
                            <span>{link.label}</span>
                        </Link>
                    </li>
                ))}
            </ul>

            <details className="group relative w-full text-xs md:hidden">
                <summary className={mobileNavToggleClassName}>
                    {mobileLabel}
                    <span className="absolute left-full top-0 flex h-full items-center pl-2.5 text-[0.8em] transition-transform group-open:rotate-180">
                        <ChevronDown className="h-4 w-4"/>
                    </span>
                </summary>
                <ul className={mobileNavMenuClassName}>
                    {links.map((link) => (
                        <li key={link.href}>
                            <Link
                                className={cn(
                                    "block px-4 py-2.5 text-white transition-colors hover:bg-osu-d3 sm:px-6 lg:px-8",
                                    link.active && "bg-osu-d4 font-semibold",
                                )}
                                href={link.href}
                            >
                                {link.label}
                            </Link>
                        </li>
                    ))}
                </ul>
            </details>
        </>
    );
}

function RulesetSelector({
                             mode,
                             state,
                             type,
                             className,
                             compact = false,
                         }: {
    mode: (typeof rankingModes)[number];
    state: RankingSearchState;
    type: RankingHeaderType;
    className?: string;
    compact?: boolean;
}) {
    return (
        <ul
            className={cn(
                "flex items-center text-sm leading-normal",
                compact ? "h-auto gap-3 sm:hidden" : "absolute right-4 top-0 h-full gap-x-5 gap-y-2.5 sm:right-[50px]",
                className,
            )}
        >
            {rankingModes.map((value) => (
                <li key={value}>
                    <Link
                        className={cn(
                            gameModeLinkClassName,
                            value === mode && "font-bold",
                        )}
                        href={buildModeHref(state, value, type)}
                        title={value}
                    >
                        <span
                            className={cn(
                                `fa-extra-mode-${value}`,
                                "text-osu-h1 transition-colors group-hover:text-white",
                                value === mode && "text-white",
                            )}
                        />
                        <span className="sr-only">{value}</span>
                    </Link>
                </li>
            ))}
        </ul>
    );
}

function RankingsBackground() {
    return (
        <div className="absolute inset-0 overflow-hidden bg-osu-d5">
            <div
                className="absolute inset-0 opacity-70"
                style={{
                    backgroundImage:
                        "linear-gradient(to right, hsl(var(--hsl-d5)), transparent 18%, transparent 82%, hsl(var(--hsl-d5))), url('/headers/rankings.jpg')",
                    backgroundPosition: "center",
                    backgroundRepeat: "no-repeat",
                    backgroundSize: "cover",
                }}
            />
            <div className="absolute inset-0 bg-[linear-gradient(180deg,rgba(12,16,20,0.24),rgba(12,16,20,0.72))]"/>
        </div>
    );
}

export function RankingsHeader({mode, state, type}: RankingsHeaderProps) {
    const typeLinks = rankingHeaderTypes.map((value) => ({
        active: value === type,
        href: buildTypeHref(state, value),
        label: getHeaderTypeLabel(value),
    }));

    return (
        <Header
            background={<RankingsBackground/>}
            bottom={(
                <>
                    <HeaderNav
                        links={typeLinks}
                        mobileLabel={getHeaderTypeLabel(type)}
                    />
                    <RulesetSelector
                        compact
                        mode={mode}
                        state={state}
                        type={type}
                    />
                </>
            )}
            bottomClassName="bg-osu-d4/95"
            icon={(
                <span className="relative h-8 w-8 shrink-0 md:h-9 md:w-9">
                    <Image
                        alt=""
                        fill
                        priority
                        src="/icons/rankings.svg"
                    />
                </span>
            )}
            mobileSubtitle={getHeaderTypeLabel(type)}
            title="rankings"
            topClassName="bg-osu-d5/92 text-osu-c1"
            topRight={<RulesetSelector className="hidden sm:flex" mode={mode} state={state} type={type}/>}
        />
    );
}

"use client";

import Image from "next/image";
import Link from "next/link";
import useSWR from "swr";
import {
    useCallback,
    useEffect,
    useMemo,
    useRef,
    useState,
    type AnchorHTMLAttributes,
    type MouseEvent,
    type ReactNode,
} from "react";
import {
    ChevronDown,
    ChevronUp,
    Grid2X2,
    Grid3X3,
    ImageIcon,
    LayoutGrid,
    List,
    Music2,
    SquareStack,
    type LucideIcon,
} from "lucide-react";
import {useSession} from "@/components/auth/session-provider";
import {Header} from "@/components/header";
import {PageWrapper} from "@/components/page-wrapper";
import {
    BEATMAPSET_SEARCH_VIEW_OPTIONS,
    BEATMAPSET_SEARCH_EXTRA_OPTIONS,
    BEATMAPSET_SEARCH_GENERAL_OPTIONS,
    BEATMAPSET_SEARCH_GENRE_OPTIONS,
    BEATMAPSET_SEARCH_LANGUAGE_OPTIONS,
    BEATMAPSET_SEARCH_MODE_OPTIONS,
    BEATMAPSET_SEARCH_NSFW_OPTIONS,
    BEATMAPSET_SEARCH_PLAYED_OPTIONS,
    BEATMAPSET_SEARCH_RANK_OPTIONS,
    BEATMAPSET_SEARCH_STATUS_OPTIONS,
    buildBeatmapsetSearchHref,
    buildBeatmapsetSearchParams,
    getPublicBeatmapsetSearchState,
    getBeatmapsetSearchSortLabel,
    getVisibleBeatmapsetSearchSortFields,
    parseBeatmapsetSearchState,
    setBeatmapsetSearchMultiValues,
    setBeatmapsetSearchView,
    toggleBeatmapsetSearchMultiValue,
    toggleBeatmapsetSearchSort,
    type BeatmapsetSearchOption,
    type BeatmapsetSearchState,
    type BeatmapsetSearchView,
} from "@/lib/beatmapset-search";
import {BEATMAPSET_SEARCH_ENDPOINT, type BeatmapsetSearchData} from "@/lib/beatmapset-search-types";
import {BeatmapsetSearchInfiniteResults} from "@/components/beatmapsets-search/infinite-results";
import {BeatmapsetSearchQueryForm} from "@/components/beatmapsets-search/search-query-form";
import {cn} from "@/lib/utils";

type BeatmapsetSearchPageProps = {
    data: BeatmapsetSearchData;
    osuApiOrigin: string;
    state: BeatmapsetSearchState;
};

type SearchNavigationHandler = (href: string, event: MouseEvent<HTMLAnchorElement>) => void;

type FilterChipLinkProps = {
    active: boolean;
    direction: "asc" | "desc";
    href: string;
    label: string;
    onNavigate: SearchNavigationHandler;
};

type ViewModeLinkProps = {
    active: boolean;
    icon: LucideIcon;
    label: string;
    onSelect: () => void;
};

type TextFilterGroupProps = {
    defaultValue?: string | null;
    label: string;
    onNavigate: SearchNavigationHandler;
    options: readonly BeatmapsetSearchOption<string | null>[];
    selectedValues: readonly string[];
    state: BeatmapsetSearchState;
    updateKey: "genre" | "language" | "mode" | "played" | "status";
};

type MultiFilterGroupProps = {
    label: string;
    onNavigate: SearchNavigationHandler;
    options: readonly BeatmapsetSearchOption<string>[];
    selectedValues: readonly string[];
    state: BeatmapsetSearchState;
    updateKey: "extra" | "general" | "rank";
};

const scoreRankAssets: Record<string, string> = {
    A: "/badges/score-ranks-v2019/GradeSmall-A.svg",
    B: "/badges/score-ranks-v2019/GradeSmall-B.svg",
    C: "/badges/score-ranks-v2019/GradeSmall-C.svg",
    D: "/badges/score-ranks-v2019/GradeSmall-D.svg",
    S: "/badges/score-ranks-v2019/GradeSmall-S.svg",
    SH: "/badges/score-ranks-v2019/GradeSmall-S-Silver.svg",
    X: "/badges/score-ranks-v2019/GradeSmall-SS.svg",
    XH: "/badges/score-ranks-v2019/GradeSmall-SS-Silver.svg",
};

const statusPillClassNames: Record<string, string> = {
    any: "bg-osu-b3 text-osu-c2",
    favourites: "bg-osu-b3 text-osu-c1",
    graveyard: "bg-osu-b6 text-osu-f1",
    leaderboard: "bg-osu-b3 text-osu-c1",
    loved: "bg-osu-h1 text-osu-b6",
    mine: "bg-osu-b3 text-osu-c1",
    pending: "bg-osu-orange-3 text-osu-b6",
    qualified: "bg-[#63b4ff] text-osu-b6",
    ranked: "bg-osu-green-1 text-osu-b6",
    wip: "bg-osu-b3 text-osu-c1",
};

const beatmapsetSearchViewStorageKey = "osunovemo.beatmapsets.search.view";

const beatmapsetSearchViewIcons: Record<BeatmapsetSearchView, LucideIcon> = {
    nano: Grid3X3,
    mini: Grid2X2,
    normal: LayoutGrid,
    extra: SquareStack,
    cover: ImageIcon,
    list: List,
};

function shouldHandleClientNavigation(event: MouseEvent<HTMLAnchorElement>) {
    return (
        !event.defaultPrevented &&
        event.button === 0 &&
        !event.altKey &&
        !event.ctrlKey &&
        !event.metaKey &&
        !event.shiftKey
    );
}

function SearchAnchor({
                          href,
                          onNavigate,
                          ...props
                      }: Omit<AnchorHTMLAttributes<HTMLAnchorElement>, "href" | "onClick"> & {
    href: string;
    onNavigate: SearchNavigationHandler;
}) {
    return (
        <a
            {...props}
            href={href}
            onClick={(event) => {
                if (!shouldHandleClientNavigation(event)) {
                    return;
                }

                event.preventDefault();
                onNavigate(href, event);
            }}
        />
    );
}

function isBeatmapsetSearchView(value: string | null): value is BeatmapsetSearchView {
    return BEATMAPSET_SEARCH_VIEW_OPTIONS.some((option) => option.value === value);
}

function readStoredBeatmapsetSearchView() {
    try {
        const value = window.localStorage.getItem(beatmapsetSearchViewStorageKey);
        return isBeatmapsetSearchView(value) ? value : null;
    } catch {
        return null;
    }
}

function writeStoredBeatmapsetSearchView(view: BeatmapsetSearchView) {
    try {
        window.localStorage.setItem(beatmapsetSearchViewStorageKey, view);
    } catch {
        // Ignore storage failures; the selected view still works for the current session.
    }
}

function searchParamsToQueryParams(searchParams: URLSearchParams) {
    const params: Record<string, string | string[] | undefined> = {};

    searchParams.forEach((value, key) => {
        params[key] = value;
    });

    return params;
}

function getClientSearchHref(state: BeatmapsetSearchState) {
    return buildBeatmapsetSearchHref(state, ["view"]);
}

function getBeatmapsetSearchApiHref(state: BeatmapsetSearchState) {
    const search = buildBeatmapsetSearchParams(state, ["view"]).toString();
    return search.length > 0 ? `/api/beatmapsets/search?${search}` : "/api/beatmapsets/search";
}

async function fetchBeatmapsetSearchData(url: string) {
    const response = await fetch(url, {cache: "no-store"});

    return (await response.json()) as BeatmapsetSearchData;
}

function getBeatmapsetSearchErrorData(error: unknown): BeatmapsetSearchData {
    const message = error instanceof Error ? error.message : "Could not load beatmapsets.";

    return {
        backendAvailable: false,
        beatmapsets: [],
        cursor: null,
        endpoint: BEATMAPSET_SEARCH_ENDPOINT,
        error: message,
        hasNextPage: false,
        recommendedDifficulty: null,
        total: 0,
    };
}

function getBeatmapsetSearchDataVersion(data: BeatmapsetSearchData) {
    const cursor = data.cursor == null ? "" : JSON.stringify(data.cursor);
    const ids = data.beatmapsets.map((beatmapset) => beatmapset.id).join(",");

    return [
        data.backendAvailable ? "available" : "unavailable",
        data.error ?? "",
        data.total ?? "",
        cursor,
        ids,
    ].join(":");
}

function resolveSearchAssetUrl(url: string, osuApiOrigin: string) {
    if (/^https?:\/\//i.test(url)) {
        return url;
    }

    return new URL(url, `${osuApiOrigin}/`).toString();
}

function FilterChipLink({active, direction, href, label, onNavigate}: FilterChipLinkProps) {
    return (
        <SearchAnchor
            className={cn(
                "group m-[5px] inline-flex items-center rounded-md px-2.5 py-1 text-sm text-osu-l1 transition-colors",
                active
                    ? "bg-osu-b3 font-semibold text-white"
                    : "hover:bg-osu-b3 hover:text-osu-l1",
            )}
            href={href}
            onNavigate={onNavigate}
        >
            {label}
            <span className={cn("ml-[5px] transition-opacity", active ? "opacity-100" : "opacity-0 group-hover:opacity-100")}>
                {direction === "asc" ? <ChevronUp className="h-3.5 w-3.5"/> : <ChevronDown className="h-3.5 w-3.5"/>}
            </span>
        </SearchAnchor>
    );
}

function ViewModeLink({active, icon: Icon, label, onSelect}: ViewModeLinkProps) {
    return (
        <button
            aria-pressed={active}
            className={cn(
                "m-[5px] inline-flex size-8 items-center justify-center rounded-md text-sm transition-colors",
                active ? "bg-osu-b3 font-semibold text-white" : "text-osu-l1 hover:bg-osu-b3 hover:text-osu-l1",
            )}
            onClick={() => {
                onSelect();
            }}
            title={label}
            type="button"
        >
            <Icon aria-hidden className="h-4 w-4"/>
            <span className="sr-only">{label}</span>
        </button>
    );
}

function HeaderNav() {
    const links = [
        {active: true, href: "/beatmapsets", label: "beatmapsets"},
        {active: false, label: "beatmaps"},
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
                                <span>{link.label}</span>
                            </Link>
                        ) : (
                            <span className="relative z-1 flex items-baseline gap-1.25 py-2.5 text-osu-c1/45 md:py-4">
                                <span>{link.label}</span>
                            </span>
                        )}
                    </li>
                ))}
            </ul>

            <details className="group relative w-full text-xs md:hidden">
                <summary
                    className="relative block w-max max-w-full list-none py-2.5 text-white marker:hidden before:absolute before:-bottom-0.5 before:left-0 before:block before:h-1 before:w-full before:rounded-full before:bg-osu-h1 before:content-['']">
                    {activeLink.label}
                    <span
                        className="absolute top-0 left-full flex h-full items-center pl-2.5 text-[0.8em] transition-transform group-open:rotate-180">
                        <ChevronDown className="h-4 w-4"/>
                    </span>
                </summary>
                <ul className="absolute inset-x-0 top-full z-101 -mx-4 hidden list-none bg-osu-d5 p-0 group-open:grid sm:-mx-6 lg:-mx-8">
                    {links.map((link) => (
                        <li key={link.label}>
                            {"href" in link ? (
                                <Link
                                    className={cn(
                                        "flex px-4 py-3 text-white/70 transition-colors hover:bg-white/5 hover:text-white sm:px-6 lg:px-8",
                                        link.active && "bg-white/8 text-white",
                                    )}
                                    href={link.href}
                                >
                                    {link.label}
                                </Link>
                            ) : (
                                <span className="flex px-4 py-3 text-osu-c1/45 sm:px-6 lg:px-8">{link.label}</span>
                            )}
                        </li>
                    ))}
                </ul>
            </details>
        </>
    );
}

function SearchFilterSection({
                                 children,
                                 className,
                                 decorationClassName,
                                 headerClassName,
                                 label,
                             }: {
    children: ReactNode;
    className?: string;
    decorationClassName?: string;
    headerClassName?: string;
    label: string;
}) {
    return (
        <div className={cn("flex items-start gap-4 xl:block xl:space-y-3", className)}>
            <div className={cn("w-[11rem] shrink-0 xl:w-auto", headerClassName)}>
                <div className="flex items-center gap-3 xl:block xl:space-y-3">
                    <div className={cn("mt-0.5 h-[1.05rem] w-1 rounded-full bg-white/12 xl:h-1 xl:w-full xl:max-w-[11rem]", decorationClassName)}/>
                    <div className="text-[1.05rem] font-bold leading-tight text-white">
                        {label}
                    </div>
                </div>
            </div>
            <div className="min-w-0 flex-1">
                {children}
            </div>
        </div>
    );
}

function FilterBullet({active, className}: { active: boolean; className?: string }) {
    return (
        <span
            className={cn(
                "h-1.5 w-1.5 shrink-0 rounded-full bg-osu-h1 transition-[opacity,box-shadow]",
                className,
                active
                    ? "opacity-100 shadow-[0_0_0_2px_rgba(255,102,171,0.16),0_0_10px_rgba(255,102,171,0.78)]"
                    : "opacity-0",
            )}
        />
    );
}

/**
 * These groups (mode, categories, genre, language, played) are single select. An empty selection
 * means "whatever the group's default is", so that is the only case where the default lights up.
 */
function isSingleFilterOptionActive(
    optionValue: string | null,
    selectedValues: readonly string[],
    defaultValue?: string | null,
) {
    if (selectedValues.length === 0) {
        return optionValue === (defaultValue ?? null);
    }

    return optionValue != null && selectedValues.includes(optionValue);
}

function buildSingleFilterHref(
    state: BeatmapsetSearchState,
    key: TextFilterGroupProps["updateKey"],
    value: string | null,
    defaultValue?: string | null,
) {
    // Replace rather than toggle: picking taiko while osu was selected used to apply both.
    //
    // "any" is written to the URL like any other value instead of being represented as an absent
    // one. Categories defaults to "leaderboard", so clearing the selection made Leaderboard light
    // up again and Any was impossible to select.
    const next = value == null || value === (defaultValue ?? null) ? [] : [value];

    return buildBeatmapsetSearchHref(setBeatmapsetSearchMultiValues(state, key, next), ["view"]);
}

function SearchTextFilterItem({
                                  active,
                                  featured = false,
                                  href,
                                  label,
                                  onNavigate,
                              }: {
    active: boolean;
    featured?: boolean;
    href: string;
    label: string;
    onNavigate: SearchNavigationHandler;
}) {
    return (
        <SearchAnchor
            className={cn(
                "flex items-center gap-2 text-[15px] leading-[1.35] transition-colors hover:text-osu-l1",
                active ? "font-semibold" : "text-osu-l2",
                featured && !active && "text-osu-orange-3 hover:text-osu-orange-3",
            )}
            href={href}
            onNavigate={onNavigate}
        >
            <FilterBullet active={active}/>
            <span className={cn(active && "text-white")}>{label}</span>
        </SearchAnchor>
    );
}

function SearchStatusFilterItem({
                                    active,
                                    href,
                                    label,
                                    onNavigate,
                                    value,
                                }: {
    active: boolean;
    href: string;
    label: string;
    onNavigate: SearchNavigationHandler;
    value: string;
}) {
    return (
        <SearchAnchor className="flex items-center gap-2" href={href} onNavigate={onNavigate}>
            <FilterBullet active={active}/>
            <span
                className={cn(
                    "inline-flex h-5 w-[7rem] items-center justify-center rounded-full px-2 py-0 text-[0.9rem] font-bold uppercase leading-none whitespace-nowrap transition-opacity",
                    statusPillClassNames[value] ?? "bg-osu-b3 text-osu-c2",
                    !active && "opacity-60 hover:opacity-100",
                    active && "shadow-[0_0_10px_rgba(255,255,255,0.06)]",
                )}
            >
                {label}
            </span>
        </SearchAnchor>
    );
}

function SearchRankFilterItem({
                                  active,
                                  href,
                                  label,
                                  onNavigate,
                                  value,
                              }: {
    active: boolean;
    href: string;
    label: string;
    onNavigate: SearchNavigationHandler;
    value: string;
}) {
    return (
        <SearchAnchor
            className="grid grid-cols-[0.5rem_auto] items-center gap-1.5"
            href={href}
            onNavigate={onNavigate}
        >
            <FilterBullet active={active}/>
            <Image
                alt={label}
                className={cn("h-[1.65rem] w-auto object-contain transition-opacity", active ? "opacity-100" : "opacity-80 hover:opacity-100")}
                height={26}
                src={scoreRankAssets[value]}
                width={52}
            />
        </SearchAnchor>
    );
}

function TextFilterGroup({
                             defaultValue,
                             label,
                             onNavigate,
                             options,
                             selectedValues,
                             state,
                             updateKey,
                         }: TextFilterGroupProps) {
    return (
        <SearchFilterSection headerClassName="xl:pl-4" label={label}>
            <div className="flex flex-wrap gap-x-4 gap-y-1 xl:block xl:space-y-[0.12rem]">
                {options.map((option) => {
                    const displayLabel =
                        option.value === "recommended"
                            ? `${option.label} (0.0)`
                            : option.label;
                    const href = buildSingleFilterHref(state, updateKey, option.value, defaultValue);

                    return (
                        <SearchTextFilterItem
                            active={isSingleFilterOptionActive(option.value, selectedValues, defaultValue)}
                            featured={option.value === "featured_artists"}
                            href={href}
                            key={`${label}-${option.value ?? "any"}`}
                            label={displayLabel}
                            onNavigate={onNavigate}
                        />
                    );
                })}
            </div>
        </SearchFilterSection>
    );
}

function MultiTextFilterGroup({
                                  fullWidthDecoration = false,
                                  label,
                                  onNavigate,
                                  options,
                                  selectedValues,
                                  state,
                                  updateKey,
                              }: Omit<MultiFilterGroupProps, "updateKey"> & {
    fullWidthDecoration?: boolean;
    updateKey: "extra" | "general";
}) {
    return (
        <SearchFilterSection
            decorationClassName={fullWidthDecoration ? "max-w-none" : undefined}
            headerClassName="xl:pl-4"
            label={label}
        >
            <div className="flex flex-wrap gap-x-4 gap-y-1 xl:block xl:space-y-[0.12rem]">
                {options.map((option) => {
                    const optionValue = option.value as BeatmapsetSearchState[typeof updateKey][number];
                    const displayLabel =
                        optionValue === "recommended"
                            ? `${option.label} (0.0)`
                            : option.label;
                    const href = buildBeatmapsetSearchHref(
                        toggleBeatmapsetSearchMultiValue(state, updateKey, optionValue),
                        ["view"],
                    );

                    return (
                        <SearchTextFilterItem
                            active={selectedValues.includes(optionValue)}
                            featured={optionValue === "featured_artists"}
                            href={href}
                            key={`${label}-${optionValue}`}
                            label={displayLabel}
                            onNavigate={onNavigate}
                        />
                    );
                })}
            </div>
        </SearchFilterSection>
    );
}

function RankFilterGroup({
                             fullWidthDecoration = false,
                             label,
                             onNavigate,
                             options,
                             selectedValues,
                             state,
                             updateKey,
                         }: MultiFilterGroupProps & {
    fullWidthDecoration?: boolean;
}) {
    return (
        <SearchFilterSection
            decorationClassName={fullWidthDecoration ? "max-w-none" : undefined}
            headerClassName="xl:pl-4"
            label={label}
        >
            <div className="grid grid-cols-2 gap-x-3 gap-y-2 sm:grid-cols-4">
                {options.map((option) => {
                    const optionValue = option.value as BeatmapsetSearchState[typeof updateKey][number];
                    const href = buildBeatmapsetSearchHref(
                        toggleBeatmapsetSearchMultiValue(state, updateKey, optionValue),
                        ["view"],
                    );

                    return (
                        <SearchRankFilterItem
                            active={selectedValues.includes(optionValue)}
                            href={href}
                            key={`${label}-${optionValue}`}
                            label={option.label}
                            onNavigate={onNavigate}
                            value={optionValue}
                        />
                    );
                })}
            </div>
        </SearchFilterSection>
    );
}

function StatusFilterGroup({
                               defaultValue,
                               label,
                               options,
                               selectedValues,
                               state,
                           updateKey,
                           onNavigate,
                       }: TextFilterGroupProps) {
    return (
        <SearchFilterSection headerClassName="xl:pl-4" label={label}>
            <div className="flex flex-wrap gap-x-4 gap-y-2 xl:block xl:space-y-1">
                {options.map((option) => {
                    const href = buildSingleFilterHref(state, updateKey, option.value, defaultValue);

                    return (
                        <SearchStatusFilterItem
                            active={isSingleFilterOptionActive(option.value, selectedValues, defaultValue)}
                            href={href}
                            key={`${label}-${option.value ?? "any"}`}
                            label={option.label}
                            onNavigate={onNavigate}
                            value={option.value ?? "any"}
                        />
                    );
                })}
            </div>
        </SearchFilterSection>
    );
}

function ExplicitContentFilterGroup({
                                        onNavigate,
                                        state,
                                    }: {
    onNavigate: SearchNavigationHandler;
    state: BeatmapsetSearchState;
}) {
    return (
        <SearchFilterSection headerClassName="xl:pl-4" label="Explicit Content">
            <div className="flex flex-wrap gap-x-4 gap-y-1 xl:block xl:space-y-[0.12rem]">
                {BEATMAPSET_SEARCH_NSFW_OPTIONS.map((option) => {
                    const href = buildBeatmapsetSearchHref({
                        ...state,
                        nsfw: option.value === "1",
                        page: 1,
                    }, ["view"]);

                    return (
                        <SearchTextFilterItem
                            active={state.nsfw === (option.value === "1")}
                            href={href}
                            key={`explicit-${option.value}`}
                            label={option.label}
                            onNavigate={onNavigate}
                        />
                    );
                })}
            </div>
        </SearchFilterSection>
    );
}

function getSearchHeroCoverUrl(data: BeatmapsetSearchData, osuApiOrigin: string) {
    const cover = data.beatmapsets[0]?.covers.cover;

    if (cover == null || cover.length === 0) {
        return null;
    }

    return resolveSearchAssetUrl(cover, osuApiOrigin);
}

export function BeatmapsetSearchPage({
    data: initialData,
    osuApiOrigin,
    state: initialState,
}: BeatmapsetSearchPageProps) {
    const {status} = useSession();
    const canUseSearchControls = status === "authenticated";
    const [state, setState] = useState(initialState);
    const stateRef = useRef(initialState);
    const searchKey = useMemo(() => getBeatmapsetSearchApiHref(state), [state]);
    const {
        data: searchData,
        error: searchError,
        isValidating: isSearchReloading,
    } = useSWR<BeatmapsetSearchData>(searchKey, fetchBeatmapsetSearchData, {
        fallbackData: initialData,
        keepPreviousData: true,
        revalidateOnMount: false,
    });
    const data = useMemo(
        () => searchError == null ? searchData ?? initialData : getBeatmapsetSearchErrorData(searchError),
        [initialData, searchData, searchError],
    );
    const dataVersion = useMemo(() => getBeatmapsetSearchDataVersion(data), [data]);

    const updateSearchState = useCallback((nextState: BeatmapsetSearchState, historyMode: "push" | "replace" = "push") => {
        if (!canUseSearchControls) {
            return;
        }

        const currentState = stateRef.current;
        const currentSearchHref = getClientSearchHref(currentState);
        const nextSearchHref = getClientSearchHref(nextState);
        const didSearchChange = currentSearchHref !== nextSearchHref;

        stateRef.current = nextState;
        setState(nextState);

        if (!didSearchChange) {
            return;
        }

        window.history[historyMode === "replace" ? "replaceState" : "pushState"](null, "", nextSearchHref);
    }, [canUseSearchControls]);

    const navigateToSearchHref = useCallback((href: string) => {
        if (!canUseSearchControls) {
            return;
        }

        const url = new URL(href, window.location.origin);
        const parsedState = parseBeatmapsetSearchState(searchParamsToQueryParams(url.searchParams));

        updateSearchState({
            ...parsedState,
            view: stateRef.current.view,
        });
    }, [canUseSearchControls, updateSearchState]);

    const handleSearchNavigation = useCallback<SearchNavigationHandler>((href, event) => {
        if (!event.defaultPrevented) {
            if (!shouldHandleClientNavigation(event)) {
                return;
            }

            event.preventDefault();
        }

        navigateToSearchHref(href);
    }, [navigateToSearchHref]);

    const handleSearchStateChange = useCallback((nextState: BeatmapsetSearchState) => {
        if (!canUseSearchControls) {
            return;
        }

        updateSearchState({
            ...nextState,
            view: stateRef.current.view,
        });
    }, [canUseSearchControls, updateSearchState]);

    const handleViewChange = useCallback((view: BeatmapsetSearchView) => {
        if (!canUseSearchControls) {
            return;
        }

        writeStoredBeatmapsetSearchView(view);

        const nextState = setBeatmapsetSearchView(stateRef.current, view);
        stateRef.current = nextState;
        setState(nextState);

        if (new URLSearchParams(window.location.search).has("view")) {
            window.history.replaceState(null, "", getClientSearchHref(nextState));
        }
    }, [canUseSearchControls]);

    useEffect(() => {
        if (status === "loading") {
            return;
        }

        if (!canUseSearchControls) {
            const nextState = getPublicBeatmapsetSearchState(stateRef.current);
            const nextHref = getClientSearchHref(nextState);

            stateRef.current = nextState;
            setState(nextState);

            if (window.location.pathname + window.location.search !== nextHref) {
                window.history.replaceState(null, "", nextHref);
            }

            return;
        }

        const searchParams = new URLSearchParams(window.location.search);
        const urlView = searchParams.get("view");

        if (isBeatmapsetSearchView(urlView)) {
            writeStoredBeatmapsetSearchView(urlView);
            window.history.replaceState(null, "", getClientSearchHref(stateRef.current));
            return;
        }

        const storedView = readStoredBeatmapsetSearchView();

        if (storedView == null || storedView === stateRef.current.view) {
            return;
        }

        const nextState = setBeatmapsetSearchView(stateRef.current, storedView);
        stateRef.current = nextState;
        setState(nextState);
    }, [canUseSearchControls, status]);

    useEffect(() => {
        const handlePopState = () => {
            const searchParams = new URLSearchParams(window.location.search);
            const parsedState = parseBeatmapsetSearchState(searchParamsToQueryParams(searchParams));
            const urlView = searchParams.get("view");

            if (!canUseSearchControls) {
                const nextState = getPublicBeatmapsetSearchState(parsedState);

                stateRef.current = nextState;
                setState(nextState);

                return;
            }

            if (isBeatmapsetSearchView(urlView)) {
                writeStoredBeatmapsetSearchView(urlView);
            }

            const nextState = {
                ...parsedState,
                view: isBeatmapsetSearchView(urlView)
                    ? urlView
                    : readStoredBeatmapsetSearchView() ?? stateRef.current.view,
            };

            stateRef.current = nextState;
            setState(nextState);
        };

        window.addEventListener("popstate", handlePopState);
        return () => window.removeEventListener("popstate", handlePopState);
    }, [canUseSearchControls]);

    const currentSortField = state.sort.split("_")[0];
    const currentSortOrder = state.sort.split("_")[1];
    const visibleSortFields = getVisibleBeatmapsetSearchSortFields({
        query: state.query,
        status: state.status,
    });
    const heroCoverUrl = useMemo(() => getSearchHeroCoverUrl(data, osuApiOrigin), [data, osuApiOrigin]);
    const searchResetKey = useMemo(() => getClientSearchHref(state), [state]);

    return (
        <main className="flex-1 bg-osu-b6 pb-10">
            <Header
                background={(
                    <div className="absolute inset-0 bg-osu-d6">
                        {heroCoverUrl != null ? (
                            <div
                                className="absolute inset-0 bg-cover bg-center opacity-30"
                                style={{backgroundImage: `url("${heroCoverUrl}")`}}
                            />
                        ) : (
                            <Image
                                alt=""
                                className="object-cover opacity-30"
                                fill
                                sizes="100vw"
                                src="/headers/rankings.jpg"
                            />
                        )}
                        <div
                            className="absolute inset-0 bg-[linear-gradient(180deg,rgba(12,16,20,0.35),rgba(12,16,20,0.88))]"/>
                        <div className="absolute inset-0 backdrop-blur-[6px]"/>
                    </div>
                )}
                icon={(
                    <div className="flex w-10 flex-none items-center justify-center self-stretch">
                        <Music2 className="h-5 w-5 text-white"/>
                    </div>
                )}
                title={
                    <>
                        beatmap <span className="text-osu-h1">listing</span>
                    </>
                }
                bottom={(
                    <HeaderNav/>
                )}
                bottomClassName="relative bg-osu-d4/95"
                topClassName="bg-osu-d5/92 text-osu-c1"
            />

            <PageWrapper modifiers="generic-compact">
                <div className="relative grid grid-cols-[minmax(0,1fr)] gap-0">
                    {canUseSearchControls ? (
                        <>
                            <section
                                className="relative bg-osu-b4 px-4.5 py-5 text-[0.95rem] text-osu-l2 lg:px-10">
                                <BeatmapsetSearchQueryForm
                                    key={searchResetKey}
                                    onSearchStateChange={handleSearchStateChange}
                                    state={state}
                                />

                                <div className="grid gap-x-2 gap-y-6 pt-2 pr-4 xl:grid-cols-[5.5rem_minmax(18rem,1fr)_minmax(0,8rem)_minmax(0,7rem)_minmax(0,6.5rem)_minmax(0,8rem)_minmax(0,9rem)]">
                                    <TextFilterGroup
                                        defaultValue={null}
                                        label="Mode"
                                        onNavigate={handleSearchNavigation}
                                        options={BEATMAPSET_SEARCH_MODE_OPTIONS}
                                        selectedValues={state.mode}
                                        state={state}
                                        updateKey="mode"
                                    />

                                    <div className="space-y-7">
                                        <MultiTextFilterGroup
                                            fullWidthDecoration
                                            label="General"
                                            onNavigate={handleSearchNavigation}
                                            options={BEATMAPSET_SEARCH_GENERAL_OPTIONS}
                                            selectedValues={state.general}
                                            state={state}
                                            updateKey="general"
                                        />
                                        <RankFilterGroup
                                            fullWidthDecoration
                                            label="Rank Achieved"
                                            onNavigate={handleSearchNavigation}
                                            options={BEATMAPSET_SEARCH_RANK_OPTIONS}
                                            selectedValues={state.rank}
                                            state={state}
                                            updateKey="rank"
                                        />
                                    </div>

                                    <StatusFilterGroup
                                        defaultValue="leaderboard"
                                        label="Categories"
                                        onNavigate={handleSearchNavigation}
                                        options={BEATMAPSET_SEARCH_STATUS_OPTIONS}
                                        selectedValues={state.status}
                                        state={state}
                                        updateKey="status"
                                    />

                                    <TextFilterGroup
                                        defaultValue={null}
                                        label="Language"
                                        onNavigate={handleSearchNavigation}
                                        options={BEATMAPSET_SEARCH_LANGUAGE_OPTIONS}
                                        selectedValues={state.language}
                                        state={state}
                                        updateKey="language"
                                    />

                                    <TextFilterGroup
                                        defaultValue={null}
                                        label="Genre"
                                        onNavigate={handleSearchNavigation}
                                        options={BEATMAPSET_SEARCH_GENRE_OPTIONS}
                                        selectedValues={state.genre}
                                        state={state}
                                        updateKey="genre"
                                    />

                                    <MultiTextFilterGroup
                                        label="Extra"
                                        onNavigate={handleSearchNavigation}
                                        options={BEATMAPSET_SEARCH_EXTRA_OPTIONS}
                                        selectedValues={state.extra}
                                        state={state}
                                        updateKey="extra"
                                    />

                                    <div className="space-y-7">
                                        <TextFilterGroup
                                            defaultValue="any"
                                            label="Played"
                                            onNavigate={handleSearchNavigation}
                                            options={BEATMAPSET_SEARCH_PLAYED_OPTIONS}
                                            selectedValues={state.played}
                                            state={state}
                                            updateKey="played"
                                        />
                                        <ExplicitContentFilterGroup onNavigate={handleSearchNavigation} state={state}/>
                                    </div>
                                </div>

                            </section>

                            <section className="relative bg-osu-b5 px-4.5 py-2.5 text-sm lg:px-10">
                                <div className="flex flex-wrap items-center justify-between gap-3 pr-4">
                                    <div className="flex flex-wrap items-center -m-[5px]">
                                        <span className="m-[5px] text-white">Sort</span>
                                        {visibleSortFields.map((field) => {
                                            const active = currentSortField === field;
                                            const href = buildBeatmapsetSearchHref(
                                                toggleBeatmapsetSearchSort(state, field),
                                                ["view"],
                                            );

                                            return (
                                                <FilterChipLink
                                                    active={active}
                                                    direction={active && currentSortOrder === "asc" ? "asc" : "desc"}
                                                    href={href}
                                                    key={`sort-${field}`}
                                                    label={getBeatmapsetSearchSortLabel(field)}
                                                    onNavigate={handleSearchNavigation}
                                                />
                                            );
                                        })}
                                    </div>

                                    <div className="flex flex-wrap items-center -m-[5px]">
                                        {BEATMAPSET_SEARCH_VIEW_OPTIONS.map((option) => (
                                            <ViewModeLink
                                                active={state.view === option.value}
                                                icon={beatmapsetSearchViewIcons[option.value]}
                                                key={`view-${option.value}`}
                                                label={option.label}
                                                onSelect={() => handleViewChange(option.value)}
                                            />
                                        ))}
                                    </div>
                                </div>
                            </section>
                        </>
                    ) : null}

                    <section
                        className="relative overflow-visible bg-osu-b4 px-4.5 py-5 text-[0.95rem] shadow-[0_12px_28px_rgba(0,0,0,0.28)] lg:px-10">
                        <BeatmapsetSearchInfiniteResults
                            initialData={data}
                            isReloading={isSearchReloading}
                            key={`${searchResetKey}:${dataVersion}`}
                            onNavigate={handleSearchNavigation}
                            state={state}
                        />
                    </section>
                </div>
            </PageWrapper>
        </main>
    );
}

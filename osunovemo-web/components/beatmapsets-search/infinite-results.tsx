"use client";

import {useCallback, useEffect, useMemo, useRef, useState, type MouseEvent} from "react";
import Link from "next/link";
import useSWRInfinite from "swr/infinite";
import {BeatmapsetSearchCardPreview, BeatmapsetSearchResultsPlaceholder} from "@/components/beatmapsets-search/search-results-placeholder";
import {buildBeatmapsetSearchParams, type BeatmapsetSearchState} from "@/lib/beatmapset-search";
import type {BeatmapsetSearchData} from "@/lib/beatmapset-search-types";
import {cn} from "@/lib/utils";

type BeatmapsetSearchInfiniteResultsProps = {
    initialData: BeatmapsetSearchData;
    isReloading?: boolean;
    onNavigate?: (href: string, event: MouseEvent<HTMLAnchorElement>) => void;
    state: BeatmapsetSearchState;
};

function getSearchResultsLayoutClassName(view: BeatmapsetSearchState["view"]) {
    if (view === "nano") {
        return "grid grid-cols-1 gap-2.5 md:grid-cols-2 xl:grid-cols-4";
    }

    if (view === "list") {
        return "space-y-2";
    }

    if (view === "cover") {
        return "grid grid-cols-1 gap-2.5 md:grid-cols-2 xl:grid-cols-2";
    }

    if (view === "mini") {
        return "grid grid-cols-1 gap-2.5 md:grid-cols-2 xl:grid-cols-4";
    }

    if (view === "extra") {
        return "grid grid-cols-1 gap-2.5 md:grid-cols-2 xl:grid-cols-2";
    }

    return "grid grid-cols-1 gap-2.5 md:grid-cols-2 xl:grid-cols-3";
}

function mergeBeatmapsets(
    currentBeatmapsets: BeatmapsetSearchData["beatmapsets"],
    nextBeatmapsets: BeatmapsetSearchData["beatmapsets"],
) {
    const merged = new Map(currentBeatmapsets.map((beatmapset) => [beatmapset.id, beatmapset]));

    for (const beatmapset of nextBeatmapsets) {
        merged.set(beatmapset.id, beatmapset);
    }

    return [...merged.values()];
}

function getSearchPageHref(
    state: BeatmapsetSearchState,
    cursor?: NonNullable<BeatmapsetSearchData["cursor"]> | null,
) {
    const searchParams = buildBeatmapsetSearchParams(state, ["view", "page"]);
    searchParams.set("sort", state.sort);

    if (cursor != null) {
        for (const [key, value] of Object.entries(cursor)) {
            searchParams.set(`cursor[${key}]`, String(value));
        }
    }

    return `/api/beatmapsets/search?${searchParams.toString()}`;
}

async function fetchBeatmapsetSearchPage(href: string) {
    const response = await fetch(href, {cache: "no-store"});

    if (!response.ok) {
        throw new Error("Could not load more beatmapsets.");
    }

    const nextData = (await response.json()) as BeatmapsetSearchData;

    if (!nextData.backendAvailable) {
        throw new Error("Could not load more beatmapsets.");
    }

    return nextData;
}

function mergeSearchPages(initialData: BeatmapsetSearchData, pages: BeatmapsetSearchData[] | undefined) {
    return (pages ?? [initialData]).slice(1).reduce(
        (currentData, nextData) => ({
            ...currentData,
            beatmapsets: mergeBeatmapsets(currentData.beatmapsets, nextData.beatmapsets),
            cursor: nextData.cursor,
            error: currentData.error ?? nextData.error,
            hasNextPage: nextData.hasNextPage,
            recommendedDifficulty: currentData.recommendedDifficulty ?? nextData.recommendedDifficulty,
            total: nextData.total,
        }),
        pages?.[0] ?? initialData,
    );
}

export function BeatmapsetSearchInfiniteResults({
    initialData,
    isReloading = false,
    onNavigate,
    state,
}: BeatmapsetSearchInfiniteResultsProps) {
    const [loadError, setLoadError] = useState<string | null>(null);
    const sentinelRef = useRef<HTMLDivElement | null>(null);
    const fallbackPages = useMemo(() => [initialData], [initialData]);
    const getSearchPageKey = useCallback(
        (pageIndex: number, previousPageData: BeatmapsetSearchData | null) => {
            if (pageIndex === 0) {
                return getSearchPageHref(state);
            }

            if (
                previousPageData == null ||
                !previousPageData.backendAvailable ||
                !previousPageData.hasNextPage ||
                previousPageData.cursor == null
            ) {
                return null;
            }

            return getSearchPageHref(state, previousPageData.cursor);
        },
        [state],
    );
    const {
        data: pages,
        error,
        isValidating,
        setSize,
        size,
    } = useSWRInfinite<BeatmapsetSearchData>(
        getSearchPageKey,
        fetchBeatmapsetSearchPage,
        {
            fallbackData: fallbackPages,
            revalidateFirstPage: false,
            revalidateOnMount: false,
        },
    );
    const data = useMemo(() => mergeSearchPages(initialData, pages), [initialData, pages]);
    const isLoadingMore = isValidating && size > 1;
    const loadErrorMessage = loadError ?? (error == null ? null : "Could not load more beatmapsets.");

    const loadNextPage = useCallback(async () => {
        if (!data.backendAvailable || !data.hasNextPage || data.cursor == null || isLoadingMore) {
            return;
        }

        setLoadError(null);

        try {
            await setSize(size + 1);
        } catch {
            setLoadError("Could not load more beatmapsets.");
        }
    }, [data.backendAvailable, data.cursor, data.hasNextPage, isLoadingMore, setSize, size]);

    const retryLoadNextPage = useCallback(async () => {
        setLoadError(null);

        try {
            await setSize(size);
        } catch {
            setLoadError("Could not load more beatmapsets.");
        }
    }, [setSize, size]);

    const layoutClassName = getSearchResultsLayoutClassName(state.view);

    useEffect(() => {
        const sentinel = sentinelRef.current;
        if (sentinel == null) {
            return;
        }

        const observer = new IntersectionObserver(
            ([entry]) => {
                if (!entry?.isIntersecting || loadErrorMessage != null) {
                    return;
                }

                void loadNextPage();
            },
            {rootMargin: "320px 0px"},
        );

        observer.observe(sentinel);
        return () => observer.disconnect();
    }, [loadErrorMessage, loadNextPage]);

    return (
        <div className="space-y-4">
            <div className="flex flex-wrap items-center justify-between gap-3">
                <div className="flex flex-wrap items-center gap-2 text-sm text-osu-l2">
                    {!data.backendAvailable ? <span className="font-semibold text-white">Search unavailable</span> : null}
                    {data.recommendedDifficulty != null && state.general.includes("recommended") ? (
                        <span className="rounded-full bg-osu-b5 px-2.5 py-1 text-xs text-osu-c1">
                            Recommended {data.recommendedDifficulty}
                        </span>
                    ) : null}
                </div>
            </div>

            {isReloading ? (
                <BeatmapsetSearchResultsPlaceholder view={state.view}/>
            ) : (
                <>
                    {data.error != null ? (
                        <div
                            className={cn(
                                "rounded-xl border px-4 py-3 text-sm",
                                data.backendAvailable
                                    ? "border-amber-300/20 bg-amber-300/10 text-amber-100"
                                    : "border-red-300/20 bg-red-300/10 text-red-100",
                            )}
                        >
                            {data.error}
                        </div>
                    ) : null}

                    {!data.backendAvailable ? (
                        <div className="rounded-xl bg-osu-b3 px-5 py-6 text-sm text-osu-l2">
                            Beatmapset search could not be loaded from the configured API.
                        </div>
                    ) : data.beatmapsets.length > 0 ? (
                        <>
                            <div className={layoutClassName}>
                                {data.beatmapsets.map((beatmapset) => (
                                    <BeatmapsetSearchCardPreview beatmapset={beatmapset} key={beatmapset.id} view={state.view}/>
                                ))}
                            </div>

                            {isLoadingMore ? (
                                <div className="pt-2">
                                    <BeatmapsetSearchResultsPlaceholder view={state.view}/>
                                </div>
                            ) : null}

                            {loadErrorMessage != null ? (
                                <div className="flex flex-wrap items-center gap-3 pt-1 text-sm">
                                    <span className="text-rose-300">{loadErrorMessage}</span>
                                    <button
                                        className="inline-flex items-center justify-center rounded-full bg-osu-b5 px-3 py-1 text-xs font-semibold uppercase text-osu-l2 transition-colors hover:bg-osu-b3 hover:text-white"
                                        onClick={() => {
                                            setLoadError(null);
                                            void retryLoadNextPage();
                                        }}
                                        type="button"
                                    >
                                        Retry
                                    </button>
                                </div>
                            ) : null}

                            {data.hasNextPage ? <div aria-hidden className="h-px w-full" ref={sentinelRef}/> : null}
                        </>
                    ) : (
                        <div className="rounded-xl bg-osu-b3 px-5 py-6 text-sm text-osu-l2">
                            <p className="text-white">
                                {state.page > 1
                                    ? "This page has no beatmapsets."
                                    : "No beatmapsets matched this search."}
                            </p>
                            <p className="pt-2">
                                <Link
                                    className="text-osu-h1 hover:text-white"
                                    href="/beatmapsets"
                                    onClick={(event) => onNavigate?.("/beatmapsets", event)}
                                    prefetch={false}
                                >
                                    Reset filters
                                </Link>
                            </p>
                        </div>
                    )}
                </>
            )}
        </div>
    );
}

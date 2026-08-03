"use client";

import {useCallback, useMemo, useState} from "react";
import useSWRInfinite from "swr/infinite";
import {ProfileScoreCard} from "@/components/profile/profile-score-card";
import {ShowMoreLink} from "@/components/show-more-link";
import type {Score, ScoreSection} from "@/lib/profile";
import type {Ruleset} from "@/lib/rankings";
import {getScoreHighlightCutoff, isScoreHighlighted} from "@/lib/score-highlight";

type ProfileScoreListProps = {
    canLoadMore?: boolean;
    highlightPeriodDays?: number | null;
    initialScores: Score[];
    mode: Ruleset;
    section: ScoreSection;
    showPpWeight?: boolean;
    totalCount?: number;
    userId: number;
};
type ProfileScoresPayload = {
    scores?: Score[];
};

const profileScoreLoadMoreBatchSize = 50;

function mergeScores(currentScores: Score[], nextScores: Score[]) {
    const merged = new Map(currentScores.map((score) => [score.id, score]));

    for (const score of nextScores) {
        merged.set(score.id, score);
    }

    return [...merged.values()];
}

function getProfileScoresHref(userId: number, section: ScoreSection, mode: Ruleset, offset: number) {
    const searchParams = new URLSearchParams({
        limit: String(profileScoreLoadMoreBatchSize),
        mode,
        offset: String(offset),
    });

    return `/api/users/${userId}/scores/${section}?${searchParams.toString()}`;
}

async function fetchProfileScoresPage(href: string) {
    const response = await fetch(href, {cache: "no-store"});

    if (!response.ok) {
        throw new Error("Failed to load scores.");
    }

    return await response.json() as ProfileScoresPayload;
}

function getProfileScoresOffset(pageIndex: number, initialScoreCount: number) {
    return initialScoreCount + (pageIndex * profileScoreLoadMoreBatchSize);
}

export function ProfileScoreList({
    canLoadMore = false,
    highlightPeriodDays = null,
    initialScores,
    mode,
    section,
    showPpWeight = false,
    totalCount,
    userId,
}: ProfileScoreListProps) {
    const [now] = useState(() => Date.now());
    const [loadError, setLoadError] = useState<string | null>(null);
    const initialScoreCount = initialScores.length;
    const getProfileScoresPageKey = useCallback(
        (pageIndex: number, previousPageData: ProfileScoresPayload | null) => {
            if (pageIndex > 0 && (previousPageData?.scores?.length ?? 0) < profileScoreLoadMoreBatchSize) {
                return null;
            }

            const offset = getProfileScoresOffset(pageIndex, initialScoreCount);

            if (totalCount != null && offset >= totalCount) {
                return null;
            }

            return getProfileScoresHref(userId, section, mode, offset);
        },
        [initialScoreCount, mode, section, totalCount, userId],
    );
    const {
        data: scorePages,
        error,
        isValidating,
        setSize,
        size,
    } = useSWRInfinite<ProfileScoresPayload>(
        getProfileScoresPageKey,
        fetchProfileScoresPage,
        {
            initialSize: 0,
            revalidateFirstPage: false,
            revalidateOnMount: false,
        },
    );
    const loadedPages = useMemo(() => scorePages ?? [], [scorePages]);
    const loadedScoreCount = useMemo(
        () => loadedPages.reduce((count, page) => count + (page.scores?.length ?? 0), initialScoreCount),
        [initialScoreCount, loadedPages],
    );
    const lastPageSize = loadedPages.at(-1)?.scores?.length ?? initialScoreCount;
    const scores = useMemo(
        () => loadedPages.reduce(
            (currentScores, page) => mergeScores(currentScores, page.scores ?? []),
            initialScores,
        ),
        [initialScores, loadedPages],
    );
    const isLoading = isValidating && size > 0;
    const loadErrorMessage = loadError ?? (error == null ? null : "Could not load more scores.");
    const highlightCutoff = useMemo(() => {
        return getScoreHighlightCutoff(highlightPeriodDays, now);
    }, [highlightPeriodDays, now]);
    const highlightedCount = useMemo(() => {
        if (highlightCutoff == null) {
            return scores.length;
        }

        return scores.reduce(
            (count, score) => (isScoreHighlighted(score.ended_at, highlightCutoff) ? count + 1 : count),
            0,
        );
    }, [highlightCutoff, scores]);

    const hasMore = useMemo(() => {
        if (!canLoadMore) {
            return false;
        }

        if (totalCount == null) {
            return lastPageSize >= profileScoreLoadMoreBatchSize || loadedScoreCount === initialScoreCount;
        }

        return loadedScoreCount < totalCount;
    }, [canLoadMore, initialScoreCount, lastPageSize, loadedScoreCount, totalCount]);

    async function handleShowMore() {
        if (isLoading || !hasMore) {
            return;
        }

        setLoadError(null);

        try {
            await setSize(size + 1);
        } catch {
            setLoadError("Could not load more scores.");
        }
    }

    async function handleRetryShowMore() {
        setLoadError(null);

        try {
            await setSize(Math.max(size, 1));
        } catch {
            setLoadError("Could not load more scores.");
        }
    }

    return (
        <div>
            <div className="grid gap-1">
                {scores.map((score, index) => (
                    <ProfileScoreCard
                        highlightActive={highlightCutoff != null}
                        highlighted={isScoreHighlighted(score.ended_at, highlightCutoff)}
                        key={`${section}-${score.id}`}
                        position={index + 1}
                        score={score}
                        showPpWeight={showPpWeight}
                    />
                ))}
            </div>

            <ShowMoreLink
                className="mt-3"
                hasMore={hasMore}
                loading={isLoading}
                onClick={loadErrorMessage == null ? handleShowMore : handleRetryShowMore}
            />

            {loadErrorMessage ? <p className="mt-2 text-center text-xs text-rose-300">{loadErrorMessage}</p> : null}
            {highlightCutoff != null && highlightedCount === 0 ? (
                <p className="mt-2 text-center text-xs text-osu-f1">No scores landed in the selected period.</p>
            ) : null}
        </div>
    );
}

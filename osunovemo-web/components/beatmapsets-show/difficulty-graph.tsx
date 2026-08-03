"use client";

import { useMemo } from "react";
import useSWR from "swr";
import type { BeatmapDifficultyGraphResponse } from "@/lib/beatmapset-types";
import type { Ruleset } from "@/lib/rankings";
import { cn } from "@/lib/utils";

type BeatmapDifficultyGraphProps = {
    beatmapId: number;
    initialGraph?: BeatmapDifficultyGraphResponse | null;
    initialGraphBeatmapId?: number | null;
    initialGraphMode?: Ruleset | null;
    mode: Ruleset;
};

const fallbackGraph: BeatmapDifficultyGraphResponse = {
    rawSegmentCount: 50,
    segments: [
        {
            color: "rgb(102, 204, 255)",
            height: 1,
            span: 50,
        },
    ],
};

function buildGraphKey(beatmapId: number | null | undefined, mode: Ruleset | null | undefined) {
    if (beatmapId == null || mode == null) {
        return null;
    }

    return `${beatmapId}:${mode}`;
}

function getGraphHref(beatmapId: number, mode: Ruleset) {
    const searchParams = new URLSearchParams();
    searchParams.set("mode", mode);

    return `/api/beatmaps/${beatmapId}/difficulty-graph?${searchParams.toString()}`;
}

export function BeatmapDifficultyGraph({
    beatmapId,
    initialGraph = null,
    initialGraphBeatmapId = null,
    initialGraphMode = null,
    mode,
}: BeatmapDifficultyGraphProps) {
    const currentKey = useMemo(() => buildGraphKey(beatmapId, mode) ?? `${beatmapId}:${mode}`, [beatmapId, mode]);
    const initialGraphKey = useMemo(
        () => buildGraphKey(initialGraphBeatmapId, initialGraphMode),
        [initialGraphBeatmapId, initialGraphMode],
    );
    const graphHref = useMemo(() => getGraphHref(beatmapId, mode), [beatmapId, mode]);
    const {
        data: graph = null,
        error,
        isLoading,
    } = useSWR<BeatmapDifficultyGraphResponse>(graphHref, {
        fallbackData: initialGraphKey === currentKey ? initialGraph ?? undefined : undefined,
        keepPreviousData: true,
        revalidateOnMount: initialGraphKey !== currentKey,
    });
    const loading = isLoading && graph == null;
    const displayGraph = graph ?? fallbackGraph;
    const errorMessage = error instanceof Error ? error.message : null;

    return (
        <div aria-busy={loading} className="mt-3 overflow-hidden rounded-md bg-osu-b4">
            <div
                className="grid h-7 items-end overflow-hidden rounded-md"
                style={{
                    gridTemplateColumns: `repeat(${displayGraph.rawSegmentCount}, minmax(0, 1fr))`,
                }}
                title={errorMessage ?? "Beatmap difficulty graph"}
            >
                {displayGraph.segments.map((segment, index) => (
                    <div
                        className={cn("rounded-[1px] transition-[height,opacity,background-color] duration-150", loading && graph == null && "animate-pulse")}
                        key={`${currentKey}-${segment.span}-${index}`}
                        style={{
                            backgroundColor: segment.color,
                            gridColumn: `span ${segment.span} / span ${segment.span}`,
                            height: `${segment.height * 100}%`,
                            opacity: loading && graph == null ? 0.65 : 1,
                        }}
                    />
                ))}
            </div>
        </div>
    );
}

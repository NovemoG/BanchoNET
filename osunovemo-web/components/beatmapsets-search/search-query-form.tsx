"use client";

import {useCallback, useEffect, useState} from "react";
import {Search} from "lucide-react";
import {
    buildBeatmapsetSearchHref,
    normalizeBeatmapsetSearchState,
    type BeatmapsetSearchState,
} from "@/lib/beatmapset-search";

type BeatmapsetSearchQueryFormProps = {
    onSearchStateChange: (state: BeatmapsetSearchState) => void;
    state: BeatmapsetSearchState;
};

export function BeatmapsetSearchQueryForm({onSearchStateChange, state}: BeatmapsetSearchQueryFormProps) {
    const [query, setQuery] = useState(state.query);
    const submitQuery = useCallback((nextQuery: string) => {
        const trimmedQuery = nextQuery.trim();
        const sort = trimmedQuery === state.query ? state.sort : undefined;
        const nextState = normalizeBeatmapsetSearchState({
            ...state,
            page: 1,
            query: nextQuery,
            sort,
        });

        if (buildBeatmapsetSearchHref(nextState) === buildBeatmapsetSearchHref(state)) {
            return;
        }

        onSearchStateChange(nextState);
    }, [onSearchStateChange, state]);

    useEffect(() => {
        const timeoutId = window.setTimeout(() => submitQuery(query), 500);

        return () => window.clearTimeout(timeoutId);
    }, [query, submitQuery]);

    return (
        <form
            onSubmit={(event) => {
                event.preventDefault();
                submitQuery(query);
            }}
        >
            <div className="relative text-[1.2rem] text-osu-f1">
                <label
                    className="flex rounded-xl border border-osu-b4 bg-osu-b5 transition-colors focus-within:border-osu-h1">
                    <span className="sr-only">Search beatmaps</span>
                    <input
                        className="min-w-0 flex-1 bg-transparent px-3 py-2.5 text-[1.2rem] text-white caret-osu-h1 outline-none placeholder:text-osu-f1"
                        name="q"
                        onChange={(event) => setQuery(event.target.value)}
                        placeholder="type in keywords..."
                        type="search"
                        value={query}
                    />
                    <button
                        aria-label="Search beatmaps"
                        className="flex w-13 shrink-0 items-center justify-center text-osu-f1"
                        type="submit"
                    >
                        <Search className="h-5 w-5"/>
                    </button>
                </label>
            </div>
        </form>
    );
}

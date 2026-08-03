import "server-only";

import {cache} from "react";
import {normalizeBeatmapsetSearchState} from "@/lib/beatmapset-search";
import {fetchBeatmapsetSearch} from "@/lib/beatmapset-search-server";
import {fetchOsuApi} from "@/lib/osu-api";
import type {BeatmapsetSearchBeatmapset} from "@/lib/beatmapset-search-types";

export type LandingGraphDatum = {
    x: number;
    y: number;
};

export type HomeStats = {
    currentGames: number;
    currentOnline: number;
    graphData: LandingGraphDatum[];
    hasLiveData: boolean;
    totalUsers: number;
};

export type HomeNewsPost = {
    author: string | null;
    id: number | string;
    imageUrl: string | null;
    preview: string;
    publishedAt: string;
    title: string;
    url: string;
};

export type HomeBeatmapsets = {
    newBeatmapsets: BeatmapsetSearchBeatmapset[];
    popularBeatmapsets: BeatmapsetSearchBeatmapset[];
};

;

;

const landingVideoUrl = process.env.OSU_LANDING_VIDEO_URL ?? "https://assets.ppy.sh/media/landing.mp4";

export {landingVideoUrl};

type ServerStatsResponse = {
    games?: number;
    online?: number;
    registered?: number;
};

type ServerOnlineHistoryResponse = {
    online?: Array<{ at?: string; count?: number }>;
};

/**
 * Our own counters, replacing a scrape of the osu-web landing page. The graph is the last 24h of
 * online samples, taken every 10 minutes by a background job and kept in redis.
 */
async function fetchServerStats(): Promise<HomeStats> {
    const [stats, history] = await Promise.all([
        fetchOsuApi<ServerStatsResponse>("stats"),
        fetchOsuApi<ServerOnlineHistoryResponse>("stats/history"),
    ]);

    const graphData = (history.online ?? [])
        .map((sample) => {
            const at = sample.at == null ? Number.NaN : Date.parse(sample.at);

            return Number.isFinite(at) && typeof sample.count === "number"
                ? {x: at, y: sample.count}
                : null;
        })
        .filter((item): item is LandingGraphDatum => item != null);

    return {
        currentGames: stats.games ?? 0,
        currentOnline: stats.online ?? 0,
        graphData,
        hasLiveData: true,
        totalUsers: stats.registered ?? 0,
    };
}

export const fetchHomeStats = cache(async (): Promise<HomeStats> => {
    try {
        return await fetchServerStats();
    } catch {
        // The homepage should still render if the API is briefly unreachable.
    }

    // hasLiveData false makes consumers show an empty state rather than invented numbers.
    return {
        currentGames: 0,
        currentOnline: 0,
        graphData: [],
        hasLiveData: false,
        totalUsers: 0,
    };
});

/**
 * No news system exists, so this is deliberately empty rather than pulling posts from osu!'s news
 * feed and linking readers off site. Consumers already hide the section when the list is empty.
 */
export const fetchHomeNews = cache(async (): Promise<HomeNewsPost[]> => []);

export const fetchHomeBeatmapsets = cache(async (): Promise<HomeBeatmapsets> => {
    const [newBeatmapsets, popularBeatmapsets] = await Promise.all([
        fetchBeatmapsetSearch(
            normalizeBeatmapsetSearchState({
                query: "",
                sort: "ranked_desc",
                status: ["ranked"],
                view: "mini",
            }),
        ),
        fetchBeatmapsetSearch(
            normalizeBeatmapsetSearchState({
                query: "",
                sort: "favourites_desc",
                status: ["ranked"],
                view: "mini",
            }),
        ),
    ]);

    return {
        newBeatmapsets: newBeatmapsets.beatmapsets.slice(0, 4),
        popularBeatmapsets: popularBeatmapsets.beatmapsets.slice(0, 4),
    };
});

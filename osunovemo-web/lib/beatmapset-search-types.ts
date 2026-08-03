import type {Beatmapset} from "@/lib/profile";

export const BEATMAPSET_SEARCH_ENDPOINT =
    "https://osu.novemo.dev/api/v2/beatmapsets/search";

export type BeatmapsetSearchBeatmapset = Beatmapset & {
    bpm?: number | null;
    genre_id?: number;
    has_favourited?: boolean;
    language_id?: number;
    last_updated?: string | null;
    nsfw?: boolean;
    storyboard?: boolean;
    video?: boolean;
};

export type BeatmapsetSearchCursor = Record<string, number | string>;

export type BeatmapsetSearchData = {
    backendAvailable: boolean;
    beatmapsets: BeatmapsetSearchBeatmapset[];
    cursor: BeatmapsetSearchCursor | null;
    endpoint: string;
    error: string | null;
    hasNextPage: boolean;
    recommendedDifficulty: string | null;
    total: number;
};

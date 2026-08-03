import "server-only";

import {
  buildBeatmapsetSearchParams,
  type BeatmapsetSearchState,
  getEffectiveStatuses,
} from "@/lib/beatmapset-search";
import {
  BEATMAPSET_SEARCH_ENDPOINT,
  type BeatmapsetSearchBeatmapset,
  type BeatmapsetSearchCursor,
  type BeatmapsetSearchData,
} from "@/lib/beatmapset-search-types";
import { fetchOsuApi, OsuApiError } from "@/lib/osu-api";

type FetchBeatmapsetSearchOptions = {
  cursor?: BeatmapsetSearchCursor | null;
};

type RawBeatmapsetSearchResponse = {
  beatmapsets: BeatmapsetSearchBeatmapset[];
  cursor?: BeatmapsetSearchCursor | null;
  error: string | null;
  recommended_difficulty: number | string | null;
  search: {
    sort: string | null;
  };
  total: number;
};

function buildSearchParams(state: BeatmapsetSearchState, cursor?: BeatmapsetSearchCursor | null) {
  const params = Object.fromEntries(
    buildBeatmapsetSearchParams(state, ["view", "page"]).entries(),
  );

  params.sort = state.sort;

  // An empty status means "the default", which the UI highlights as Leaderboard. The URL omits it
  // to stay clean, but the API has its own default, so it has to be sent explicitly or the listing
  // silently returns everything while the sidebar claims Leaderboard is selected.
  params.s = getEffectiveStatuses(state.status).join(".");

  if (cursor != null) {
    for (const [key, value] of Object.entries(cursor)) {
      params[`cursor[${key}]`] = String(value);
    }
  }

  return params;
}

function getSearchErrorMessage(error: unknown) {
  if (error instanceof OsuApiError) {
    if (error.status === 401) {
      return "Beatmapset search requires a signed-in user session.";
    }

    return `Beatmapset search failed with status ${error.status}.`;
  }

  if (error instanceof Error) {
    return error.message;
  }

  return "Beatmapset search failed.";
}

async function fetchBeatmapsetSearchPage(
  state: BeatmapsetSearchState,
  cursor?: BeatmapsetSearchCursor | null,
) {
  return fetchOsuApi<RawBeatmapsetSearchResponse>(
    "beatmapsets/search",
    buildSearchParams(state, cursor),
  );
}

export async function fetchBeatmapsetSearch(
  state: BeatmapsetSearchState,
  options: FetchBeatmapsetSearchOptions = {},
): Promise<BeatmapsetSearchData> {
  try {
    const currentPage = await fetchBeatmapsetSearchPage(state, options.cursor);

    return {
      backendAvailable: true,
      beatmapsets: currentPage.beatmapsets,
      cursor: currentPage.cursor ?? null,
      endpoint: BEATMAPSET_SEARCH_ENDPOINT,
      error: currentPage.error,
      hasNextPage: currentPage.cursor != null,
      recommendedDifficulty:
        currentPage.recommended_difficulty == null
          ? null
          : String(currentPage.recommended_difficulty),
      total: currentPage.total,
    };
  } catch (error) {
    return {
      backendAvailable: false,
      beatmapsets: [],
      cursor: null,
      endpoint: BEATMAPSET_SEARCH_ENDPOINT,
      error: getSearchErrorMessage(error),
      hasNextPage: false,
      recommendedDifficulty: null,
      total: 0,
    };
  }
}

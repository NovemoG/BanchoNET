import "server-only";

import { cache } from "react";
import {
  type RankingSearchState,
  type RankingType,
  type Ruleset,
} from "@/lib/rankings";
import {
  type Country,
  type CountryRankingEntry,
  type GlobalRankingEntry,
  osuApiBaseUrl,
  resolveAssetUrl,
  type TeamRankingEntry,
} from "@/lib/osu-api-common";
import { getAccessToken } from "@/lib/auth/session";
import { getClientCredentialsToken } from "@/lib/auth/osu-auth-client";

export class OsuApiError extends Error {
  constructor(
    message: string,
    readonly status: number,
  ) {
    super(message);
    this.name = "OsuApiError";
  }
}

/**
 * - `none`   — no token. api/v2 is [Authorize]d throughout, so this only suits non api/v2 targets.
 * - `user`   — requires a signed in user; throws when there is none. For /me and anything that
 *              reads or writes the caller's own data.
 * - `client` — app token via the client credentials grant, for public reads.
 * - `user-or-client` — prefer the user's token so personalised fields come back, fall back to the
 *              app token so the page still renders for a logged out visitor. This is the default:
 *              every public page used to throw a 401 for anonymous visitors otherwise.
 */
type OsuApiAuthMode = "none" | "user" | "client" | "user-or-client";

type OsuApiFetchOptions = {
  auth?: OsuApiAuthMode;
  /**
   * Seconds to cache the response for. Omitted means no-store, which is right for anything that
   * changes per request; set it for slow-moving reference data that would otherwise be refetched
   * on every navigation.
   */
  revalidate?: number;
};

async function getAuthorizationHeader(authMode: OsuApiAuthMode) {
  if (authMode === "none") {
    return null;
  }

  if (authMode === "client") {
    return `Bearer ${await getClientCredentialsToken()}`;
  }

  const accessToken = await getAccessToken();

  if (accessToken != null) {
    return `Bearer ${accessToken}`;
  }

  if (authMode === "user-or-client") {
    return `Bearer ${await getClientCredentialsToken()}`;
  }

  throw new OsuApiError("User session is required for this osu API request.", 401);
}

export { resolveAssetUrl };
export type {
  CountryRankingEntry,
  GlobalRankingEntry,
  TeamRankingEntry,
  UserSummary,
} from "@/lib/osu-api-common";

type OsuApiSearchParamValue = number | string | Array<number | string>;

type RankingResponse<T> = {
  ranking: T[];
  total: number;
};

type RankingTypeMap = {
  country: CountryRankingEntry;
  global: GlobalRankingEntry;
  team: TeamRankingEntry;
};

function getEndpointType(state: RankingSearchState) {
  if (state.type === "global") {
    return state.sort;
  }

  return state.type;
}

function buildOsuApiUrl(
  path: string,
  searchParams?: Record<string, OsuApiSearchParamValue>,
) {
  const url = new URL(path, `${osuApiBaseUrl}/`);

  if (searchParams != null) {
    for (const [key, value] of Object.entries(searchParams)) {
      if (Array.isArray(value)) {
        for (const item of value) {
          url.searchParams.append(key, String(item));
        }

        continue;
      }

      url.searchParams.set(key, String(value));
    }
  }

  return url;
}

async function fetchOsuApiResponse(
  path: string,
  searchParams?: Record<string, OsuApiSearchParamValue>,
  accept = "application/json",
  authMode: OsuApiAuthMode = "user-or-client",
  revalidate?: number,
) {
  const url = buildOsuApiUrl(path, searchParams);
  const authorizationHeader = await getAuthorizationHeader(authMode);
  const headers: Record<string, string> = {
    Accept: accept,
  };

  if (authorizationHeader != null) {
    headers.Authorization = authorizationHeader;
  }

  const response = await fetch(url, {
    ...(revalidate == null ? { cache: "no-store" as const } : { next: { revalidate } }),
    headers,
  });

  if (!response.ok) {
    const body = await response.text();
    throw new OsuApiError(
      `osu API request failed with ${response.status} ${response.statusText}: ${body.slice(0, 280)}`,
      response.status,
    );
  }

  return response;
}

export async function fetchOsuApi<T>(
  path: string,
  searchParams?: Record<string, OsuApiSearchParamValue>,
  options: OsuApiFetchOptions = {},
): Promise<T> {
  const response = await fetchOsuApiResponse(
    path,
    searchParams,
    "application/json",
    options.auth,
    options.revalidate,
  );
  return (await response.json()) as T;
}

export async function fetchOsuApiText(
  path: string,
  searchParams?: Record<string, OsuApiSearchParamValue>,
  options: OsuApiFetchOptions = {},
): Promise<string> {
  const response = await fetchOsuApiResponse(path, searchParams, "text/plain", options.auth);
  return response.text();
}

const fetchRankingsByState = cache(
  async (state: RankingSearchState): Promise<RankingResponse<RankingTypeMap[RankingType]>> => {
    const endpointType = getEndpointType(state);
    const searchParams: Record<string, string | number> = { page: state.page };

    if (state.type === "global") {
      if (state.country != null) {
        searchParams.country = state.country;
      }

      if (state.filter === "friends") {
        searchParams.filter = state.filter;
      }

      if (state.variant != null && state.variant !== "all") {
        searchParams.variant = state.variant;
      }
    }

    return fetchOsuApi<RankingResponse<RankingTypeMap[RankingType]>>(
      `rankings/${state.mode}/${endpointType}`,
      searchParams,
    );
  },
);

export async function fetchRankings(state: RankingSearchState) {
  return fetchRankingsByState(state);
}

/**
 * One small query rather than paging through the entire country ranking, which was several
 * aggregate requests on every performance/score page load just to fill a dropdown.
 */
export const fetchCountryOptions = cache(async (mode: Ruleset) => {
  // The country list changes about as often as a player from a new country gets ranked, so it is
  // cached rather than refetched on every rankings navigation (including each country change).
  const response = await fetchOsuApi<{ countries?: Country[] }>(
    `rankings/${mode}/countries`,
    undefined,
    { revalidate: 3600 },
  );

  return (response.countries ?? [])
    .map((country) => ({ label: country.name, value: country.code }))
    .sort((left, right) => left.label.localeCompare(right.label));
});

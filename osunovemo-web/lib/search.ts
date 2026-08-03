import { fetchOsuApi, type UserSummary } from "@/lib/osu-api";

type UserSearchResponse = {
  user?: {
    data?: UserSummary[];
    total?: number;
  };
};

export type UserSearchResult = {
  total: number;
  users: UserSummary[];
};

/**
 * Our own user search, replacing the link out to osu!'s. Backed by
 * GET /api/v2/search?mode=user, which is [AllowClientCredentials] so it works signed out too.
 */
export async function searchUsers(query: string): Promise<UserSearchResult> {
  const trimmed = query.trim();

  if (trimmed.length === 0) {
    return { total: 0, users: [] };
  }

  try {
    const response = await fetchOsuApi<UserSearchResponse>("search", {
      mode: "user",
      query: trimmed,
    });

    const users = response.user?.data ?? [];

    return { total: response.user?.total ?? users.length, users };
  } catch {
    // A failing search should render as "no results", not take the page down.
    return { total: 0, users: [] };
  }
}

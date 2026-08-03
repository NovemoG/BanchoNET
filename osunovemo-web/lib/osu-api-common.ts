export const osuApiBaseUrl = process.env.OSU_API_BASE_URL ?? "https://osu.novemo.dev/api/v2";

export function getOsuApiOrigin() {
  return new URL(osuApiBaseUrl).origin;
}

/**
 * The public API base, which is what the browser must use. Distinct from {@link osuApiBaseUrl}:
 * that one is resolved server side and points straight at the container over the docker network,
 * so it is not reachable from a browser.
 */
export const publicOsuApiBaseUrl =
  process.env.NEXT_PUBLIC_OSU_API_BASE_URL ?? "https://osu.novemo.dev/api/v2";

/**
 * Bare domain, derived by dropping the leading subdomain from the public API host. Used to build
 * URLs on sibling subdomains and for the stable client's -devserver argument.
 */
export function getPublicDomain() {
  const host = new URL(publicOsuApiBaseUrl).hostname;
  const separator = host.indexOf(".");

  return separator < 0 ? host : host.slice(separator + 1);
}

export function resolveAssetUrl(url: string) {
  if (/^https?:\/\//i.test(url)) {
    return url;
  }

  return new URL(url, `${getOsuApiOrigin()}/`).toString();
}

export type Country = {
  code: string;
  name: string;
};

export type TeamSummary = {
  flag_url: string;
  id: number;
  name: string;
  short_name: string;
};

export type UserSummary = {
  avatar_url: string;
  country?: Country | null;
  country_code: string;
  id: number;
  is_active: boolean;
  is_supporter: boolean;
  profile_colour: string | null;
  team?: TeamSummary | null;
  username: string;
};

export type GradeCounts = {
  a: number;
  s: number;
  sh: number;
  ss: number;
  ssh: number;
};

export type GlobalRankingEntry = {
  accuracy: number;
  country_rank?: number | null;
  global_rank?: number | null;
  grade_counts: GradeCounts;
  play_count: number;
  pp: number;
  rank_change_since_30_days?: number | null;
  ranked_score: number;
  user: UserSummary;
};

export type CountryRankingEntry = {
  active_users: number;
  code: string;
  country: Country;
  performance: number;
  play_count: number;
  ranked_score: number;
};

export type TeamRankingEntry = {
  member_count: number;
  performance: number;
  play_count: number;
  rank?: number;
  ranked_score: number;
  team: TeamSummary;
};

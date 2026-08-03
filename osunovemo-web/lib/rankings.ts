export const PAGE_SIZE = 50;
export const DEFAULT_MODE = "osu";
export const DEFAULT_TYPE = "global";
export const DEFAULT_SORT = "performance";
export const DEFAULT_FILTER = "all";

export const rankingModes = ["osu", "taiko", "fruits", "mania"] as const;
export const rankingTypes = ["global", "country", "team"] as const;
export const globalSortOptions = ["performance", "score", "accuracy"] as const;
export const teamSortOptions = ["performance", "score"] as const;
export const countrySortOptions = [DEFAULT_SORT] as const;
export const userFilters = ["all", "friends"] as const;
export const maniaVariantOptions = ["all", "4k", "7k"] as const;

export type Ruleset = (typeof rankingModes)[number];
export type RankingType = (typeof rankingTypes)[number];
export type GlobalRankingSort = (typeof globalSortOptions)[number];
export type TeamRankingSort = (typeof teamSortOptions)[number];
export type CountryRankingSort = (typeof countrySortOptions)[number];
export type RankingSort = GlobalRankingSort;
export type UserFilter = (typeof userFilters)[number];
export type Variant = (typeof maniaVariantOptions)[number] | null;

export type CountryOption = {
  label: string;
  value: string;
};

export type RankingSearchState = {
  country: string | null;
  filter: UserFilter;
  mode: Ruleset;
  page: number;
  sort: RankingSort;
  type: RankingType;
  variant: Variant;
};

const modeLabels: Record<Ruleset, string> = {
  fruits: "fruits",
  mania: "mania",
  osu: "osu!",
  taiko: "taiko",
};

const rankingTypeLabels: Record<RankingType, string> = {
  country: "country",
  global: "global",
  team: "team",
};

function getString(value: string | string[] | undefined) {
  if (Array.isArray(value)) {
    return value[0];
  }

  return value;
}

function isRuleset(value: string | undefined): value is Ruleset {
  return rankingModes.includes(value as Ruleset);
}

function isRankingType(value: string | undefined): value is RankingType {
  return rankingTypes.includes(value as RankingType);
}

function isRankingSort(value: string | undefined): value is RankingSort {
  return globalSortOptions.includes(value as RankingSort);
}

function isUserFilter(value: string | undefined): value is UserFilter {
  return userFilters.includes(value as UserFilter);
}

function isVariant(value: string | undefined) {
  return value == null || maniaVariantOptions.includes(value as (typeof maniaVariantOptions)[number]);
}

export function getSortOptions(type: "global"): readonly GlobalRankingSort[];
export function getSortOptions(type: "country"): readonly CountryRankingSort[];
export function getSortOptions(type: "team"): readonly TeamRankingSort[];
export function getSortOptions(type: RankingType): readonly RankingSort[];
export function getSortOptions(type: RankingType): readonly RankingSort[] {
  switch (type) {
    case "country":
      return countrySortOptions;
    case "team":
      return teamSortOptions;
    default:
      return globalSortOptions;
  }
}

export function normalizeSortForType(type: "global", sort: RankingSort): GlobalRankingSort;
export function normalizeSortForType(type: "country", sort: RankingSort): CountryRankingSort;
export function normalizeSortForType(type: "team", sort: RankingSort): TeamRankingSort;
export function normalizeSortForType(type: RankingType, sort: RankingSort): RankingSort;
export function normalizeSortForType(type: RankingType, sort: RankingSort): RankingSort {
  const sortOptions = getSortOptions(type);

  return sortOptions.includes(sort) ? sort : sortOptions[0];
}

function normalizeRankingState(state: RankingSearchState): RankingSearchState {
  const normalizedType = state.type;
  const normalizedMode = state.mode;
  const normalizedSort = normalizeSortForType(normalizedType, state.sort);
  const normalizedFilter =
    normalizedType === "global" && isUserFilter(state.filter)
      ? state.filter
      : DEFAULT_FILTER;
  const normalizedVariant =
    normalizedType === "global" && normalizedMode === "mania"
      ? state.variant ?? "all"
      : null;

  return {
    ...state,
    country: normalizedType === "global" ? state.country : null,
    filter: normalizedFilter,
    mode: normalizedMode,
    sort: normalizedSort,
    type: normalizedType,
    variant: normalizedVariant,
  };
}

export function parseRankingState(
  routeParams: {
    mode?: string;
    sort?: string;
    type?: string;
  },
  rawParams: Record<string, string | string[] | undefined>,
): RankingSearchState {
  const modeValue = routeParams.mode;
  const typeValue = routeParams.type;
  const sortValue = routeParams.sort;
  const filterValue = getString(rawParams.filter);
  const variantValue = getString(rawParams.variant);
  const countryValue = getString(rawParams.country);
  const pageValue = Number.parseInt(getString(rawParams.page) ?? "1", 10);

  const mode = isRuleset(modeValue) ? modeValue : DEFAULT_MODE;
  const type = isRankingType(typeValue) ? typeValue : DEFAULT_TYPE;
  const sort = isRankingSort(sortValue) ? sortValue : DEFAULT_SORT;
  const page = Number.isFinite(pageValue) && pageValue > 0 ? pageValue : 1;

  const filter = isUserFilter(filterValue) ? filterValue : DEFAULT_FILTER;

  let variant: Variant = null;

  if (mode === "mania") {
    variant = isVariant(variantValue)
      ? ((variantValue ?? "all") as Variant)
      : "all";
  }

  return normalizeRankingState({
    country:
      type === "global" && countryValue != null && countryValue.length > 0
        ? countryValue.toUpperCase()
        : null,
    filter,
    mode,
    page,
    sort,
    type,
    variant,
  });
}

export function buildRankingsPath({
  mode,
  sort,
  type,
}: Pick<RankingSearchState, "mode" | "sort" | "type">) {
  return `/rankings/${mode}/${type}/${normalizeSortForType(type, sort)}`;
}

export function buildRankingsHref(state: RankingSearchState) {
  const normalizedState = normalizeRankingState(state);
  const searchParams = new URLSearchParams();

  if (normalizedState.type === "global") {
    searchParams.set("filter", normalizedState.filter);

    if (normalizedState.country != null) {
      searchParams.set("country", normalizedState.country);
    }

    if (
      normalizedState.mode === "mania" &&
      normalizedState.variant != null &&
      normalizedState.variant !== "all"
    ) {
      searchParams.set("variant", normalizedState.variant);
    }
  }

  if (normalizedState.page > 1) {
    searchParams.set("page", String(normalizedState.page));
  }

  const queryString = searchParams.toString();
  const pathname = buildRankingsPath(normalizedState);

  return queryString.length > 0 ? `${pathname}?${queryString}` : pathname;
}

export function getModeLabel(mode: Ruleset) {
  return modeLabels[mode];
}

export function getRankingTypeLabel(type: RankingType) {
  return rankingTypeLabels[type];
}

export function getVariantOptions(mode: Ruleset) {
  return mode === "mania" ? [...maniaVariantOptions] : null;
}

const integerFormatter = new Intl.NumberFormat("en-US");
const percentageFormatter = new Intl.NumberFormat("en-US", {
  maximumFractionDigits: 2,
  minimumFractionDigits: 2,
  style: "percent",
});

export function formatInteger(value: number) {
  return integerFormatter.format(value);
}

export function formatAccuracy(value: number) {
  return percentageFormatter.format(value);
}

export function getFlagEmoji(code: string | null | undefined) {
  if (code == null || code.length !== 2) {
    return "🏳";
  }

  return code
    .toUpperCase()
    .split("")
    .map((character) =>
      String.fromCodePoint(127397 + character.charCodeAt(0)),
    )
    .join("");
}

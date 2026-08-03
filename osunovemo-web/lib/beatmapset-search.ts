type QueryValue = string | string[] | undefined;

export type BeatmapsetSearchOption<T extends string | null = string | null> = {
  label: string;
  value: T;
};

export const BEATMAPSET_SEARCH_GENERAL_OPTIONS = [
  { label: "Recommended difficulty", value: "recommended" },
  { label: "Include converted beatmaps", value: "converts" },
  { label: "Subscribed mappers", value: "follows" },
  { label: "Spotlighted beatmaps", value: "spotlights" },
  { label: "Featured Artists", value: "featured_artists" },
] as const satisfies readonly BeatmapsetSearchOption<string>[];

export const BEATMAPSET_SEARCH_EXTRA_OPTIONS = [
  { label: "Has Video", value: "video" },
  { label: "Has Storyboard", value: "storyboard" },
] as const satisfies readonly BeatmapsetSearchOption<string>[];

export const BEATMAPSET_SEARCH_MODE_OPTIONS = [
  { label: "Any", value: null },
  { label: "osu!", value: "0" },
  { label: "osu!taiko", value: "1" },
  { label: "osu!catch", value: "2" },
  { label: "osu!mania", value: "3" },
] as const satisfies readonly BeatmapsetSearchOption<string | null>[];

export const BEATMAPSET_SEARCH_STATUS_OPTIONS = [
  { label: "Any", value: "any" },
  { label: "Leaderboard", value: "leaderboard" },
  { label: "Ranked", value: "ranked" },
  { label: "Qualified", value: "qualified" },
  { label: "Loved", value: "loved" },
  { label: "Pending", value: "pending" },
  { label: "WIP", value: "wip" },
  { label: "Graveyard", value: "graveyard" },
  { label: "Favourites", value: "favourites" },
  { label: "My Maps", value: "mine" },
] as const satisfies readonly BeatmapsetSearchOption<string>[];

export const BEATMAPSET_SEARCH_NSFW_OPTIONS = [
  { label: "Hide", value: "0" },
  { label: "Show", value: "1" },
] as const satisfies readonly BeatmapsetSearchOption<string>[];

export const BEATMAPSET_SEARCH_PLAYED_OPTIONS = [
  { label: "Any", value: "any" },
  { label: "Played", value: "played" },
  { label: "Unplayed", value: "unplayed" },
] as const satisfies readonly BeatmapsetSearchOption<string>[];

export const BEATMAPSET_SEARCH_RANK_OPTIONS = [
  { label: "Silver SS", value: "XH" },
  { label: "SS", value: "X" },
  { label: "Silver S", value: "SH" },
  { label: "S", value: "S" },
  { label: "A", value: "A" },
  { label: "B", value: "B" },
  { label: "C", value: "C" },
  { label: "D", value: "D" },
] as const satisfies readonly BeatmapsetSearchOption<string>[];

export const BEATMAPSET_SEARCH_GENRE_OPTIONS = [
  { label: "Any", value: null },
  { label: "Unspecified", value: "1" },
  { label: "Video Game", value: "2" },
  { label: "Anime", value: "3" },
  { label: "Rock", value: "4" },
  { label: "Pop", value: "5" },
  { label: "Other", value: "6" },
  { label: "Novelty", value: "7" },
  { label: "Hip Hop", value: "9" },
  { label: "Electronic", value: "10" },
  { label: "Metal", value: "11" },
  { label: "Classical", value: "12" },
  { label: "Folk", value: "13" },
  { label: "Jazz", value: "14" },
] as const satisfies readonly BeatmapsetSearchOption<string | null>[];

export const BEATMAPSET_SEARCH_LANGUAGE_OPTIONS = [
  { label: "Any", value: null },
  { label: "English", value: "2" },
  { label: "Chinese", value: "4" },
  { label: "French", value: "7" },
  { label: "German", value: "8" },
  { label: "Italian", value: "11" },
  { label: "Japanese", value: "3" },
  { label: "Korean", value: "6" },
  { label: "Spanish", value: "10" },
  { label: "Swedish", value: "9" },
  { label: "Russian", value: "12" },
  { label: "Polish", value: "13" },
  { label: "Instrumental", value: "5" },
  { label: "Other", value: "14" },
  { label: "Unspecified", value: "1" },
] as const satisfies readonly BeatmapsetSearchOption<string | null>[];

export const BEATMAPSET_SEARCH_SORT_FIELDS = [
  "title",
  "artist",
  "difficulty",
  "updated",
  "ranked",
  "rating",
  "plays",
  "favourites",
  "relevance",
  "nominations",
] as const;

export const BEATMAPSET_SEARCH_VIEW_OPTIONS = [
  { label: "Nano", value: "nano" },
  { label: "Mini", value: "mini" },
  { label: "Normal", value: "normal" },
  { label: "Extra", value: "extra" },
  { label: "Cover", value: "cover" },
  { label: "List", value: "list" },
] as const satisfies readonly BeatmapsetSearchOption<string>[];

export type BeatmapsetSearchGeneralValue =
  (typeof BEATMAPSET_SEARCH_GENERAL_OPTIONS)[number]["value"];
export type BeatmapsetSearchExtraValue =
  (typeof BEATMAPSET_SEARCH_EXTRA_OPTIONS)[number]["value"];
export type BeatmapsetSearchStatus =
  (typeof BEATMAPSET_SEARCH_STATUS_OPTIONS)[number]["value"];
export type BeatmapsetSearchPlayedValue =
  (typeof BEATMAPSET_SEARCH_PLAYED_OPTIONS)[number]["value"];
export type BeatmapsetSearchRankValue =
  (typeof BEATMAPSET_SEARCH_RANK_OPTIONS)[number]["value"];
export type BeatmapsetSearchSortField = (typeof BEATMAPSET_SEARCH_SORT_FIELDS)[number];
export type BeatmapsetSearchSortOrder = "asc" | "desc";
export type BeatmapsetSearchSort =
  `${BeatmapsetSearchSortField}_${BeatmapsetSearchSortOrder}`;
export type BeatmapsetSearchView =
  (typeof BEATMAPSET_SEARCH_VIEW_OPTIONS)[number]["value"];

export type BeatmapsetSearchState = {
  extra: BeatmapsetSearchExtraValue[];
  general: BeatmapsetSearchGeneralValue[];
  genre: string[];
  language: string[];
  mode: string[];
  nsfw: boolean;
  page: number;
  played: BeatmapsetSearchPlayedValue[];
  query: string;
  rank: BeatmapsetSearchRankValue[];
  sort: BeatmapsetSearchSort;
  status: BeatmapsetSearchStatus[];
  view: BeatmapsetSearchView;
};

export type BeatmapsetSearchQueryParams = Record<string, QueryValue>;

type BeatmapsetSearchMultiKey =
  | "extra"
  | "general"
  | "genre"
  | "language"
  | "mode"
  | "played"
  | "rank"
  | "status";
type BeatmapsetSearchStateKey = keyof BeatmapsetSearchState;
const DEFAULT_STATUS: BeatmapsetSearchStatus = "leaderboard";
const DEFAULT_VIEW: BeatmapsetSearchView = "normal";

const beatmapsetSearchSortLabels: Record<BeatmapsetSearchSortField, string> = {
  artist: "Artist",
  difficulty: "Difficulty",
  favourites: "Favourites",
  nominations: "Nominations",
  plays: "Plays",
  ranked: "Ranked",
  rating: "Rating",
  relevance: "Relevance",
  title: "Title",
  updated: "Updated",
};

function isPresent<T>(value: T): value is NonNullable<T> {
  return value != null;
}

const optionValues = {
  extra: BEATMAPSET_SEARCH_EXTRA_OPTIONS.map((option) => option.value),
  general: BEATMAPSET_SEARCH_GENERAL_OPTIONS.map((option) => option.value),
  language: BEATMAPSET_SEARCH_LANGUAGE_OPTIONS.map((option) => option.value).filter(isPresent),
  mode: BEATMAPSET_SEARCH_MODE_OPTIONS.map((option) => option.value).filter(isPresent),
  played: BEATMAPSET_SEARCH_PLAYED_OPTIONS.map((option) => option.value),
  rank: BEATMAPSET_SEARCH_RANK_OPTIONS.map((option) => option.value),
  status: BEATMAPSET_SEARCH_STATUS_OPTIONS.map((option) => option.value),
  genre: BEATMAPSET_SEARCH_GENRE_OPTIONS.map((option) => option.value).filter(isPresent),
} as const;

const multiValueOrder = {
  extra: new Map(optionValues.extra.map((value, index) => [value, index])),
  general: new Map(optionValues.general.map((value, index) => [value, index])),
  genre: new Map(optionValues.genre.map((value, index) => [value, index])),
  language: new Map(optionValues.language.map((value, index) => [value, index])),
  mode: new Map(optionValues.mode.map((value, index) => [value, index])),
  played: new Map(optionValues.played.map((value, index) => [value, index])),
  rank: new Map(optionValues.rank.map((value, index) => [value, index])),
  status: new Map(optionValues.status.map((value, index) => [value, index])),
} as const;

function getSingleValue(value: QueryValue) {
  return Array.isArray(value) ? value[0] : value;
}

function parseBoolean(value: QueryValue) {
  const singleValue = getSingleValue(value);
  return singleValue === "1" || singleValue === "true";
}

function parsePage(value: QueryValue) {
  const singleValue = getSingleValue(value);
  const parsed = Number.parseInt(singleValue ?? "", 10);
  return Number.isFinite(parsed) && parsed > 0 ? parsed : 1;
}

function parseMultiValue<T extends string>(value: QueryValue, allowedValues: readonly T[]) {
  const allowedValueSet = new Set(allowedValues);
  const values = (getSingleValue(value) ?? "")
    .split(".")
    .filter((entry): entry is T => allowedValueSet.has(entry as T));

  return [...new Set(values)];
}

function parseSort(value: QueryValue) {
  const singleValue = getSingleValue(value);
  if (singleValue == null) {
    return null;
  }

  const [field, order] = singleValue.split("_");
  if (
    !BEATMAPSET_SEARCH_SORT_FIELDS.includes(field as BeatmapsetSearchSortField) ||
    (order !== "asc" && order !== "desc")
  ) {
    return null;
  }

  return {
    field: field as BeatmapsetSearchSortField,
    order: order as BeatmapsetSearchSortOrder,
  };
}

function parseView(value: QueryValue) {
  const singleValue = getSingleValue(value);
  return BEATMAPSET_SEARCH_VIEW_OPTIONS.find((option) => option.value === singleValue)?.value ?? null;
}

function sortMultiValues<K extends BeatmapsetSearchMultiKey>(
  key: K,
  values: BeatmapsetSearchState[K],
) {
  const order = multiValueOrder[key] as ReadonlyMap<string, number>;

  return [...values].sort(
    (left, right) =>
      (order.get(left as string) ?? Number.MAX_SAFE_INTEGER) -
      (order.get(right as string) ?? Number.MAX_SAFE_INTEGER),
  );
}

function sanitizeQuery(query: string) {
  return query.trim();
}

function sanitizeOptionalAnyValues<T extends string>(values: T[], anyValue: T) {
  if (values.length === 0) {
    return values;
  }

  return values.includes(anyValue) ? values.filter((value) => value !== anyValue) : values;
}

export function getEffectiveStatuses(statuses: BeatmapsetSearchStatus[]) {
  return statuses.length > 0 ? statuses : [DEFAULT_STATUS];
}

export function getDefaultBeatmapsetSearchSort(input: {
  query: string;
  status: BeatmapsetSearchStatus[];
}): BeatmapsetSearchSort {
  if (input.query.length > 0) {
    return "relevance_desc";
  }

  const statuses = getEffectiveStatuses(input.status);

  if (
    statuses.length === 1 &&
    ["pending", "wip", "graveyard", "mine"].includes(statuses[0])
  ) {
    return "updated_desc";
  }

  return "ranked_desc";
}

export function getVisibleBeatmapsetSearchSortFields(input: {
  query: string;
  status: BeatmapsetSearchStatus[];
}) {
  const fields: BeatmapsetSearchSortField[] = [
    "title",
    "artist",
    "difficulty",
    "rating",
    "plays",
    "favourites",
  ];

  if (input.query.length > 0) {
    fields.push("relevance");
  }

  const statuses = getEffectiveStatuses(input.status);

  if (statuses.includes("pending")) {
    fields.push("nominations");
  }

  if (statuses.some((status) => ["graveyard", "pending", "wip", "any", "favourites", "mine"].includes(status))) {
    fields.push("updated");
  }

  if (statuses.some((status) => !["graveyard", "pending", "wip"].includes(status))) {
    fields.push("ranked");
  }

  return fields;
}

export function getBeatmapsetSearchSortLabel(field: BeatmapsetSearchSortField) {
  return beatmapsetSearchSortLabels[field];
}

export function normalizeBeatmapsetSearchState(
  state: Partial<BeatmapsetSearchState> & Pick<BeatmapsetSearchState, "query" | "status">,
): BeatmapsetSearchState {
  const query = sanitizeQuery(state.query);
  // "any" is kept here, unlike for `played`. That helper treats "any" as "no filter", which only
  // holds when the group's default IS any — status defaults to "leaderboard", so dropping "any"
  // silently collapsed it back into the default and made Any impossible to select.
  const parsedStatus = parseMultiValue(
    state.status?.join(".") ?? state.status,
    optionValues.status,
  ) as BeatmapsetSearchStatus[];
  const status = sortMultiValues(
    "status",
    // Any is the broadest option, so it wins outright if combined with narrower ones.
    parsedStatus.includes("any") ? (["any"] as BeatmapsetSearchStatus[]) : parsedStatus,
  );
  const defaultSort = getDefaultBeatmapsetSearchSort({ query, status });
  const visibleSortFields = new Set(getVisibleBeatmapsetSearchSortFields({ query, status }));
  const parsedSort = parseSort(state.sort);

  return {
    extra: sortMultiValues(
      "extra",
      parseMultiValue(state.extra?.join(".") ?? state.extra, optionValues.extra) as BeatmapsetSearchExtraValue[],
    ),
    general: sortMultiValues(
      "general",
      parseMultiValue(
        state.general?.join(".") ?? state.general,
        optionValues.general,
      ) as BeatmapsetSearchGeneralValue[],
    ),
    genre: sortMultiValues(
      "genre",
      parseMultiValue(state.genre?.join(".") ?? state.genre, optionValues.genre),
    ),
    language: sortMultiValues(
      "language",
      parseMultiValue(state.language?.join(".") ?? state.language, optionValues.language),
    ),
    mode: sortMultiValues(
      "mode",
      parseMultiValue(state.mode?.join(".") ?? state.mode, optionValues.mode),
    ),
    nsfw: Boolean(state.nsfw),
    page: typeof state.page === "number" && state.page > 0 ? Math.floor(state.page) : 1,
    played: sortMultiValues(
      "played",
      sanitizeOptionalAnyValues(
        parseMultiValue(state.played?.join(".") ?? state.played, optionValues.played),
        "any",
      ) as BeatmapsetSearchPlayedValue[],
    ),
    query,
    rank: sortMultiValues(
      "rank",
      parseMultiValue(state.rank?.join(".") ?? state.rank, optionValues.rank) as BeatmapsetSearchRankValue[],
    ),
    sort:
      parsedSort != null && visibleSortFields.has(parsedSort.field)
        ? `${parsedSort.field}_${parsedSort.order}`
        : defaultSort,
    status,
    view: parseView(state.view) ?? DEFAULT_VIEW,
  };
}

export function parseBeatmapsetSearchState(searchParams: BeatmapsetSearchQueryParams) {
  const query = sanitizeQuery(getSingleValue(searchParams.q) ?? "");

  return normalizeBeatmapsetSearchState({
    extra: parseMultiValue(searchParams.e, optionValues.extra),
    general: parseMultiValue(searchParams.c, optionValues.general),
    genre: parseMultiValue(searchParams.g, optionValues.genre),
    language: parseMultiValue(searchParams.l, optionValues.language),
    mode: parseMultiValue(searchParams.m, optionValues.mode),
    nsfw: parseBoolean(searchParams.nsfw),
    page: parsePage(searchParams.page),
    played: parseMultiValue(searchParams.played, optionValues.played),
    query,
    rank: parseMultiValue(searchParams.r, optionValues.rank),
    sort: getSingleValue(searchParams.sort) as BeatmapsetSearchSort | undefined,
    status: parseMultiValue(searchParams.s, optionValues.status),
    view: parseView(searchParams.view) ?? undefined,
  });
}

export function getPublicBeatmapsetSearchState(state?: Pick<BeatmapsetSearchState, "page">) {
  return normalizeBeatmapsetSearchState({
    page: state?.page ?? 1,
    query: "",
    status: [],
  });
}

export function buildBeatmapsetSearchParams(
  state: BeatmapsetSearchState,
  omitKeys: BeatmapsetSearchStateKey[] = [],
) {
  const omitKeySet = new Set(omitKeys);
  const params = new URLSearchParams();
  const defaultSort = getDefaultBeatmapsetSearchSort({
    query: state.query,
    status: state.status,
  });

  if (!omitKeySet.has("query") && state.query.length > 0) {
    params.set("q", state.query);
  }

  if (!omitKeySet.has("general") && state.general.length > 0) {
    params.set("c", state.general.join("."));
  }

  if (!omitKeySet.has("extra") && state.extra.length > 0) {
    params.set("e", state.extra.join("."));
  }

  if (!omitKeySet.has("genre") && state.genre.length > 0) {
    params.set("g", state.genre.join("."));
  }

  if (!omitKeySet.has("language") && state.language.length > 0) {
    params.set("l", state.language.join("."));
  }

  if (!omitKeySet.has("mode") && state.mode.length > 0) {
    params.set("m", state.mode.join("."));
  }

  if (!omitKeySet.has("nsfw") && state.nsfw) {
    params.set("nsfw", "1");
  }

  if (!omitKeySet.has("played") && state.played.length > 0) {
    params.set("played", state.played.join("."));
  }

  if (!omitKeySet.has("rank") && state.rank.length > 0) {
    params.set("r", state.rank.join("."));
  }

  if (!omitKeySet.has("status") && state.status.length > 0) {
    params.set("s", state.status.join("."));
  }

  if (!omitKeySet.has("sort") && state.sort !== defaultSort) {
    params.set("sort", state.sort);
  }

  if (!omitKeySet.has("view") && state.view !== DEFAULT_VIEW) {
    params.set("view", state.view);
  }

  if (!omitKeySet.has("page") && state.page > 1) {
    params.set("page", state.page.toString());
  }

  return params;
}

export function buildBeatmapsetSearchHref(
  state: BeatmapsetSearchState,
  omitKeys: BeatmapsetSearchStateKey[] = [],
) {
  const search = buildBeatmapsetSearchParams(state, omitKeys).toString();
  return search.length > 0 ? `/beatmapsets?${search}` : "/beatmapsets";
}

export function toggleBeatmapsetSearchMultiValue<K extends BeatmapsetSearchMultiKey>(
  state: BeatmapsetSearchState,
  key: K,
  value: BeatmapsetSearchState[K][number],
) {
  const currentValues = state[key] as Array<BeatmapsetSearchState[K][number]>;
  const hasValue = currentValues.includes(value);
  const nextValues = hasValue
    ? currentValues.filter((entry) => entry !== value)
    : [...currentValues, value];

  return normalizeBeatmapsetSearchState({
    ...state,
    [key]: nextValues as BeatmapsetSearchState[K],
    page: 1,
  });
}

export function setBeatmapsetSearchMultiValues<K extends BeatmapsetSearchMultiKey>(
  state: BeatmapsetSearchState,
  key: K,
  values: BeatmapsetSearchState[K],
) {
  return normalizeBeatmapsetSearchState({
    ...state,
    [key]: values,
    page: 1,
  });
}

export function toggleBeatmapsetSearchSort(
  state: BeatmapsetSearchState,
  field: BeatmapsetSearchSortField,
) {
  const currentSort = parseSort(state.sort);
  const nextOrder: BeatmapsetSearchSortOrder =
    currentSort?.field === field && currentSort.order === "desc" ? "asc" : "desc";

  return normalizeBeatmapsetSearchState({
    ...state,
    page: 1,
    sort: `${field}_${nextOrder}`,
  });
}

export function setBeatmapsetSearchView(
  state: BeatmapsetSearchState,
  view: BeatmapsetSearchView,
) {
  return normalizeBeatmapsetSearchState({
    ...state,
    view,
  });
}

export function hasActiveAdvancedBeatmapsetSearchFilters(state: BeatmapsetSearchState) {
  return (
    state.genre.length > 0 ||
    state.language.length > 0 ||
    state.extra.length > 0 ||
    state.rank.length > 0 ||
    state.played.length > 0
  );
}

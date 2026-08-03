import "server-only";

import {cache} from "react";
import {OsuApiError, fetchOsuApi} from "@/lib/osu-api";
import type {Ruleset} from "@/lib/rankings";

export const profileExtraPages = [
    "beatmaps",
    "historical",
    "kudosu",
    "me",
    "medals",
    "recent_activity",
    "top_ranks",
    "account_standing",
] as const;
export const profileScoreSections = ["best", "firsts", "pinned", "recent"] as const;
export const profileScoreBatchSize = 5;
export const profileScoreLoadMoreBatchSize = 50;

export type ProfileExtraPage = (typeof profileExtraPages)[number];
export type ScoreSection = (typeof profileScoreSections)[number];
export type BeatmapsetSection =
    | "favourite"
    | "graveyard"
    | "guest"
    | "loved"
    | "nominated"
    | "pending"
    | "ranked";

export type ProfileCountry = {
    code: string;
    name: string;
};

export type ProfileTeam = {
    flag_url: string | null;
    id: number;
    name: string;
    short_name: string;
};

export type ProfileGroup = {
    colour: string | null;
    id: number;
    identifier: string;
    name: string;
    short_name: string;
};

export type ProfileBadge = {
    awarded_at: string;
    description: string;
    "image@2x_url": string;
    image_url: string;
    url: string;
};

export type ProfileCover = {
    custom_url: string | null;
    id: string | null;
    url: string | null;
};

export type ProfileUserAchievement = {
    achieved_at: string;
    achievement_id: number;
};

export type ProfileAchievement = {
    description: string;
    grouping: string;
    icon_url: string;
    id: number;
    mode: Ruleset | null;
    name: string;
    ordering: number;
};

export type RankHistory = {
    data: number[];
    mode: Ruleset;
};

export type RankHighest = {
    rank: number;
    updated_at: string;
};

export type UserStatistics = {
    accuracy: number;
    country_rank?: number | null;
    global_rank: number | null;
    grade_counts: Record<"a" | "s" | "sh" | "ss" | "ssh", number>;
    hit_accuracy: number;
    is_ranked: boolean;
    level: {
        current: number;
        progress: number;
    };
    maximum_combo: number;
    play_count: number;
    play_time: number;
    pp: number;
    ranked_score: number;
    replays_watched_by_others: number;
    total_hits: number;
    total_score: number;
    variants?: Array<{
        country_rank: number | null;
        global_rank: number | null;
        mode: Ruleset;
        pp: number;
        variant: "4k" | "7k";
    }>;
};

export type MonthlyCount = {
    count: number;
    start_date: string;
};

export type KudosuSummary = {
    available: number;
    total: number;
};

export type AccountHistoryEntry = {
    description?: string | null;
    id: number;
    length: number;
    permanent: boolean;
    timestamp: string;
    type: string;
};

export type ProfileUser = {
    account_history: AccountHistoryEntry[];
    avatar_url: string;
    badges: ProfileBadge[];
    comments_count?: number;
    country: ProfileCountry | null;
    country_code: string;
    cover: ProfileCover;
    discord: string | null;
    follower_count: number;
    graveyard_beatmapset_count?: number;
    groups: ProfileGroup[];
    id: number;
    interests: string | null;
    is_active: boolean;
    is_bot: boolean;
    is_online: boolean;
    is_supporter: boolean;
    join_date: string;
    kudosu: KudosuSummary;
    last_visit: string | null;
    location: string | null;
    loved_beatmapset_count?: number;
    mapping_follower_count: number;
    monthly_playcounts: MonthlyCount[];
    occupation: string | null;
    page: {
        html: string;
        raw: string;
    };
    pending_beatmapset_count?: number;
    playmode: Ruleset;
    playstyle: string[];
    post_count: number;
    previous_usernames: string[];
    profile_colour: string | null;
    profile_hue: number | null;
    profile_order: ProfileExtraPage[];
    rank_highest: RankHighest | null;
    rank_history: RankHistory | null;
    ranked_beatmapset_count?: number;
    replays_watched_counts: MonthlyCount[];
    scores_best_count?: number;
    scores_first_count?: number;
    scores_pinned_count?: number;
    scores_recent_count?: number;
    statistics: UserStatistics;
    support_level?: number;
    team?: ProfileTeam | null;
    title: string | null;
    title_url: string | null;
    twitter: string | null;
    user_achievements: ProfileUserAchievement[];
    username: string;
    website: string | null;
};

export type Beatmap = {
    accuracy?: number;
    ar?: number;
    beatmapset_id?: number;
    bpm?: number;
    count_circles?: number;
    count_sliders?: number;
    count_spinners?: number;
    cs?: number;
    difficulty_rating: number;
    drain?: number;
    hit_length?: number;
    id: number;
    max_combo?: number | null;
    mode: Ruleset;
    passcount?: number;
    playcount?: number;
    status: string;
    total_length?: number;
    user_id?: number;
    url: string;
    version: string;
};

export type Beatmapset = {
    anime_cover?: boolean;
    artist: string;
    artist_unicode: string;
    availability?: {
        download_disabled: boolean;
        more_information: string | null;
    } | null;
    covers: {
        card: string;
        cover: string;
        list: string;
        slimcover: string;
    };
    creator: string;
    favourite_count: number;
    has_favourited?: boolean;
    hype?: {
        current: number;
        required: number;
    } | null;
    id: number;
    last_updated?: string | null;
    nominations_summary?: {
        current: number;
        eligible_main_rulesets?: Ruleset[];
        required_meta: {
            main_ruleset: number;
            non_main_ruleset: number;
        };
    } | null;
    nsfw?: boolean;
    play_count: number;
    preview_url: string;
    ranked_date?: string | null;
    source: string;
    spotlight?: boolean;
    status: string;
    storyboard?: boolean;
    track_id?: number | null;
    title: string;
    title_unicode: string;
    user_id: number;
    video?: boolean;
    beatmaps?: Beatmap[];
};

export type ScoreMod = {
    acronym: string;
    settings?: Record<string, boolean | number | string>;
};

export type Score = {
    accuracy: number;
    beatmap?: Beatmap;
    beatmapset?: Beatmapset;
    ended_at: string;
    id: number;
    max_combo: number;
    mods?: ScoreMod[];
    passed: boolean;
    pp: number | null;
    rank: string;
    statistics: Partial<Record<string, number>>;
    total_score: number;
    user_id: number;
    weight?: {
        percentage: number;
        pp: number;
    };
};

function normalizeScoreModSettingValue(value: boolean | number | string) {
    if (typeof value !== "string") {
        return value;
    }

    if (value === "true") {
        return true;
    }

    if (value === "false") {
        return false;
    }

    const numericValue = Number(value);
    return Number.isFinite(numericValue) ? numericValue : value;
}

export function normalizeScore<T extends Score>(score: T): T {
    return {
        ...score,
        mods: score.mods?.map((mod) => ({
            ...mod,
            settings: mod.settings == null
                ? undefined
                : Object.fromEntries(
                    Object.entries(mod.settings).map(([key, value]) => [
                        key,
                        normalizeScoreModSettingValue(value),
                    ]),
                ),
        })),
    } as T;
}

export function normalizeScores<T extends Score>(scores: T[]) {
    return scores.map(normalizeScore);
}

export type ProfileEvent =
    | {
    achievement: {
        icon_url: string;
        name: string;
    };
    created_at: string;
    id: number;
    type: "achievement";
    user: {
        url: string;
        username: string;
    };
}
    | {
    beatmap: {
        title: string;
        url: string;
    };
    count: number;
    created_at: string;
    id: number;
    type: "beatmapPlaycount";
}
    | {
    approval: string;
    beatmapset: {
        title: string;
        url: string;
    };
    created_at: string;
    id: number;
    type: "beatmapsetApprove";
    user: {
        url: string;
        username: string;
    };
}
    | {
    beatmapset: {
        title: string;
        url: string;
    };
    created_at: string;
    id: number;
    type: "beatmapsetDelete" | "beatmapsetRevive" | "beatmapsetUpdate" | "beatmapsetUpload";
    user?: {
        url: string;
        username: string;
    };
}
    | {
    beatmap: {
        title: string;
        url: string;
    };
    created_at: string;
    id: number;
    mode: Ruleset;
    rank: number;
    scoreRank: string;
    type: "rank";
    user: {
        url: string;
        username: string;
    };
}
    | {
    beatmap: {
        title: string;
        url: string;
    };
    created_at: string;
    id: number;
    mode: Ruleset;
    type: "rankLost";
    user: {
        url: string;
        username: string;
    };
}
    | {
    created_at: string;
    id: number;
    type: "usernameChange";
    user: {
        previousUsername: string;
        url: string;
        username: string;
    };
}
    | {
    created_at: string;
    id: number;
    type: "userSupportAgain" | "userSupportFirst" | "userSupportGift";
    user: {
        url: string;
        username: string;
    };
};

export type KudosuHistory = {
    action: string;
    amount: number;
    created_at: string;
    giver: {
        url: string;
        username: string;
    } | null;
    id: number;
    model: string;
    post: {
        title: string;
        url: string | null;
    };
};

export type ProfilePageData = {
    achievements: ProfileAchievement[] | null;
    beatmapsets: Partial<Record<BeatmapsetSection, Beatmapset[] | null>>;
    currentMode: Ruleset;
    kudosuHistory: KudosuHistory[] | null;
    recentActivity: ProfileEvent[] | null;
    scores: Partial<Record<ScoreSection, Score[] | null>>;
    source: "api";
    user: ProfileUser;
};

const defaultCover: ProfileCover = {
    custom_url: null,
    id: null,
    url: null,
};

const defaultStatistics: UserStatistics = {
    accuracy: 0,
    country_rank: null,
    global_rank: null,
    grade_counts: {
        a: 0,
        s: 0,
        sh: 0,
        ss: 0,
        ssh: 0,
    },
    hit_accuracy: 0,
    is_ranked: false,
    level: {
        current: 0,
        progress: 0,
    },
    maximum_combo: 0,
    play_count: 0,
    play_time: 0,
    pp: 0,
    ranked_score: 0,
    replays_watched_by_others: 0,
    total_hits: 0,
    total_score: 0,
    variants: [],
};

function normalizeStatistics(statistics: Partial<UserStatistics> | null | undefined): UserStatistics {
    return {
        ...defaultStatistics,
        ...statistics,
        grade_counts: {
            ...defaultStatistics.grade_counts,
            ...statistics?.grade_counts,
        },
        level: {
            ...defaultStatistics.level,
            ...statistics?.level,
        },
        variants: statistics?.variants ?? [],
    };
}

function normalizeProfileUser(
    user: Partial<ProfileUser>,
    requestedMode?: Ruleset,
): ProfileUser {
    const playmode = user.playmode ?? requestedMode ?? "osu";

    return {
        account_history: user.account_history ?? [],
        avatar_url: user.avatar_url ?? "/icons/rankings.svg",
        badges: user.badges ?? [],
        comments_count: user.comments_count ?? 0,
        country: user.country ?? null,
        country_code: user.country_code ?? user.country?.code ?? "XX",
        cover: {
            ...defaultCover,
            ...user.cover,
        },
        discord: user.discord ?? null,
        follower_count: user.follower_count ?? 0,
        graveyard_beatmapset_count: user.graveyard_beatmapset_count ?? 0,
        groups: user.groups ?? [],
        id: user.id ?? 0,
        interests: user.interests ?? null,
        is_active: user.is_active ?? true,
        is_bot: user.is_bot ?? false,
        is_online: user.is_online ?? false,
        is_supporter: user.is_supporter ?? false,
        join_date: user.join_date ?? new Date(0).toISOString(),
        kudosu: {
            available: user.kudosu?.available ?? 0,
            total: user.kudosu?.total ?? 0,
        },
        last_visit: user.last_visit ?? null,
        location: user.location ?? null,
        loved_beatmapset_count: user.loved_beatmapset_count ?? 0,
        mapping_follower_count: user.mapping_follower_count ?? 0,
        monthly_playcounts: user.monthly_playcounts ?? [],
        occupation: user.occupation ?? null,
        page: {
            html: user.page?.html ?? "",
            raw: user.page?.raw ?? "",
        },
        pending_beatmapset_count: user.pending_beatmapset_count ?? 0,
        playmode,
        playstyle: user.playstyle ?? [],
        post_count: user.post_count ?? 0,
        previous_usernames: user.previous_usernames ?? [],
        profile_colour: user.profile_colour ?? null,
        profile_hue: user.profile_hue ?? null,
        profile_order: user.profile_order ?? [...profileExtraPages],
        rank_highest: user.rank_highest ?? null,
        rank_history: user.rank_history ?? null,
        ranked_beatmapset_count: user.ranked_beatmapset_count ?? 0,
        replays_watched_counts: user.replays_watched_counts ?? [],
        scores_best_count: user.scores_best_count ?? 0,
        scores_first_count: user.scores_first_count ?? 0,
        scores_pinned_count: user.scores_pinned_count ?? 0,
        scores_recent_count: user.scores_recent_count ?? 0,
        statistics: normalizeStatistics(user.statistics),
        support_level: user.support_level ?? 0,
        team: user.team ?? null,
        title: user.title ?? null,
        title_url: user.title_url ?? null,
        twitter: user.twitter ?? null,
        user_achievements: user.user_achievements ?? [],
        username: user.username ?? `user-${user.id ?? "unknown"}`,
        website: user.website ?? null,
    };
}

async function fetchOptional<T>(
    path: string,
    searchParams?: Record<string, string | number>,
) {
    try {
        return await fetchOsuApi<T>(path, searchParams);
    } catch {
        return null;
    }
}

export function isProfileNotFound(error: unknown) {
    return error instanceof OsuApiError && error.status === 404;
}

export function isScoreSection(value: string): value is ScoreSection {
    return profileScoreSections.includes(value as ScoreSection);
}

function buildProfileScoreSearchParams(
    section: ScoreSection,
    mode: Ruleset,
    options?: {
        limit?: number;
        offset?: number;
    },
) {
    const searchParams: Record<string, string | number> = {
        limit: options?.limit ?? profileScoreBatchSize,
        mode,
    };

    if ((options?.offset ?? 0) > 0) {
        searchParams.offset = options?.offset ?? 0;
    }

    if (section === "recent") {
        searchParams.include_fails = 1;
    }

    return searchParams;
}

export async function fetchProfileScores(
    userId: number | string,
    section: ScoreSection,
    mode: Ruleset,
    options?: {
        limit?: number;
        offset?: number;
    },
) {
    const scores = await fetchOsuApi<Score[]>(
        `users/${userId}/scores/${section}`,
        buildProfileScoreSearchParams(section, mode, options),
    );

    return normalizeScores(scores);
}

async function fetchOptionalProfileScores(
    userId: number,
    section: ScoreSection,
    mode: Ruleset,
    options?: {
        limit?: number;
        offset?: number;
    },
) {
    try {
        return await fetchProfileScores(userId, section, mode, options);
    } catch {
        return null;
    }
}

const fetchUserProfileByKey = cache(async (user: string, mode?: Ruleset) => {
    const path = mode == null ? `users/${user}` : `users/${user}/${mode}`;
    const searchParams = /^\d+$/.test(user) ? undefined : {key: "username"};

    return normalizeProfileUser(await fetchOsuApi<ProfileUser>(path, searchParams), mode);
});

export async function fetchUserProfile(user: string, mode?: Ruleset) {
    return fetchUserProfileByKey(user, mode);
}

export async function fetchProfilePageData(userKey: string, mode?: Ruleset): Promise<ProfilePageData> {
    const user = await fetchUserProfile(userKey, mode);
    const currentMode = mode ?? user.playmode;

    const [
        pinned,
        best,
        firsts,
        recent,
        favourite,
        ranked,
        loved,
        guest,
        pending,
        graveyard,
        nominated,
        recentActivity,
        kudosuHistory,
    ] = await Promise.all([
        fetchOptionalProfileScores(user.id, "pinned", currentMode),
        fetchOptionalProfileScores(user.id, "best", currentMode),
        fetchOptionalProfileScores(user.id, "firsts", currentMode),
        fetchOptionalProfileScores(user.id, "recent", currentMode),
        fetchOptional<Beatmapset[]>(`users/${user.id}/beatmapsets/favourite`, {limit: 4}),
        fetchOptional<Beatmapset[]>(`users/${user.id}/beatmapsets/ranked`, {limit: 4}),
        fetchOptional<Beatmapset[]>(`users/${user.id}/beatmapsets/loved`, {limit: 4}),
        fetchOptional<Beatmapset[]>(`users/${user.id}/beatmapsets/guest`, {limit: 4}),
        fetchOptional<Beatmapset[]>(`users/${user.id}/beatmapsets/pending`, {limit: 4}),
        fetchOptional<Beatmapset[]>(`users/${user.id}/beatmapsets/graveyard`, {limit: 4}),
        fetchOptional<Beatmapset[]>(`users/${user.id}/beatmapsets/nominated`, {limit: 4}),
        fetchOptional<ProfileEvent[]>(`users/${user.id}/recent_activity`, {limit: 10}),
        fetchOptional<KudosuHistory[]>(`users/${user.id}/kudosu`, {limit: 10}),
    ]);

    return {
        achievements: null,
        beatmapsets: {
            favourite,
            graveyard,
            guest,
            loved,
            nominated,
            pending,
            ranked,
        },
        currentMode,
        kudosuHistory,
        recentActivity,
        scores: {
            best,
            firsts,
            pinned,
            recent,
        },
        source: "api",
        user,
    };
}

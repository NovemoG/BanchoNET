import "server-only";

import {cache} from "react";
import {fetchOsuApi, resolveAssetUrl} from "@/lib/osu-api";

export type FriendCountry = {
    code: string;
    name: string;
};

export type FriendGroup = {
    colour?: string | null;
    color?: string | null;
    id: number;
    name: string;
};

export type FriendUser = {
    avatar_url?: string | null;
    country?: FriendCountry | null;
    country_code?: string | null;
    cover?: {
        custom_url?: string | null;
        url?: string | null;
    } | null;
    groups?: FriendGroup[] | null;
    id: number;
    is_bot?: boolean;
    is_deleted?: boolean;
    is_online?: boolean;
    is_supporter?: boolean;
    last_visit?: string | null;
    profile_colour?: string | null;
    statistics?: {
        global_rank?: number | null;
        pp?: number | null;
    } | null;
    support_level?: number | null;
    username: string;
};

type FriendRelation = {
    target?: FriendUser | null;
};

type FriendsApiResponse =
    | Array<FriendRelation | FriendUser>
    | {
    data?: Array<FriendRelation | FriendUser>;
};

function isRecord(value: unknown): value is Record<string, unknown> {
    return typeof value === "object" && value !== null;
}

function getResponseItems(response: FriendsApiResponse) {
    if (Array.isArray(response)) {
        return response;
    }

    return response.data ?? [];
}

function getFriendUser(item: FriendRelation | FriendUser): FriendUser | null {
    if (!isRecord(item)) {
        return null;
    }

    if ("target" in item && isRecord(item.target)) {
        return item.target as FriendUser;
    }

    return item as FriendUser;
}

function isValidFriendUser(user: FriendUser | null): user is FriendUser {
    return user != null && Number.isFinite(user.id) && typeof user.username === "string";
}

export function resolveFriendAssetUrl(url?: string | null) {
    if (url == null || url.length === 0) {
        return null;
    }

    if (url.startsWith("local:/")) {
        return url.replace("local:", "");
    }

    return resolveAssetUrl(url);
}

export function getFriendCoverUrl(user?: { cover?: FriendUser["cover"]; avatar_url?: string | null } | null) {
    return resolveFriendAssetUrl(user?.cover?.url ?? user?.avatar_url ?? null);
}

export const fetchFriends = cache(async () => {
    const response = await fetchOsuApi<FriendsApiResponse>("friends", undefined, {auth: "user"});

    return getResponseItems(response)
        .map(getFriendUser)
        .filter(isValidFriendUser);
});

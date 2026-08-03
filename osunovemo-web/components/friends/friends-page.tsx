import type {CSSProperties, ReactNode} from "react";
import Link from "next/link";
import {Grid2X2, Heart, LayoutGrid, List, UserRound, UsersRound} from "lucide-react";
import {HomeHeader} from "@/components/home/home-header";
import {PageWrapper} from "@/components/page-wrapper";
import type {FriendUser} from "@/lib/friends";
import {getFriendCoverUrl, resolveFriendAssetUrl} from "@/lib/friends";
import {cn} from "@/lib/utils";

type FriendFilter = "all" | "offline" | "online";
type FriendSort = "last_visit" | "rank" | "username";
type FriendView = "brick" | "card" | "list";

export type FriendsPageState = {
    filter: FriendFilter;
    sort: FriendSort;
    view: FriendView;
};

type FriendsPageProps = {
    currentUserCoverUrl?: string | null;
    errorMessage?: string | null;
    friends: FriendUser[];
    state: FriendsPageState;
};

const filters: Array<{label: string; value: FriendFilter}> = [
    {label: "all", value: "all"},
    {label: "online", value: "online"},
    {label: "offline", value: "offline"},
];

const sortModes: Array<{label: string; value: FriendSort}> = [
    {label: "Recently active", value: "last_visit"},
    {label: "Rank", value: "rank"},
    {label: "Username", value: "username"},
];

const viewModes: Array<{icon: ReactNode; label: string; value: FriendView}> = [
    {icon: <LayoutGrid aria-hidden className="size-4"/>, label: "card", value: "card"},
    {icon: <List aria-hidden className="size-4"/>, label: "list", value: "list"},
    {icon: <Grid2X2 aria-hidden className="size-4"/>, label: "brick", value: "brick"},
];

const defaultState: FriendsPageState = {
    filter: "all",
    sort: "last_visit",
    view: "card",
};

const numberFormatter = new Intl.NumberFormat("en-US");
const relativeFormatter = new Intl.RelativeTimeFormat("en-US", {numeric: "auto"});

function isFilter(value: string): value is FriendFilter {
    return filters.some((filter) => filter.value === value);
}

function isSort(value: string): value is FriendSort {
    return sortModes.some((sort) => sort.value === value);
}

function isView(value: string): value is FriendView {
    return viewModes.some((view) => view.value === value);
}

function getSearchParamValue(value: string | string[] | undefined) {
    return Array.isArray(value) ? value[0] : value;
}

export function parseFriendsPageState(searchParams: Record<string, string | string[] | undefined>): FriendsPageState {
    const filter = getSearchParamValue(searchParams.filter);
    const sort = getSearchParamValue(searchParams.sort);
    const view = getSearchParamValue(searchParams.view);

    return {
        filter: filter != null && isFilter(filter) ? filter : defaultState.filter,
        sort: sort != null && isSort(sort) ? sort : defaultState.sort,
        view: view != null && isView(view) ? view : defaultState.view,
    };
}

function buildFriendsHref(state: FriendsPageState, updates: Partial<FriendsPageState>) {
    const nextState = {...state, ...updates};
    const searchParams = new URLSearchParams();

    if (nextState.filter !== defaultState.filter) {
        searchParams.set("filter", nextState.filter);
    }

    if (nextState.sort !== defaultState.sort) {
        searchParams.set("sort", nextState.sort);
    }

    if (nextState.view !== defaultState.view) {
        searchParams.set("view", nextState.view);
    }

    const queryString = searchParams.toString();
    return queryString.length > 0 ? `/friends?${queryString}` : "/friends";
}

function getBackgroundStyle(url: string | null): CSSProperties | undefined {
    return url == null ? undefined : {backgroundImage: `url(${JSON.stringify(url)})`};
}

function getProfileHref(user: FriendUser) {
    return `/users/${encodeURIComponent(String(user.id))}`;
}

function getRank(user: FriendUser) {
    return user.statistics?.global_rank ?? null;
}

function getPp(user: FriendUser) {
    return user.statistics?.pp ?? null;
}

function formatRank(user: FriendUser) {
    const rank = getRank(user);
    return rank == null ? "unranked" : `#${numberFormatter.format(rank)}`;
}

function formatPp(user: FriendUser) {
    const pp = getPp(user);
    return pp == null ? null : `${numberFormatter.format(Math.round(pp))} pp`;
}

function formatRelativeTime(value?: string | null) {
    if (value == null) {
        return null;
    }

    const date = new Date(value);
    if (Number.isNaN(date.getTime())) {
        return null;
    }

    const seconds = Math.round((date.getTime() - Date.now()) / 1000);
    const units: Array<[Intl.RelativeTimeFormatUnit, number]> = [
        ["year", 60 * 60 * 24 * 365],
        ["month", 60 * 60 * 24 * 30],
        ["week", 60 * 60 * 24 * 7],
        ["day", 60 * 60 * 24],
        ["hour", 60 * 60],
        ["minute", 60],
    ];

    for (const [unit, scale] of units) {
        if (Math.abs(seconds) >= scale) {
            return relativeFormatter.format(Math.round(seconds / scale), unit);
        }
    }

    return relativeFormatter.format(seconds, "second");
}

function getStatusText(user: FriendUser) {
    if (user.is_online) {
        return "online";
    }

    const lastVisit = formatRelativeTime(user.last_visit);
    return lastVisit == null ? "offline" : `last seen ${lastVisit}`;
}

function getFilteredFriends(friends: FriendUser[], filter: FriendFilter) {
    if (filter === "online") {
        return friends.filter((friend) => friend.is_online);
    }

    if (filter === "offline") {
        return friends.filter((friend) => !friend.is_online);
    }

    return friends;
}

function compareUsernames(left: FriendUser, right: FriendUser) {
    return left.username.localeCompare(right.username, "en", {sensitivity: "base"});
}

function getLastVisitTime(user: FriendUser) {
    if (user.is_online) {
        return Number.POSITIVE_INFINITY;
    }

    const date = new Date(user.last_visit ?? "");
    return Number.isNaN(date.getTime()) ? 0 : date.getTime();
}

function sortFriends(friends: FriendUser[], sort: FriendSort) {
    const sortedFriends = friends.slice();

    if (sort === "rank") {
        return sortedFriends.sort((left, right) => (getRank(left) ?? Number.MAX_VALUE) - (getRank(right) ?? Number.MAX_VALUE));
    }

    if (sort === "username") {
        return sortedFriends.sort(compareUsernames);
    }

    return sortedFriends.sort((left, right) => {
        if (left.is_online && right.is_online) {
            return compareUsernames(left, right);
        }

        if (left.is_online || right.is_online) {
            return left.is_online ? -1 : 1;
        }

        return getLastVisitTime(right) - getLastVisitTime(left);
    });
}

function StatusDot({online}: { online?: boolean }) {
    return (
        <span
            className={cn(
                "size-6 shrink-0 rounded-full border-4 border-osu-b6 bg-osu-b2",
                online && "border-osu-green-1 bg-osu-b6",
            )}
        />
    );
}

function Avatar({className, user}: { className?: string; user: FriendUser }) {
    const avatarUrl = resolveFriendAssetUrl(user.avatar_url);

    return (
        <span
            className={cn("flex shrink-0 items-center justify-center rounded-md bg-osu-b4 bg-cover bg-center text-osu-c1", className)}
            style={getBackgroundStyle(avatarUrl)}
        >
            {avatarUrl == null ? <UserRound aria-hidden className="size-5"/> : null}
        </span>
    );
}

function FriendCard({user}: { user: FriendUser }) {
    const coverUrl = getFriendCoverUrl(user);
    const pp = formatPp(user);

    return (
        <Link
            aria-label={user.username}
            className="group relative flex h-36 overflow-hidden rounded-md bg-osu-b3 text-white shadow-[0_2px_10px_rgba(0,0,0,0.25)] outline-none transition-[box-shadow,transform] hover:-translate-y-0.5 hover:shadow-[0_8px_24px_rgba(0,0,0,0.34)] focus-visible:ring-2 focus-visible:ring-osu-c2/60"
            href={getProfileHref(user)}
        >
            {coverUrl != null ? (
                <span
                    className="absolute inset-0 bg-cover bg-center opacity-65 transition-transform duration-200 group-hover:scale-[1.03]"
                    style={getBackgroundStyle(coverUrl)}
                />
            ) : null}
            <span
                className={cn(
                    "absolute inset-0",
                    user.is_online
                        ? "bg-[linear-gradient(180deg,hsl(var(--hsl-b5)/0.50),hsl(var(--hsl-b5)/0.84))]"
                        : "bg-[linear-gradient(180deg,hsl(var(--hsl-b5)/0.62),hsl(var(--hsl-b5)/0.9))]",
                )}
            />
            <span className="relative flex min-w-0 flex-1 flex-col justify-between p-2.5">
                <span className="flex min-w-0 items-start gap-2.5">
                    <Avatar className="size-15" user={user}/>
                    <span className="grid min-w-0 flex-1 gap-1">
                        <span className="flex min-w-0 items-center gap-1.5">
                            <span className="truncate text-base font-semibold">{user.username}</span>
                            {user.is_supporter ? <Heart aria-hidden className="size-4 shrink-0 fill-current text-osu-h1"/> : null}
                        </span>
                        <span className="truncate text-xs font-semibold text-osu-l1">{formatRank(user)}</span>
                        {pp != null ? <span className="truncate text-xs text-osu-c2">{pp}</span> : null}
                    </span>
                </span>
                <span className="flex min-w-0 items-center gap-2">
                    <StatusDot online={user.is_online}/>
                    <span className="min-w-0 truncate text-sm text-osu-c1">{getStatusText(user)}</span>
                </span>
            </span>
        </Link>
    );
}

function FriendListItem({user}: { user: FriendUser }) {
    const coverUrl = getFriendCoverUrl(user);
    const pp = formatPp(user);

    return (
        <Link
            className="group relative flex min-h-12 overflow-hidden rounded-md bg-osu-b3 text-white shadow-[0_1px_6px_rgba(0,0,0,0.22)] outline-none transition-colors hover:bg-osu-b2 focus-visible:ring-2 focus-visible:ring-osu-c2/60"
            href={getProfileHref(user)}
        >
            {coverUrl != null ? (
                <span
                    className="absolute inset-y-0 right-0 w-1/2 bg-cover bg-center opacity-30"
                    style={getBackgroundStyle(coverUrl)}
                />
            ) : null}
            <span className="absolute inset-0 bg-[linear-gradient(to_right,hsl(var(--hsl-b5))_48%,hsl(var(--hsl-b5)/0.72))]"/>
            <span className="relative flex min-w-0 flex-1 items-center gap-2.5 pr-3">
                <Avatar className="size-12 rounded-l-md rounded-r-none" user={user}/>
                <span className="grid min-w-0 flex-1 grid-cols-[minmax(0,1fr)_auto] items-center gap-x-3 gap-y-0.5 py-1.5">
                    <span className="flex min-w-0 items-center gap-1.5">
                        <span className="truncate font-semibold">{user.username}</span>
                        {user.is_supporter ? <Heart aria-hidden className="size-3.5 shrink-0 fill-current text-osu-h1"/> : null}
                    </span>
                    <span className="text-right text-sm font-semibold text-osu-l1">{formatRank(user)}</span>
                    <span className="truncate text-xs text-osu-c2">{getStatusText(user)}</span>
                    {pp != null ? <span className="text-right text-xs text-osu-c2">{pp}</span> : null}
                </span>
                <StatusDot online={user.is_online}/>
            </span>
        </Link>
    );
}

function FriendBrick({user}: { user: FriendUser }) {
    const group = user.groups?.[0];
    const groupColor = group?.colour ?? group?.color ?? "hsl(var(--hsl-b1))";

    return (
        <Link
            className={cn(
                "flex max-w-full items-center rounded-md bg-osu-b2/80 px-2 py-1 text-sm text-white shadow-[inset_0_0_0_2px_transparent] transition-colors hover:bg-osu-b1",
                user.is_online && "shadow-[inset_0_0_0_2px_hsl(var(--hsl-green-2))]",
            )}
            href={getProfileHref(user)}
        >
            <span
                className="mr-2 h-4 w-1 shrink-0 rounded-full"
                style={{backgroundColor: groupColor}}
                title={group?.name}
            />
            <span className="truncate font-semibold">{user.username}</span>
        </Link>
    );
}

function CountLink({
                       active,
                       count,
                       href,
                       label,
                   }: {
    active: boolean;
    count: number;
    href: string;
    label: string;
}) {
    return (
        <Link
            className={cn(
                "group flex min-h-18 flex-col justify-center bg-osu-b4 px-5 py-3 transition-colors hover:bg-osu-b3",
                active && "bg-osu-b3",
            )}
            href={href}
        >
            <span className="mb-2 h-1 w-full max-w-28 rounded-full bg-osu-h1 opacity-30 transition-opacity group-hover:opacity-75"/>
            <span className={cn("text-sm font-semibold text-osu-l1", active && "text-white")}>{label}</span>
            <span className="text-2xl font-bold text-white">{numberFormatter.format(count)}</span>
        </Link>
    );
}

function ToolbarLink({
                         active,
                         children,
                         href,
                         title,
                     }: {
    active: boolean;
    children: ReactNode;
    href: string;
    title?: string;
}) {
    return (
        <Link
            className={cn(
                "inline-flex min-h-8 items-center gap-1 rounded-md px-2.5 py-1.5 text-sm font-semibold text-osu-l1 transition-colors hover:bg-osu-b4 hover:text-white",
                active && "bg-osu-b4 text-white",
            )}
            href={href}
            title={title}
        >
            {children}
        </Link>
    );
}

function FriendsToolbar({state}: { state: FriendsPageState }) {
    return (
        <div className="flex flex-wrap items-center justify-between gap-3 bg-osu-b4 px-4 py-4 sm:px-6 lg:px-8">
            <div className="flex flex-wrap items-center gap-1.5">
                <span className="mr-1 text-sm font-semibold text-white">Sort by</span>
                {sortModes.map((sort) => (
                    <ToolbarLink
                        active={state.sort === sort.value}
                        href={buildFriendsHref(state, {sort: sort.value})}
                        key={sort.value}
                    >
                        {sort.label}
                    </ToolbarLink>
                ))}
            </div>

            <div className="flex flex-wrap items-center gap-1.5">
                {viewModes.map((view) => (
                    <ToolbarLink
                        active={state.view === view.value}
                        href={buildFriendsHref(state, {view: view.value})}
                        key={view.value}
                        title={view.label}
                    >
                        {view.icon}
                        <span className="sr-only">{view.label}</span>
                    </ToolbarLink>
                ))}
            </div>
        </div>
    );
}

function FriendsGrid({friends, view}: { friends: FriendUser[]; view: FriendView }) {
    if (friends.length === 0) {
        return (
            <div className="flex min-h-36 flex-col items-center justify-center gap-2 rounded-md bg-osu-b4 px-5 py-8 text-center text-osu-c2">
                <UsersRound aria-hidden className="size-8 text-osu-l1"/>
                <div className="text-base font-semibold text-white">No friends found.</div>
            </div>
        );
    }

    if (view === "brick") {
        return (
            <div className="flex flex-wrap gap-1">
                {friends.map((friend) => <FriendBrick key={friend.id} user={friend}/>)}
            </div>
        );
    }

    if (view === "list") {
        return (
            <div className="grid gap-0.5">
                {friends.map((friend) => <FriendListItem key={friend.id} user={friend}/>)}
            </div>
        );
    }

    return (
        <div className="grid grid-cols-[repeat(auto-fill,minmax(min(16rem,100%),1fr))] gap-2.5">
            {friends.map((friend) => <FriendCard key={friend.id} user={friend}/>)}
        </div>
    );
}

function ErrorPanel({message}: { message: string }) {
    return (
        <div className="rounded-md bg-osu-b4 px-5 py-6 text-osu-c1 shadow-[0_2px_10px_rgba(0,0,0,0.25)]">
            <div className="text-base font-semibold text-white">Friends unavailable</div>
            <p className="mt-2 text-sm text-osu-c2">{message}</p>
        </div>
    );
}

export function FriendsPage({currentUserCoverUrl, errorMessage, friends, state}: FriendsPageProps) {
    const counts = {
        all: friends.length,
        offline: friends.filter((friend) => !friend.is_online).length,
        online: friends.filter((friend) => friend.is_online).length,
    };
    const visibleFriends = sortFriends(getFilteredFriends(friends, state.filter), state.sort);

    return (
        <>
            <HomeHeader active="friends" backgroundImageUrl={currentUserCoverUrl}/>

            <PageWrapper className="mb-10 bg-osu-b5 shadow-[0_12px_32px_rgba(0,0,0,0.24)]" modifiers="generic-compact">
                <div className="grid grid-cols-1 bg-osu-b4 sm:grid-cols-3">
                    {filters.map((filter) => (
                        <CountLink
                            active={state.filter === filter.value}
                            count={counts[filter.value]}
                            href={buildFriendsHref(state, {filter: filter.value})}
                            key={filter.value}
                            label={filter.label}
                        />
                    ))}
                </div>

                <div className="bg-osu-b4">
                    <FriendsToolbar state={state}/>
                    <div className="px-4 pb-5 sm:px-6 lg:px-8">
                        {errorMessage != null ? (
                            <ErrorPanel message={errorMessage}/>
                        ) : (
                            <FriendsGrid friends={visibleFriends} view={state.view}/>
                        )}
                    </div>
                </div>
            </PageWrapper>
        </>
    );
}

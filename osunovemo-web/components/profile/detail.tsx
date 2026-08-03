"use client";

/* eslint-disable @next/next/no-img-element */
import {useState, type ReactNode} from "react";
import {
    Activity,
    ChevronDown,
    ChevronUp,
    Clock3,
    Heart,
    HeartOff,
    Link2,
    MapPin,
    MessageSquare,
    MousePointer2,
    Shield,
    UserRound,
} from "lucide-react";
import {CountryFlag} from "@/components/country-flag";
import {ProfileRankChart} from "@/components/profile/profile-rank-chart";
import {cn} from "@/lib/utils";
import {type Ruleset} from "@/lib/rankings";
import type {ProfileUser} from "@/lib/profile";
import type {FriendRelation} from "@/lib/friend-relation";

type DetailProps = {
    currentMode: Ruleset;
    friendRelation?: FriendRelation | null;
    user: ProfileUser;
};

const friendPillStyles: Record<FriendRelation, string> = {
    friend: "bg-osu-green-1 text-osu-b6 hover:brightness-110",
    mutual: "bg-osu-p text-white hover:brightness-110",
    none: "bg-osu-b4 text-white hover:bg-osu-b2",
};

const integerFormatter = new Intl.NumberFormat("en-US");
const decimalFormatter = new Intl.NumberFormat("en-US", {
    maximumFractionDigits: 2,
    minimumFractionDigits: 2,
});
const dateFormatter = new Intl.DateTimeFormat("en-US", {
    day: "numeric",
    month: "short",
    year: "numeric",
});
const relativeFormatter = new Intl.RelativeTimeFormat("en-US", {numeric: "auto"});

function formatInteger(value: number | null | undefined) {
    return integerFormatter.format(Math.round(value ?? 0));
}

function formatDecimal(value: number | null | undefined) {
    return decimalFormatter.format(value ?? 0);
}

function formatAccuracy(value: number | null | undefined) {
    return `${formatDecimal((value ?? 0) * 100)}%`;
}

function formatDate(value: string | null | undefined) {
    if (value == null) {
        return null;
    }

    const date = new Date(value);
    if (Number.isNaN(date.getTime())) {
        return null;
    }

    return dateFormatter.format(date);
}

function formatRelativeTime(value: string | null | undefined) {
    if (value == null) {
        return "unknown";
    }

    const date = new Date(value);
    if (Number.isNaN(date.getTime())) {
        return "unknown";
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

function formatPlayTime(seconds: number) {
    const days = Math.floor(seconds / 86400);
    const hours = Math.floor((seconds % 86400) / 3600);
    const minutes = Math.floor((seconds % 3600) / 60);
    return days > 0 ? `${days}d ${hours}h ${minutes}m` : `${hours}h ${minutes}m`;
}

function buildRankHistoryData(user: ProfileUser) {
    const raw = user.rank_history?.data ?? [];
    const data = raw
        .map((rank, index) => ({
            offsetDays: index - raw.length + 1,
            value: rank,
        }))
        .filter((point) => point.value > 0);

    if (data.length === 0 && user.statistics.global_rank != null && user.statistics.global_rank > 0) {
        data.push({offsetDays: 0, value: user.statistics.global_rank});
    }

    if (data.length === 1) {
        data.unshift({
            offsetDays: data[0].offsetDays - 1,
            value: data[0].value,
        });
    }

    const lastPoint = data.at(-1);
    if (
        lastPoint != null &&
        lastPoint.offsetDays !== 0 &&
        user.statistics.global_rank != null &&
        user.statistics.global_rank > 0
    ) {
        data.push({offsetDays: 0, value: user.statistics.global_rank});
    }

    return data;
}

function resolveProfileAsset(src: string) {
    if (src.startsWith("local:/")) {
        return src.replace("local:", "");
    }

    if (/^https?:\/\//i.test(src)) {
        return src;
    }

    const apiBaseUrl = process.env.NEXT_PUBLIC_OSU_API_BASE_URL ?? "https://osu.novemo.dev/api/v2";
    return new URL(src, `${new URL(apiBaseUrl).origin}/`).toString();
}

function ValueBlock({
                        label,
                        value,
                    }: {
    label: string;
    value: ReactNode;
}) {
    return (
        <div className="min-w-0 text-left">
            <div className="text-[0.8rem] text-osu-f1">{label}</div>
            <div className="mt-0.5 text-[1.15rem] leading-[1.1] font-bold text-white">{value}</div>
        </div>
    );
}

function PreviousUsernames({usernames}: { usernames: string[] }) {
    if (usernames.length === 0) {
        return null;
    }

    const text = usernames.join(", ");

    return (
        <div
            className="group/previous flex items-baseline rounded-2xl text-shadow-none transition-all duration-200 ease-[cubic-bezier(0.22,0.61,0.36,1)] lg:m-1.25 lg:w-75 lg:pointer-events-none lg:hover:-translate-y-4 lg:hover:bg-osu-b6 lg:hover:pointer-events-auto">
            <div className="ml-2.5 inline-flex text-base text-white lg:hidden" title={`Previous usernames: ${text}`}>
                <UserRound className="size-4"/>
            </div>
            <div className="hidden p-2.5 text-base text-white lg:inline-flex">
                <UserRound className="size-4"/>
            </div>
            <div
                className="hidden p-2.5 opacity-0 transition-all duration-200 ease-[cubic-bezier(0.22,0.61,0.36,1)] lg:block lg:group-hover/previous:opacity-100">
                <div className="text-[0.8rem]">Previous usernames</div>
                <div className="mt-0 text-[0.95rem] leading-[1.2] font-bold whitespace-pre-wrap">{text}</div>
            </div>
        </div>
    );
}

function ProfileInfo({
                         currentMode,
                         user,
                     }: {
    currentMode: Ruleset;
    user: ProfileUser;
}) {
    const coverAsset = user.cover.url === null ? null : resolveProfileAsset(user.cover.url);
    const [showCover, setShowCover] = useState(coverAsset !== null);

    return (
        <div className="flex w-full flex-col bg-osu-b3">
            {(showCover && coverAsset !== null) && (
                <div
                    className="relative h-25 bg-cover bg-center lg:h-62.5 after:absolute after:inset-0 after:bg-[linear-gradient(180deg,rgba(8,10,14,0.16),rgba(8,10,14,0.42))] after:content-['']"
                    style={{backgroundImage: `url('${coverAsset}')`}}
                />
            )}

            <div className="relative flex items-center px-5 py-2.5 lg:h-21.25 lg:py-0">
                <div
                    className={cn(
                        "z-1 overflow-hidden rounded-[20px]",
                        showCover
                            ? "h-16.25 w-16.25 lg:mb-2.5 lg:h-30 lg:w-30 lg:self-end lg:rounded-[40px]"
                            : "h-16.25 w-16.25",
                    )}
                >
                    <img
                        alt={user.username}
                        className="h-full w-full object-cover"
                        src={resolveProfileAsset(user.avatar_url)}
                    />
                </div>

                <div
                    className={cn("min-w-0 flex-1 [text-shadow:0_1px_2px_rgba(0,0,0,0.45)]", showCover ? "ml-2.5 lg:ml-5" : "ml-2.5")}>
                    <h1 className="-mt-1.25 flex max-w-full items-center p-0 text-[1.6rem] leading-normal font-normal lg:text-[2rem]">
                        <div className="truncate">{user.username}</div>
                        <div
                            className="inline-block translate-y-[-0.3em] lg:absolute lg:-top-4 lg:left-full lg:translate-y-0">
                            <PreviousUsernames usernames={user.previous_usernames}/>
                        </div>
                        <div className="ml-1.5 flex items-baseline gap-1 text-[15px]">
                            {user.groups.map((group) => (
                                <div key={group.id} className="inline-flex text-white"
                                     style={{color: group.colour ?? undefined}}>
                                    {group.short_name}
                                </div>
                            ))}
                        </div>
                    </h1>

                    {user.title ? (
                        user.title_url ? (
                            <a
                                className="mt-1 inline-block text-base leading-normal"
                                href={user.title_url}
                                rel="noreferrer"
                                style={{color: user.profile_colour ?? undefined}}
                                target="_blank"
                            >
                                {user.title}
                            </a>
                        ) : (
                            <div className="mt-1 inline-block text-base leading-normal"
                                 style={{color: user.profile_colour ?? undefined}}>
                                {user.title}
                            </div>
                        )
                    ) : null}

                    <div
                        className="mt-2.5 flex flex-wrap gap-1.25 text-[15px] lg:mt-1.25 lg:flex-nowrap lg:gap-2.5 lg:text-[18px]">
                        {user.country ? (
                            <a
                                className="grid items-center gap-1 text-white lg:grid-cols-[auto_1fr]"
                                href={`/rankings/${currentMode}/global/performance?country=${user.country.code}`}
                            >
                                <CountryFlag className="text-lg" code={user.country.code} name={user.country.name}/>
                                <div className="hidden text-[0.95rem] lg:block">{user.country.name}</div>
                            </a>
                        ) : null}
                        {user.team ? (
                            <a className="grid items-center gap-1 text-white lg:grid-cols-[auto_1fr]" href="#">
                                {user.team.flag_url ? (
                                    <img
                                        alt={user.team.name}
                                        className="h-4.5 w-6.5 rounded-xs object-cover"
                                        src={resolveProfileAsset(user.team.flag_url)}
                                    />
                                ) : null}
                                <div className="hidden truncate text-[0.95rem] lg:block">{user.team.name}</div>
                            </a>
                        ) : null}
                    </div>
                </div>

                {coverAsset != null ? (
                    <div className="absolute top-0 right-5 flex h-full items-center">
                        <button
                            className="inline-flex size-7.5 items-center justify-center rounded-full border-0 bg-osu-b4 text-white"
                            onClick={() => setShowCover((current) => !current)}
                            title={showCover ? "Hide cover" : "Show cover"}
                            type="button"
                        >
                            {showCover ? <ChevronUp className="size-4"/> : <ChevronDown className="size-4"/>}
                        </button>
                    </div>
                ) : null}
            </div>
        </div>
    );
}

function ProfileBadges({user}: { user: ProfileUser }) {
    if (user.badges.length === 0) {
        return null;
    }

    return (
        <div
            className="flex flex-wrap gap-2.5 bg-osu-b4 px-5 py-2.5 shadow-[inset_0_1px_0_rgba(255,255,255,0.04)] lg:px-12.5">
            {user.badges.map((badge) => (
                <a key={`${badge.image_url}-${badge.awarded_at}`} href={badge.url} rel="noreferrer" target="_blank">
                    <img
                        alt={badge.description}
                        className="h-10 w-21.5 object-contain"
                        src={resolveProfileAsset(badge["image@2x_url"] || badge.image_url)}
                        title={`${badge.description}${badge.awarded_at ? ` - ${formatDate(badge.awarded_at)}` : ""}`}
                    />
                </a>
            ))}
        </div>
    );
}

const scoreRankAssets: Record<"XH" | "X" | "SH" | "S" | "A", string> = {
    A: "/badges/score-ranks-v2019/GradeSmall-A.svg",
    S: "/badges/score-ranks-v2019/GradeSmall-S.svg",
    SH: "/badges/score-ranks-v2019/GradeSmall-S-Silver.svg",
    X: "/badges/score-ranks-v2019/GradeSmall-SS.svg",
    XH: "/badges/score-ranks-v2019/GradeSmall-SS-Silver.svg",
};

function ScoreRankBadge({rank}: { rank: keyof typeof scoreRankAssets }) {
    return <img alt={rank} className="block h-5.5 w-11 object-contain" src={scoreRankAssets[rank]}/>;
}

function ProfileNumbers({user}: { user: ProfileUser }) {
    const rankHistoryData = buildRankHistoryData(user);

    return (
        <div className="grid gap-2.5 bg-osu-b5 px-5 py-2.5">
            <div className="grid items-start gap-5 lg:grid-cols-[minmax(0,1fr)_auto_auto] lg:items-stretch lg:gap-6">
                <div className="flex h-full flex-col gap-4.5 lg:gap-5.5">
                    <div className="flex flex-col gap-3 lg:flex-row lg:items-start lg:justify-between">
                        <div className="flex gap-5">
                            <ValueBlock
                                label="Global Rank"
                                value={user.statistics.global_rank != null ? `#${formatInteger(user.statistics.global_rank)}` : "-"}
                            />
                            <ValueBlock
                                label="Country Rank"
                                value={user.statistics.country_rank != null ? `#${formatInteger(user.statistics.country_rank)}` : "-"}
                            />
                        </div>
                    </div>

                    <div
                        className="relative flex h-22.5 min-h-22.5 max-h-22.5 w-full flex-none items-center justify-center overflow-visible py-1 text-base">
                        <ProfileRankChart
                            className="h-full min-h-0 max-h-full w-full"
                            data={rankHistoryData}
                            emptyLabel="Unranked"
                            strokeColor="hsl(var(--hsl-h1))"
                        />
                    </div>

                    <div className="flex flex-col gap-3 lg:flex-row lg:items-end lg:justify-between">
                        <div className="grid grid-cols-2 gap-x-5">
                            <ValueBlock label="pp" value={formatInteger(user.statistics.pp)}/>
                            <ValueBlock label="Play Time" value={formatPlayTime(user.statistics.play_time)}/>
                        </div>

                        <div className="flex gap-5">
                            <div
                                className="mx-auto flex flex-wrap justify-center gap-1.5 text-center text-[0.95rem] font-bold">
                                {([
                                    ["XH", user.statistics.grade_counts.ssh ?? 0],
                                    ["X", user.statistics.grade_counts.ss ?? 0],
                                    ["SH", user.statistics.grade_counts.sh ?? 0],
                                    ["S", user.statistics.grade_counts.s ?? 0],
                                    ["A", user.statistics.grade_counts.a ?? 0],
                                ] as Array<[keyof typeof scoreRankAssets, number]>).map(([label, value]) => (
                                    <div key={label} className="grid min-w-11 justify-items-center gap-0.5">
                                        <ScoreRankBadge rank={label}/>
                                        <div>{formatInteger(value)}</div>
                                    </div>
                                ))}
                            </div>
                        </div>
                    </div>
                </div>

                <div className="h-0.5 w-full bg-osu-b6 lg:h-full lg:w-0.5"/>

                <div
                    className="grid auto-rows-min grid-cols-2 content-start gap-x-5 gap-y-1 self-stretch text-base lg:grid-cols-[auto_auto]">
                    {[
                        ["Ranked Score", formatInteger(user.statistics.ranked_score)],
                        ["Hit Accuracy", formatAccuracy(user.statistics.accuracy)],
                        ["Play Count", formatInteger(user.statistics.play_count)],
                        ["Total Score", formatInteger(user.statistics.total_score)],
                        ["Total Hits", formatInteger(user.statistics.total_hits)],
                        ["Hits Per Play", formatInteger(user.statistics.total_hits / Math.max(user.statistics.play_count, 1))],
                        ["Maximum Combo", formatInteger(user.statistics.maximum_combo)],
                        ["Replays Watched", formatInteger(user.statistics.replays_watched_by_others)],
                    ].map(([label, value]) => (
                        <dl key={label} className="contents">
                            <dt className="font-normal text-osu-f1">{label}</dt>
                            <dd className="text-right font-bold text-white">{value}</dd>
                        </dl>
                    ))}
                </div>
            </div>
        </div>
    );
}

function FollowerPill({friendRelation, user}: { friendRelation?: FriendRelation | null; user: ProfileUser }) {
    const [relation, setRelation] = useState(friendRelation ?? "none");
    const [followers, setFollowers] = useState(user.follower_count);
    const [hovered, setHovered] = useState(false);
    const [pending, setPending] = useState(false);

    const className = cn(
        "inline-flex h-10 items-center gap-2 rounded-full px-3.5 text-[0.95rem] font-semibold transition-colors",
        friendPillStyles[relation],
    );

    if (friendRelation == null) {
        return (
            <div className={className}>
                <Heart className="size-4"/>
                <div>{formatInteger(user.follower_count)}</div>
            </div>
        );
    }

    const isFriend = relation !== "none";

    const toggle = async () => {
        const previousRelation = relation;
        const previousFollowers = followers;

        setPending(true);
        setRelation(isFriend ? "none" : "friend");
        setFollowers((count) => Math.max(count + (isFriend ? -1 : 1), 0));

        try {
            const response = isFriend
                ? await fetch(`/api/friends/${user.id}`, {method: "DELETE"})
                : await fetch(`/api/friends?target=${user.id}`, {method: "POST"});

            if (!response.ok) {
                setRelation(previousRelation);
                setFollowers(previousFollowers);
                return;
            }

            if (!isFriend) {
                const body = (await response.json()) as { user_relation?: { mutual?: boolean } };

                setRelation(body.user_relation?.mutual === true ? "mutual" : "friend");
            }
        } catch {
            setRelation(previousRelation);
            setFollowers(previousFollowers);
        } finally {
            setPending(false);
        }
    };

    return (
        <button
            className={cn(className, "disabled:opacity-60")}
            disabled={pending}
            onClick={() => void toggle()}
            onMouseEnter={() => setHovered(true)}
            onMouseLeave={() => setHovered(false)}
            title={isFriend ? "remove friend" : "add friend"}
            type="button"
        >
            {isFriend && hovered ? <HeartOff className="size-4"/> : <Heart className="size-4"/>}
            <div>{formatInteger(followers)}</div>
        </button>
    );
}

function DetailBar({friendRelation, user}: { friendRelation?: FriendRelation | null; user: ProfileUser }) {
    return (
        <div className="relative flex w-full flex-wrap gap-2.5 bg-osu-b3 px-5 py-2.5 lg:px-12.5">
            <FollowerPill friendRelation={friendRelation} user={user}/>
            <div
                className="inline-flex h-10 items-center gap-2 rounded-full bg-osu-b4 px-3.5 text-[0.95rem] font-semibold text-white">
                <Activity className="size-4"/>
                <div>{formatInteger(user.mapping_follower_count)}</div>
            </div>
        </div>
    );
}

function Links({user}: { user: ProfileUser }) {
    type LinkRowItem = {
        icon: ReactNode;
        label: string;
        value: ReactNode;
    };

    const rows: Array<Array<LinkRowItem | null>> = [
        [
            {
                icon: <Clock3 className="size-4"/>,
                label: "Joined",
                value: formatDate(user.join_date) ?? "-",
            },
            {
                icon: <Clock3 className="size-4"/>,
                label: "Last Visit",
                value: user.is_online ? "Online" : formatRelativeTime(user.last_visit),
            },
            user.playstyle.length > 0
                ? {
                    icon: <MousePointer2 className="size-4"/>,
                    label: "Playstyle",
                    value: user.playstyle.join(", "),
                }
                : null,
        ],
        [
            user.location
                ? {icon: <MapPin className="size-4"/>, label: "Location", value: user.location}
                : null,
            user.interests
                ? {icon: <Heart className="size-4"/>, label: "Interests", value: user.interests}
                : null,
            user.occupation
                ? {icon: <Shield className="size-4"/>, label: "Occupation", value: user.occupation}
                : null,
        ],
        [
            user.twitter
                ? {
                    icon: <MessageSquare className="size-4"/>,
                    label: "Twitter",
                    value: (
                        <a href={`https://twitter.com/${user.twitter}`} rel="noreferrer" target="_blank">
                            @{user.twitter}
                        </a>
                    ),
                }
                : null,
            user.discord
                ? {
                    icon: <MessageSquare className="size-4"/>,
                    label: "Discord",
                    value: user.discord,
                }
                : null,
            user.website
                ? {
                    icon: <Link2 className="size-4"/>,
                    label: "Website",
                    value: (
                        <a href={user.website} rel="noreferrer" target="_blank">
                            {user.website.replace(/^https?:\/\//, "")}
                        </a>
                    ),
                }
                : null,
        ],
    ];

    const filteredRows = rows
        .map((row) => row.filter((item): item is LinkRowItem => item != null))
        .filter((row) => row.length > 0);

    return (
        <div
            className="relative flex min-h-12.5 flex-col justify-center bg-osu-b4 px-5 py-2.5 text-base shadow-[inset_0_1px_0_rgba(255,255,255,0.04)] lg:px-12.5">
            {filteredRows.map((row, index) => (
                <div
                    key={index}
                    className={cn(
                        index === 0
                            ? "grid grid-cols-[repeat(auto-fit,minmax(140px,max-content))] gap-x-2.5 gap-y-1"
                            : "mt-2.5 flex flex-wrap gap-x-2.5 gap-y-1",
                    )}
                >
                    {row.map((item) => (
                        <div key={`${index}-${item.label}`}
                             className={cn("min-w-0", index === 0 && "inline-flex items-center gap-1 whitespace-nowrap")}>
                            <div className="mr-0.5 inline-flex text-osu-f1">{item.icon}</div>
                            <div className={cn("wrap-break-word font-semibold", index === 0 && "inline-block")}>{item.value}</div>
                        </div>
                    ))}
                </div>
            ))}
        </div>
    );
}

export function Detail({currentMode, friendRelation, user}: DetailProps) {
    return (
        <>
            <ProfileInfo currentMode={currentMode} user={user}/>
            <ProfileBadges user={user}/>
            <ProfileNumbers user={user}/>
            <DetailBar friendRelation={friendRelation} user={user}/>
            <Links user={user}/>
        </>
    );
}

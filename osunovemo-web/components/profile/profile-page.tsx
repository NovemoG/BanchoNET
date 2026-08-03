import type {CSSProperties} from "react";
import {Activity, Award, Heart, Star, Trophy, UserRound, Waves} from "lucide-react";
import {Detail} from "@/components/profile/detail";
import type {FriendRelation} from "@/lib/friend-relation";
import {Header} from "@/components/profile/header";
import {ProfileLineChart} from "@/components/profile/profile-line-chart";
import {ProfileScoreSection} from "@/components/profile/profile-score-section";
import {SectionTitle} from "@/components/profile/section-title";
import {BeatmapsetPanel} from "@/components/profile/beatmapset-panel";
import {PageWrapper} from "@/components/page-wrapper";
import type {
    BeatmapsetSection,
    ProfileEvent,
    ProfileExtraPage,
    ProfilePageData,
    ScoreSection,
} from "@/lib/profile";
import type {Ruleset} from "@/lib/rankings";

type ProfilePageProps = {
    data: ProfilePageData;
    friendRelation?: FriendRelation | null;
    ruleset?: Ruleset;
};

const integerFormatter = new Intl.NumberFormat("en-US");
const dateFormatter = new Intl.DateTimeFormat("en-US", {
    day: "numeric",
    month: "short",
    year: "numeric",
});
const monthFormatter = new Intl.DateTimeFormat("en-US", {
    month: "short",
    year: "numeric",
});
const relativeFormatter = new Intl.RelativeTimeFormat("en-US", {numeric: "auto"});

const sectionLabels: Record<ProfileExtraPage, string> = {
    account_standing: "Account Standing",
    beatmaps: "Beatmaps",
    historical: "Historical",
    kudosu: "Kudosu",
    medals: "Medals",
    me: "Me",
    recent_activity: "Recent Activity",
    top_ranks: "Top Ranks",
};

const beatmapSectionLabels: Record<BeatmapsetSection, string> = {
    favourite: "Favourites",
    graveyard: "Graveyard",
    guest: "Guest Difficulty",
    loved: "Loved",
    nominated: "Nominated",
    pending: "Pending",
    ranked: "Ranked",
};

function formatInteger(value: number | null | undefined) {
    return integerFormatter.format(Math.round(value ?? 0));
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

function getKnownScoreSectionCount(data: ProfilePageData, section: ScoreSection) {
    const loadedCount = data.scores[section]?.length ?? 0;

    if (data.source !== "api") {
        return undefined;
    }

    switch (section) {
        case "best":
            return data.user.scores_best_count && data.user.scores_best_count > 0
                ? Math.max(data.user.scores_best_count, loadedCount)
                : undefined;
        case "firsts":
            return data.user.scores_first_count && data.user.scores_first_count > 0
                ? Math.max(data.user.scores_first_count, loadedCount)
                : undefined;
        case "pinned":
            return data.user.scores_pinned_count && data.user.scores_pinned_count > 0
                ? Math.max(data.user.scores_pinned_count, loadedCount)
                : undefined;
        case "recent":
            return data.user.scores_recent_count && data.user.scores_recent_count > 0
                ? Math.max(data.user.scores_recent_count, loadedCount)
                : undefined;
    }
}

function getScoreSectionCount(data: ProfilePageData, section: ScoreSection) {
    return getKnownScoreSectionCount(data, section) ?? (data.scores[section]?.length ?? 0);
}

function getVisibleSections(data: ProfilePageData) {
    const meVisible = data.user.page.html.trim().length > 0;
    const topRanksVisible = Object.values(data.scores).some((section) => (section?.length ?? 0) > 0);
    const beatmapsVisible = Object.values(data.beatmapsets).some((section) => (section?.length ?? 0) > 0);
    const recentVisible = (data.recentActivity?.length ?? 0) > 0;
    const historicalVisible =
        data.user.monthly_playcounts.length > 0 ||
        data.user.replays_watched_counts.length > 0 ||
        (data.scores.recent?.length ?? 0) > 0;
    const kudosuVisible = (data.kudosuHistory?.length ?? 0) > 0 || data.user.kudosu.total > 0;
    const standingVisible = data.user.account_history.length > 0;

    const visibility: Partial<Record<ProfileExtraPage, boolean>> = {
        account_standing: standingVisible,
        beatmaps: beatmapsVisible,
        historical: historicalVisible,
        kudosu: kudosuVisible,
        me: meVisible,
        recent_activity: recentVisible,
        top_ranks: topRanksVisible,
    };

    return data.user.profile_order.filter((section) => visibility[section]).concat(
        standingVisible && !data.user.profile_order.includes("account_standing") ? ["account_standing"] : [],
    );
}

function formatMonthLabel(value: string) {
    const date = new Date(value);
    if (Number.isNaN(date.getTime())) {
        return value;
    }

    return monthFormatter.format(date);
}

function renderEvent(event: ProfileEvent) {
    switch (event.type) {
        case "achievement":
            return {
                icon: <Award className="h-4 w-4"/>,
                text: `Unlocked ${event.achievement.name}`,
                url: event.user.url,
            };
        case "beatmapPlaycount":
            return {
                icon: <Activity className="h-4 w-4"/>,
                text: `${formatInteger(event.count)} plays on ${event.beatmap.title}`,
                url: event.beatmap.url,
            };
        case "beatmapsetApprove":
            return {
                icon: <Trophy className="h-4 w-4"/>,
                text: `${event.approval} ${event.beatmapset.title}`,
                url: event.beatmapset.url,
            };
        case "beatmapsetDelete":
        case "beatmapsetRevive":
        case "beatmapsetUpdate":
        case "beatmapsetUpload":
            return {
                icon: <Activity className="h-4 w-4"/>,
                text: `${event.type.replace("beatmapset", "")} ${event.beatmapset.title}`,
                url: event.beatmapset.url,
            };
        case "rank":
            return {
                icon: <Star className="h-4 w-4"/>,
                text: `Reached #${event.rank} with ${event.scoreRank} on ${event.beatmap.title}`,
                url: event.beatmap.url,
            };
        case "rankLost":
            return {
                icon: <Waves className="h-4 w-4"/>,
                text: `Lost #1 on ${event.beatmap.title}`,
                url: event.beatmap.url,
            };
        case "usernameChange":
            return {
                icon: <UserRound className="h-4 w-4"/>,
                text: `Renamed from ${event.user.previousUsername} to ${event.user.username}`,
                url: event.user.url,
            };
        case "userSupportAgain":
        case "userSupportFirst":
        case "userSupportGift":
            return {
                icon: <Heart className="h-4 w-4"/>,
                text: "Received supporter",
                url: event.user.url,
            };
    }
}

function ExtraSectionHeader({
                                section,
                            }: {
    section: ProfileExtraPage;
}) {
    return <h2
        className="mb-5 inline-block max-w-full border-b-2 border-osu-h1 pb-1.25 text-[1.2rem] font-bold text-osu-c1">{sectionLabels[section]}</h2>;
}

export function ProfilePage({data, friendRelation = null, ruleset}: ProfilePageProps) {
    const {user} = data;
    const currentMode = ruleset ?? data.currentMode;
    const visibleSections = getVisibleSections(data);
    const profileHue = user.profile_hue ?? (user.id % 360);
    const monthlyPlaycounts = user.monthly_playcounts.map((item) => ({
        label: formatMonthLabel(item.start_date),
        value: item.count,
    }));
    const replayCounts = user.replays_watched_counts.map((item) => ({
        label: formatMonthLabel(item.start_date),
        value: item.count,
    }));

    return (
        <main
            className="bg-osu-b6"
            style={profileHue != null ? ({"--base-hue": profileHue.toString()} as CSSProperties) : undefined}
        >
            <Header currentMode={currentMode} user={user}/>

            <PageWrapper modifiers="generic-compact">
                <Detail currentMode={currentMode} friendRelation={friendRelation} user={user}/>

                {visibleSections.length > 1 ? (
                    <div className="sticky top-0 z-30">
                        <div
                            className="flex items-center gap-5 overflow-x-auto bg-osu-b5 px-5 py-2.5 text-base text-[#ccc] max-sm:pt-1.25 max-sm:text-xs lg:px-12.5">
                            {visibleSections.map((section) => (
                                <a
                                    key={section}
                                    className="inline-flex shrink-0 text-osu-f1 transition-colors hover:text-white focus-visible:text-white last:max-sm:pr-2.5"
                                    href={`#${section}`}
                                >
                                    {sectionLabels[section]}
                                </a>
                            ))}
                        </div>
                    </div>
                ) : null}

                <div className="relative grid grid-cols-[minmax(0,1fr)] gap-2.5 py-2.5">
                    {visibleSections.map((section) => {
                        if (section === "me") {
                            return (
                                <section
                                    key={section}
                                    className="relative mx-0.5 rounded-2xl bg-osu-b4 px-4.5 py-5 text-[0.95rem] shadow-[0_12px_28px_rgba(0,0,0,0.28)] lg:mx-2.5 lg:px-10"
                                    id={section}
                                >
                                    <ExtraSectionHeader section={section}/>
                                    <div
                                        className="prose prose-invert max-w-none prose-a:text-osu-h1 prose-p:text-white/80 prose-strong:text-white"
                                        dangerouslySetInnerHTML={{__html: user.page.html}}
                                    />
                                </section>
                            );
                        }

                        if (section === "top_ranks") {
                            return (
                                <section
                                    key={section}
                                    className="relative mx-0.5 rounded-2xl bg-osu-b4 px-4.5 py-5 text-[0.95rem] shadow-[0_12px_28px_rgba(0,0,0,0.28)] lg:mx-2.5 lg:px-10"
                                    id={section}
                                >
                                    <ExtraSectionHeader section={section}/>
                                    <div className="grid gap-6">
                                        {([
                                            ["pinned", "Pinned"],
                                            ["best", "Best Performance"],
                                            ["firsts", "First Place Ranks"],
                                        ] as const).map(([key, label]) =>
                                            (data.scores[key]?.length ?? 0) > 0 ? (
                                                <ProfileScoreSection
                                                    canLoadMore={data.source === "api"}
                                                    count={getScoreSectionCount(data, key)}
                                                    initialScores={data.scores[key] ?? []}
                                                    key={key}
                                                    mode={currentMode}
                                                    section={key}
                                                    showHighlightControl={key === "best"}
                                                    showPpWeight={key === "best"}
                                                    title={label}
                                                    totalCount={getKnownScoreSectionCount(data, key)}
                                                    userId={user.id}
                                                />
                                            ) : null,
                                        )}
                                    </div>
                                </section>
                            );
                        }

                        if (section === "beatmaps") {
                            return (
                                <section
                                    key={section}
                                    className="relative mx-0.5 rounded-2xl bg-osu-b4 px-4.5 py-5 text-[0.95rem] shadow-[0_12px_28px_rgba(0,0,0,0.28)] lg:mx-2.5 lg:px-10"
                                    id={section}
                                >
                                    <ExtraSectionHeader section={section}/>
                                    <div className="grid gap-6">
                                        {(Object.entries(beatmapSectionLabels) as Array<[BeatmapsetSection, string]>).map(([key, label]) =>
                                            (data.beatmapsets[key]?.length ?? 0) > 0 ? (
                                                <div key={key}>
                                                    <SectionTitle count={data.beatmapsets[key]?.length} title={label}/>
                                                    <div className="grid gap-3">
                                                        {data.beatmapsets[key]?.map((beatmapset) => (
                                                            <BeatmapsetPanel key={`${key}-${beatmapset.id}`}
                                                                             beatmapset={beatmapset}/>
                                                        ))}
                                                    </div>
                                                </div>
                                            ) : null,
                                        )}
                                    </div>
                                </section>
                            );
                        }

                        if (section === "recent_activity") {
                            return (
                                <section
                                    key={section}
                                    className="relative mx-0.5 rounded-2xl bg-osu-b4 px-4.5 py-5 text-[0.95rem] shadow-[0_12px_28px_rgba(0,0,0,0.28)] lg:mx-2.5 lg:px-10"
                                    id={section}
                                >
                                    <ExtraSectionHeader section={section}/>
                                    <ul className="-my-0.5 flex list-none flex-col p-0">
                                        {data.recentActivity?.map((event) => {
                                            const rendered = renderEvent(event);
                                            return (
                                                <li key={event.id}
                                                    className="my-0.5 flex items-baseline justify-between leading-5.5">
                                                    <div className="flex min-w-0 flex-1 items-baseline">
                                                        <div
                                                            className="mr-1.25 inline-flex h-5.5 w-7 flex-none items-center justify-center text-sm">{rendered.icon}</div>
                                                        <div className="mr-5 overflow-hidden text-ellipsis">
                                                            <a className="hover:text-white" href={rendered.url}
                                                               rel="noreferrer" target="_blank">
                                                                {rendered.text}
                                                            </a>
                                                        </div>
                                                    </div>
                                                    <div
                                                        className="whitespace-nowrap text-osu-f1">{formatRelativeTime(event.created_at)}</div>
                                                </li>
                                            );
                                        })}
                                    </ul>
                                </section>
                            );
                        }

                        if (section === "historical") {
                            return (
                                <section
                                    key={section}
                                    className="relative mx-0.5 rounded-2xl bg-osu-b4 px-4.5 py-5 text-[0.95rem] shadow-[0_12px_28px_rgba(0,0,0,0.28)] lg:mx-2.5 lg:px-10"
                                    id={section}
                                >
                                    <ExtraSectionHeader section={section}/>
                                    <div className="grid gap-6 xl:grid-cols-2">
                                        <div>
                                            <SectionTitle title="Monthly Playcounts"/>
                                            <div className="rounded-sm border border-white/8 bg-black/15 p-3">
                                                <ProfileLineChart
                                                    className="h-32 w-full"
                                                    data={monthlyPlaycounts}
                                                    strokeColor="hsl(var(--hsl-h1))"
                                                    valueLabel="Playcount"
                                                />
                                            </div>
                                        </div>
                                        <div>
                                            <SectionTitle title="Replays Watched"/>
                                            <div className="rounded-sm border border-white/8 bg-black/15 p-3">
                                                <ProfileLineChart
                                                    className="h-32 w-full"
                                                    data={replayCounts}
                                                    strokeColor="hsl(var(--hsl-l1))"
                                                    valueLabel="Replays"
                                                />
                                            </div>
                                        </div>
                                    </div>

                                    <div className="mt-6">
                                        {(data.scores.recent?.length ?? 0) > 0 ? (
                                            <ProfileScoreSection
                                                canLoadMore={data.source === "api"}
                                                count={getScoreSectionCount(data, "recent")}
                                                initialScores={data.scores.recent ?? []}
                                                mode={currentMode}
                                                section="recent"
                                                title="Recent Plays"
                                                totalCount={getKnownScoreSectionCount(data, "recent")}
                                                userId={user.id}
                                            />
                                        ) : (
                                            <>
                                                <SectionTitle title="Recent Plays"/>
                                                <div className="rounded-sm border border-white/8 bg-black/15 px-4 py-6 text-center text-sm text-white/55">
                                                    No plays in the last 24 hours.
                                                </div>
                                            </>
                                        )}
                                    </div>
                                </section>
                            );
                        }

                        return (
                            <section
                                key={section}
                                className="relative mx-0.5 rounded-2xl bg-osu-b4 px-4.5 py-5 text-[0.95rem] shadow-[0_12px_28px_rgba(0,0,0,0.28)] lg:mx-2.5 lg:px-10"
                                id={section}
                            >
                                <ExtraSectionHeader section={section}/>
                                <div className="overflow-hidden rounded-sm border border-white/8">
                                    <table className="w-full text-left text-sm">
                                        <thead className="bg-black/20 text-white/55">
                                        <tr>
                                            <th className="px-4 py-3 font-medium">Type</th>
                                            <th className="px-4 py-3 font-medium">When</th>
                                            <th className="px-4 py-3 font-medium">Length</th>
                                        </tr>
                                        </thead>
                                        <tbody>
                                        {user.account_history.map((entry) => (
                                            <tr key={entry.id}
                                                className="border-t border-white/8 bg-black/10 text-white/80">
                                                <td className="px-4 py-3">{entry.description ?? entry.type}</td>
                                                <td className="px-4 py-3">{formatDate(entry.timestamp)}</td>
                                                <td className="px-4 py-3">{entry.permanent ? "Permanent" : `${formatInteger(entry.length)}h`}</td>
                                            </tr>
                                        ))}
                                        </tbody>
                                    </table>
                                </div>
                            </section>
                        );
                    })}
                </div>
            </PageWrapper>
        </main>
    );
}

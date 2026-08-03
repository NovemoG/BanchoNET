import Link from "next/link";
import {ChevronRight, Download} from "lucide-react";
import {BeatmapsetPanel} from "@/components/beatmapset-panels/beatmapset-panel";
import {HomeHeader} from "@/components/home/home-header";
import {PageWrapper} from "@/components/page-wrapper";
import type {HomeBeatmapsets, HomeNewsPost, HomeStats} from "@/lib/home";
import {cn} from "@/lib/utils";
import {OnlineGraph} from "@/components/home/online-graph";

type UserHomePageProps = {
    beatmapsets: HomeBeatmapsets;
    news: HomeNewsPost[];
    stats: HomeStats;
};

const actionLinks = [
    {
        className: "bg-[hsl(200,60%,50%)] shadow-[0_3px_0_hsl(200,60%,38%),0_8px_18px_rgba(0,0,0,0.28)] hover:bg-[hsl(200,60%,58%)]",
        href: "/download",
        icon: Download,
        label: "Download",
    },
];

function formatInteger(value: number) {
    return new Intl.NumberFormat("en-US").format(value);
}

function NewsPreview({post, collapsed = false}: {collapsed?: boolean; post: HomeNewsPost}) {
    const date = new Date(post.publishedAt);
    const day = Number.isNaN(date.getTime())
        ? "--"
        : new Intl.DateTimeFormat("en-US", {day: "2-digit"}).format(date);
    const month = Number.isNaN(date.getTime())
        ? ""
        : new Intl.DateTimeFormat("en-US", {month: "short", year: collapsed ? undefined : "numeric"}).format(date);

    return (
        <article className={cn("mb-1 overflow-hidden bg-osu-b4 shadow-[0_2px_10px_rgba(0,0,0,0.35)]", collapsed && "shadow-none")}>
            {collapsed ? null : (
                <a
                    aria-label={post.title}
                    className="block h-32 bg-osu-b6 bg-cover bg-center"
                    href={post.url}
                    style={post.imageUrl == null ? undefined : {backgroundImage: `url("${post.imageUrl}")`}}
                />
            )}
            <div className="flex">
                <time className={cn("m-2.5 flex w-[70px] flex-none flex-col items-end border-r border-osu-l1 pr-2.5 text-right text-osu-l1", collapsed && "my-0 flex-row items-baseline justify-end gap-1 py-1")}>
                    <span className={cn("text-xl font-extrabold", collapsed && "text-base font-bold")}>{day}</span>
                    <span className={cn("text-[11px]", collapsed && "text-base")}>{month}</span>
                </time>
                <div className={cn("min-w-0 flex-1 pr-3", collapsed && "flex items-center")}>
                    <a className={cn("my-1 block text-base font-bold text-white hover:text-white/80", collapsed && "my-0 truncate")} href={post.url}>
                        {post.title}
                    </a>
                    {collapsed ? null : <p className="mb-3 line-clamp-3 text-sm leading-5 text-osu-c2">{post.preview}</p>}
                </div>
            </div>
        </article>
    );
}

function StatusBox({stats}: {stats: HomeStats}) {
    return (
        <div className="overflow-hidden rounded-md bg-osu-b5 shadow-[0_2px_10px_rgba(0,0,0,0.25)]">
            <div className="grid grid-cols-3 gap-3 p-4 text-center">
                <div>
                    <div className="text-xs font-semibold text-osu-f1">Online Friends</div>
                    <div className="text-xl font-bold text-white">0</div>
                </div>
                <div>
                    <div className="text-xs font-semibold text-osu-f1">Games</div>
                    <div className="text-xl font-bold text-white">{formatInteger(stats.currentGames)}</div>
                </div>
                <div>
                    <div className="text-xs font-semibold text-osu-f1">Online Users</div>
                    <div className="text-xl font-bold text-white">{formatInteger(stats.currentOnline)}</div>
                </div>
            </div>

            <div className="relative">
                <OnlineGraph className="h-32 w-full" data={stats.graphData}/>
            </div>
        </div>
    );
}

function ActionButton({className, href, icon: Icon, label}: (typeof actionLinks)[number]) {
    return (
        <a
            className={cn(
                "flex translate-y-0 items-center rounded-lg bg-center px-5 py-2.5 text-xl font-bold text-white transition active:translate-y-0.5",
                className,
            )}
            href={href}
            style={{backgroundImage: "url('/layout/button.svg')", backgroundSize: "175%"}}
        >
            <span className="min-w-0 flex-1 truncate">{label}</span>
            <Icon aria-hidden className="size-6 flex-none"/>
        </a>
    );
}

function BeatmapsetList({
    beatmapsets,
    href,
    title,
}: {
    beatmapsets: HomeBeatmapsets["newBeatmapsets"];
    href: string;
    title: string;
}) {
    return (
        <>
            <h3 className="m-0 pb-2.5 pt-5 text-base font-bold text-osu-l1">{title}</h3>
            <div className="grid gap-2.5 text-sm">
                {beatmapsets.map((beatmapset) => (
                    <BeatmapsetPanel beatmapset={beatmapset} key={beatmapset.id} size="mini"/>
                ))}
                <Link className="flex items-center justify-center gap-1 rounded-md bg-osu-b5 px-4 py-3 text-sm font-semibold text-osu-l1 hover:bg-osu-b3 hover:text-white" href={href}>
                    see more
                    <ChevronRight aria-hidden className="size-4"/>
                </Link>
            </div>
        </>
    );
}

export function UserHomePage({beatmapsets, news, stats}: UserHomePageProps) {
    const [featuredNews, ...collapsedNews] = news;

    return (
        <>
            <HomeHeader active="dashboard"/>

            <PageWrapper className="mb-10 bg-osu-b5 shadow-[0_12px_32px_rgba(0,0,0,0.24)]">
                <div className="flex flex-col lg:flex-row lg:items-start lg:px-5">
                    {news.length > 0 ? (
                        <section className="w-full flex-none pb-5 lg:mr-2.5 lg:w-[calc(60%-10px)]">
                            <h2 className="m-0 px-5 pb-2.5 pt-5 text-xl font-bold text-osu-l1 lg:px-0">News</h2>
                            {featuredNews != null ? <NewsPreview post={featuredNews}/> : null}
                            {collapsedNews.length > 0 ? (
                                <div className="mb-1 bg-osu-b4 py-1 shadow-[0_2px_10px_rgba(0,0,0,0.25)]">
                                    {collapsedNews.map((post) => (
                                        <NewsPreview collapsed key={post.id} post={post}/>
                                    ))}
                                </div>
                            ) : null}
                        </section>
                    ) : null}

                    <aside
                        className={cn(
                            "w-full bg-osu-b4 p-5 lg:mb-2.5 lg:ml-2.5 lg:px-2.5 lg:shadow-[0_2px_10px_rgba(0,0,0,0.25)]",
                            news.length > 0 && "lg:w-[calc(40%-10px)]",
                        )}
                    >
                        <div className="pb-5">
                            <StatusBox stats={stats}/>
                        </div>

                        <div className="grid gap-2.5">
                            {actionLinks.map((link) => (
                                <ActionButton {...link} key={link.label}/>
                            ))}
                        </div>

                        <BeatmapsetList
                            beatmapsets={beatmapsets.newBeatmapsets}
                            href="/beatmapsets?sort=ranked_desc&s=ranked"
                            title="New Ranked Beatmaps"
                        />
                        <BeatmapsetList
                            beatmapsets={beatmapsets.popularBeatmapsets}
                            href="/beatmapsets?sort=favourites_desc&s=ranked"
                            title="Popular Beatmaps"
                        />
                    </aside>
                </div>
            </PageWrapper>
        </>
    );
}

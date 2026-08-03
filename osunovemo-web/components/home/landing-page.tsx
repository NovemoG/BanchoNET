import Link from "next/link";
import {Download} from "lucide-react";
import {LandingLogin} from "@/components/home/landing-login";
import type {HomeNewsPost, HomeStats} from "@/lib/home";
import {OnlineGraph} from "@/components/home/online-graph";
import {landingVideoUrl} from "@/lib/home";
import {cn} from "@/lib/utils";

type LandingPageProps = {
    news: HomeNewsPost[];
    stats: HomeStats;
};

const navLinks = [
    {href: "/", label: "home"},
    {href: "/beatmapsets", label: "beatmaps"},
    {href: "/rankings/osu/global/performance", label: "rankings"},
    {href: "/community/chat", label: "community"},
];


function formatInteger(value: number) {
    return new Intl.NumberFormat("en-US").format(value);
}

function getNewsDateParts(value: string) {
    const date = new Date(value);

    if (Number.isNaN(date.getTime())) {
        return {day: "--", monthYear: ""};
    }

    return {
        day: new Intl.DateTimeFormat("en-US", {day: "2-digit"}).format(date),
        monthYear: new Intl.DateTimeFormat("en-US", {month: "short", year: "numeric"}).format(date),
    };
}


function LandingNewsCard({post, compact = false}: {compact?: boolean; post: HomeNewsPost}) {
    const date = getNewsDateParts(post.publishedAt);

    return (
        <article
            className={cn(
                "overflow-hidden bg-osu-b4 shadow-[0_2px_10px_rgba(0,0,0,0.35)]",
                compact && "shadow-none",
            )}
        >
            {compact ? null : (
                <a
                    aria-label={post.title}
                    className="block h-32 bg-osu-b6 bg-cover bg-center"
                    href={post.url}
                    style={post.imageUrl == null ? undefined : {backgroundImage: `url("${post.imageUrl}")`}}
                />
            )}

            <div className="flex bg-osu-b4">
                <time
                    className={cn(
                        "m-2.5 flex w-[70px] flex-none flex-col items-end border-r border-osu-l1 pr-2.5 text-right text-osu-l1",
                        compact && "my-0 flex-row items-baseline justify-end gap-1 py-1",
                    )}
                    dateTime={post.publishedAt}
                >
                    <span className={cn("text-xl font-extrabold", compact && "text-base font-bold")}>{date.day}</span>
                    <span className={cn("text-[11px]", compact && "text-base")}>{date.monthYear}</span>
                </time>

                <div className={cn("min-w-0 flex-1 pr-3", compact && "flex items-center")}>
                    <a
                        className={cn(
                            "my-1 block text-base font-bold text-white transition hover:text-white/80",
                            compact && "my-0 truncate",
                        )}
                        href={post.url}
                    >
                        {post.title}
                    </a>
                    {compact ? null : (
                        <p className="mb-3 line-clamp-3 text-sm leading-5 text-osu-c2">{post.preview}</p>
                    )}
                </div>
            </div>
        </article>
    );
}

function LandingNews({posts}: {posts: HomeNewsPost[]}) {
    if (posts.length === 0) {
        return null;
    }

    const [featured, ...rest] = posts;

    return (
        <section className="mx-auto flex w-[calc(100%-20px)] max-w-[1200px] flex-col gap-2.5 py-2.5 lg:w-[calc(100%-100px)]">
            <div className="grid gap-2.5 lg:grid-cols-2">
                <LandingNewsCard post={featured}/>
                <div className="flex flex-col">
                    {rest.map((post) => (
                        <LandingNewsCard compact key={post.id} post={post}/>
                    ))}
                </div>
            </div>
            <div className="flex justify-center p-2.5">
                <a
                    className="rounded-full bg-osu-b4 px-5 py-2 text-sm font-semibold lowercase text-osu-l1 transition hover:bg-osu-b3 hover:text-white"
                    href="https://osu.ppy.sh/home/news"
                >
                    see more news
                </a>
            </div>
        </section>
    );
}

export function LandingPage({news, stats}: LandingPageProps) {
    return (
        <div className="flex-1 bg-osu-b6 text-white">
            <nav className="mx-auto flex w-[calc(100%-20px)] max-w-[1200px] items-center justify-between px-5 pb-5 pt-7 text-sm lg:w-[calc(100%-100px)]">
                <div className="flex flex-wrap gap-x-6 gap-y-2">
                    {navLinks.map((link) => (
                        <Link
                            className={cn(
                                "whitespace-nowrap lowercase text-white transition-colors hover:text-osu-h1",
                                link.href === "/" && "font-bold",
                            )}
                            href={link.href}
                            key={link.label}
                        >
                            {link.label}
                        </Link>
                    ))}
                </div>
                <LandingLogin/>
            </nav>

            <section className="mx-auto w-[calc(100%-20px)] max-w-[1200px] lg:w-[calc(100%-100px)]">
                <div className="relative flex h-[450px] items-center justify-end overflow-hidden bg-[#333] shadow-[0_12px_32px_rgba(0,0,0,0.35)]">
                    <div className="absolute inset-0">
                        <video
                            autoPlay
                            className="size-full object-cover"
                            loop
                            muted
                            playsInline
                            src={landingVideoUrl}
                        />
                        <div className="absolute inset-0 bg-osu-b6/50"/>
                    </div>

                    <p className="absolute left-0 top-0 w-full px-[50px] py-7 text-center text-sm [text-shadow:0_1px_3px_rgba(0,0,0,0.65)] md:text-right">
                        {stats.hasLiveData ? (
                            <>
                                <strong>{formatInteger(stats.totalUsers)}</strong> registered players,{" "}
                                <strong>{formatInteger(stats.currentOnline)}</strong> currently online in{" "}
                                <strong>{formatInteger(stats.currentGames)}</strong> games
                            </>
                        ) : (
                            <>
players online around the world
                            </>
                        )}
                    </p>

                    <div className="relative w-full px-[50px] text-center md:text-right">
                        <h1 className="m-0 text-[32px] font-bold leading-tight [text-shadow:0_1px_3px_rgba(0,0,0,0.65)]">
                            the bestest free-to-win rhythm game
                        </h1>
                        <h2 className="m-0 text-[20px] font-semibold leading-tight text-osu-h1 [text-shadow:0_1px_3px_rgba(0,0,0,0.65)]">
                            rhythm is just a click away
                        </h2>

                        <div className="relative h-0">
                            <div className="absolute top-full mt-5 flex w-full justify-center md:justify-end">
                                <a
                                    className="inline-flex items-center gap-5 rounded-md bg-osu-h2 px-5 py-2.5 text-2xl font-semibold text-white shadow-[0_12px_32px_rgba(0,0,0,0.35)] transition hover:bg-osu-h1"
                                    href="/download"
                                >
                                    <span>Download now</span>
                                    <span className="flex size-10 items-center justify-center rounded-full border-2 border-white shadow-[0_0_0_4px_rgba(0,0,0,0.2)]">
                                        <Download aria-hidden className="size-5"/>
                                    </span>
                                </a>
                            </div>
                        </div>
                    </div>

                    <OnlineGraph className="absolute inset-x-0 bottom-0 h-[90px] w-full" data={stats.graphData}/>
                </div>
            </section>

            <LandingNews posts={news}/>

        </div>
    );
}

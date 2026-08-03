import type {Metadata} from "next";
import {LandingPage} from "@/components/home/landing-page";
import {UserHomePage} from "@/components/home/user-home-page";
import {auth} from "@/lib/auth";
import {fetchHomeBeatmapsets, fetchHomeNews, fetchHomeStats} from "@/lib/home";

export const metadata: Metadata = {
    description: "osu! - Rhythm is just a click away!",
    title: "welcome | cossu!",
};

export default async function HomePage() {
    const [session, stats, news] = await Promise.all([
        auth(),
        fetchHomeStats(),
        fetchHomeNews(),
    ]);

    if (session != null) {
        const beatmapsets = await fetchHomeBeatmapsets();

        return (
            <UserHomePage
                beatmapsets={beatmapsets}
                news={news}
                stats={stats}
            />
        );
    }

    return <LandingPage news={news} stats={stats}/>;
}

import type {Metadata} from "next";
import localFont from "next/font/local";
import {Geist_Mono} from "next/font/google";
import "./globals.css";
import {AppShell} from "@/components/app-shell";
import {SessionProvider} from "@/components/auth/session-provider";
import {MiniPlayer} from "@/components/audio/mini-player";
import {MiniPlayerProvider} from "@/components/audio/mini-player-provider";
import {RealtimeProvider} from "@/components/realtime/realtime-provider";
import {SwrProvider} from "@/components/swr-provider";
import {TooltipProvider} from "@/components/ui/tooltip";
import {auth} from "@/lib/auth";
import {cn} from "@/lib/utils";

const torus = localFont({
    src: [
        {path: "./fonts/torus/Torus-Thin.otf", weight: "100", style: "normal"},
        {path: "./fonts/torus/Torus-Light.otf", weight: "300", style: "normal"},
        {path: "./fonts/torus/Torus-Regular.otf", weight: "400", style: "normal"},
        {path: "./fonts/torus/Torus-SemiBold.otf", weight: "600", style: "normal"},
        {path: "./fonts/torus/Torus-SemiBold.otf", weight: "700", style: "normal"},
        {path: "./fonts/torus/Torus-Bold.otf", weight: "800", style: "normal"},
        {path: "./fonts/torus/Torus-Heavy.otf", weight: "900", style: "normal"},
    ],
    variable: "--font-torus",
});

const geistMono = Geist_Mono({
    subsets: ["latin"],
    variable: "--font-geist-mono",
});

export const metadata: Metadata = {
    description: "osu-web rankings components ported to Next.js",
    title: "cossu! rankings",
};

export default async function RootLayout({
                                            children,
                                        }: Readonly<{
    children: React.ReactNode;
}>) {
    const initialSession = await auth();

    return (
        <html
            lang="en"
            className={cn("dark h-full antialiased", torus.variable, geistMono.variable)}
        >
        <body className={cn("min-h-full flex flex-col font-sans", torus.className)}>
        <SessionProvider
            key={initialSession == null ? "guest" : `user-${initialSession.user.id}`}
            initialSession={initialSession}
        >
            <SwrProvider>
                <TooltipProvider>
                    <RealtimeProvider>
                        <MiniPlayerProvider>
                            <AppShell>{children}</AppShell>
                            <MiniPlayer/>
                        </MiniPlayerProvider>
                    </RealtimeProvider>
                </TooltipProvider>
            </SwrProvider>
        </SessionProvider>
        </body>
        </html>
    );
}

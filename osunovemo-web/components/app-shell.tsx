"use client";

import type {ReactNode} from "react";
import {usePathname} from "next/navigation";
import {useSession} from "@/components/auth/session-provider";
import {ChatOverlay} from "@/components/chat/chat-overlay";
import {SiteFooter} from "@/components/site-footer";
import {TopHeader} from "@/components/top-header";
import {cn} from "@/lib/utils";

type AppShellProps = {
    children: ReactNode;
};

export function AppShell({children}: AppShellProps) {
    const pathname = usePathname();
    const {status} = useSession();
    const isGuestLanding = pathname === "/" && status !== "authenticated";
    const showChatOverlay = status === "authenticated" && !isGuestLanding && !pathname.startsWith("/community/chat");

    return (
        // min-h-screen column with a growing main, so the footer sits at the bottom of the
        // viewport on a short page instead of floating up under the content.
        <div className="flex min-h-screen flex-col">
            {isGuestLanding ? null : <TopHeader/>}
            <main className={cn("flex flex-1 flex-col", isGuestLanding && "min-h-full")}>
                {children}
            </main>
            <SiteFooter/>
            {showChatOverlay ? <ChatOverlay/> : null}
        </div>
    );
}

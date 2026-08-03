import type {ReactNode} from "react";
import Link from "next/link";
import {ChevronDown, Home, Settings, UsersRound} from "lucide-react";
import {Header} from "@/components/header";
import {cn} from "@/lib/utils";

export type HomeHeaderSection = "dashboard" | "friends" | "settings" | "watchlists";

type HomeHeaderLink = {
    active: boolean;
    href: string;
    label: HomeHeaderSection;
};

type HomeHeaderProps = {
    active: HomeHeaderSection;
    backgroundImageUrl?: string | null;
};

const linkClassName =
    "relative z-1 flex items-baseline gap-1.25 py-2.5 text-osu-c1 transition-colors before:absolute before:-bottom-0.5 before:left-0 before:hidden before:h-1.25 before:w-full before:scale-y-0 before:rounded-full before:bg-osu-h1 before:transition-transform hover:text-white md:py-4 md:before:block hover:before:scale-y-100";

const mobileSummaryClassName =
    "relative block w-max max-w-full list-none py-2.5 text-white marker:hidden before:absolute before:-bottom-0.5 before:left-0 before:block before:h-1 before:w-full before:rounded-full before:bg-osu-h1 before:content-['']";

const mobileLinkClassName =
    "flex px-4 py-3 text-white/70 transition-colors hover:bg-white/5 hover:text-white sm:px-6 lg:px-8";

function isExternalHref(href: string) {
    return href.startsWith("http://") || href.startsWith("https://");
}

function getHeaderLinks(active: HomeHeaderSection): HomeHeaderLink[] {
    return [
        {active: active === "dashboard", href: "/", label: "dashboard"},
        {active: active === "friends", href: "/friends", label: "friends"},
        {active: active === "settings", href: "/home/account/edit", label: "settings"},
    ];
}

function NavItemLink({
                         children,
                         className,
                         href,
                     }: {
    children: ReactNode;
    className?: string;
    href: string;
}) {
    if (isExternalHref(href)) {
        return (
            <a className={className} href={href}>
                {children}
            </a>
        );
    }

    return (
        <Link className={className} href={href}>
            {children}
        </Link>
    );
}

function HeaderNav({active}: { active: HomeHeaderSection }) {
    const links = getHeaderLinks(active);
    const activeLink = links.find((link) => link.active) ?? links[0];

    return (
        <>
            <ul className="relative hidden items-center gap-5 text-xs md:flex md:text-sm before:absolute before:bottom-0 before:left-0 before:right-0 before:h-px before:bg-osu-h1">
                {links.map((link) => (
                    <li className="relative flex" key={link.label}>
                        <NavItemLink
                            className={cn(linkClassName, link.active && "font-semibold text-white before:scale-y-100")}
                            href={link.href}
                        >
                            <span>{link.label}</span>
                        </NavItemLink>
                    </li>
                ))}
            </ul>

            <details className="group relative w-full text-xs md:hidden">
                <summary className={mobileSummaryClassName}>
                    {activeLink.label}
                    <span
                        className="absolute top-0 left-full flex h-full items-center pl-2.5 text-[0.8em] transition-transform group-open:rotate-180">
                        <ChevronDown className="size-4"/>
                    </span>
                </summary>
                <ul className="absolute inset-x-0 top-full z-101 -mx-4 hidden list-none bg-osu-d5 p-0 group-open:grid sm:-mx-6 lg:-mx-8">
                    {links.map((link) => (
                        <li key={link.label}>
                            <NavItemLink
                                className={cn(mobileLinkClassName, link.active && "bg-white/8 text-white")}
                                href={link.href}
                            >
                                {link.label}
                            </NavItemLink>
                        </li>
                    ))}
                </ul>
            </details>
        </>
    );
}

function HeaderBackground({backgroundImageUrl}: { backgroundImageUrl?: string | null }) {
    if (backgroundImageUrl != null) {
        return (
            <div className="absolute inset-0 overflow-hidden bg-osu-d5">
                <div
                    className="absolute -inset-10 scale-110 bg-cover bg-center opacity-30 blur-[50px]"
                    style={{backgroundImage: `url("${backgroundImageUrl}")`}}
                />
                <div
                    className="absolute inset-0 bg-[linear-gradient(to_right,hsl(var(--hsl-d5)),transparent_18%,transparent_82%,hsl(var(--hsl-d5)))]"/>
            </div>
        );
    }

    return (
        <div className="absolute inset-0 bg-osu-b4">
            <div className="absolute inset-0 bg-[url('/layout/nav2-background-hue0.png')] bg-cover bg-center opacity-70"/>
            <div className="absolute inset-0 bg-linear-to-b from-osu-b6/10 to-osu-b6/80"/>
        </div>
    );
}

function HeaderIcon({active}: { active: HomeHeaderSection }) {
    if (active === "friends") {
        return <UsersRound aria-hidden className="size-5 text-white"/>;
    }

    if (active === "settings") {
        return <Settings aria-hidden className="size-5 text-white"/>;
    }

    return <Home aria-hidden className="size-5 text-white"/>;
}

export function HomeHeader({active, backgroundImageUrl}: HomeHeaderProps) {
    return (
        <Header
            background={<HeaderBackground backgroundImageUrl={backgroundImageUrl}/>}
            bottom={<HeaderNav active={active}/>}
            bottomClassName="relative bg-osu-d4/95"
            icon={
                <div className="flex w-10 flex-none items-center justify-center self-stretch">
                    <HeaderIcon active={active}/>
                </div>
            }
            title="dashboard"
            topClassName="bg-osu-d5/92 text-osu-c1"
        />
    );
}

import Link from "next/link";
import {ChevronDown, Star, UserRound} from "lucide-react";
import {Header as PageHeader} from "@/components/header";
import {resolveAssetUrl} from "@/lib/osu-api-common";
import {cn} from "@/lib/utils";
import type {ProfileUser} from "@/lib/profile";
import {rankingModes, type Ruleset} from "@/lib/rankings";

type HeaderProps = {
    currentMode: Ruleset;
    user: ProfileUser;
};

type HeaderLink = {
    active: boolean;
    href?: string;
    label: string;
};

const modeLabels: Record<Ruleset, string> = {
    fruits: "fruits",
    mania: "mania",
    osu: "osu!",
    taiko: "taiko",
};

function profileHref(userId: number | string, mode?: Ruleset) {
    return mode == null
        ? `/users/${encodeURIComponent(String(userId))}`
        : `/users/${encodeURIComponent(String(userId))}/${mode}`;
}

function resolveProfileAsset(src: string) {
    if (src.startsWith("local:/")) {
        return src.replace("local:", "");
    }

    return resolveAssetUrl(src);
}

function profileHeaderLinks(user: ProfileUser): HeaderLink[] {
    const links: HeaderLink[] = [
        {
            active: true,
            href: profileHref(user.id, user.playmode as Ruleset),
            label: "info",
        },
    ];

    if (!user.is_bot) {
        links.push(
            {active: false, label: "modding"},
            {active: false, label: "playlists"},
            {active: false, label: "multiplayer"},
            {active: false, label: "ranked play"},
        );
    }

    return links;
}

function HeaderNav({links}: { links: HeaderLink[] }) {
    const activeLink = links.find((link) => link.active) ?? links[0];

    return (
        <>
            <ul className="relative hidden items-center gap-5 text-xs md:flex md:text-sm before:absolute before:bottom-0 before:left-0 before:right-0 before:h-px before:bg-osu-h1">
                {links.map((link) => (
                    <li key={link.label} className="relative flex">
                        {link.href ? (
                            <Link
                                className={cn(
                                    "relative z-1 flex items-baseline gap-1.25 py-2.5 text-osu-c1 transition-colors before:absolute before:-bottom-0.5 before:left-0 before:hidden before:h-1.25 before:w-full before:scale-y-0 before:rounded-full before:bg-osu-h1 before:transition-transform hover:text-white md:py-4 md:before:block hover:before:scale-y-100",
                                    link.active && "font-semibold text-white before:scale-y-100",
                                )}
                                href={link.href}
                            >
                                <span>{link.label}</span>
                            </Link>
                        ) : (
                            <span className="relative z-1 flex items-baseline gap-1.25 py-2.5 text-osu-c1/45 md:py-4">
                <span>{link.label}</span>
              </span>
                        )}
                    </li>
                ))}
            </ul>

            <details className="group relative w-full text-xs md:hidden">
                <summary
                    className={"relative block w-max max-w-full list-none py-2.5 text-white marker:hidden before:absolute before:-bottom-0.5 before:left-0 before:block before:h-1 before:w-full before:rounded-full before:bg-osu-h1 before:content-['']"}>
                    {activeLink.label}
                    <span
                        className="absolute top-0 left-full flex h-full items-center pl-2.5 text-[0.8em] transition-transform group-open:rotate-180">
            <ChevronDown className="h-4 w-4"/>
          </span>
                </summary>
                <ul className={"absolute inset-x-0 top-full z-101 -mx-4 hidden list-none bg-osu-d5 p-0 group-open:grid sm:-mx-6 lg:-mx-8"}>
                    {links.map((link) => (
                        <li key={link.label}>
                            {link.href ? (
                                <Link
                                    className={cn(
                                        "block px-4 py-2.5 text-white transition-colors hover:bg-osu-d3 sm:px-6 lg:px-8",
                                        link.active && "bg-osu-d4 font-semibold",
                                    )}
                                    href={link.href}
                                >
                                    {link.label}
                                </Link>
                            ) : (
                                <span className="block cursor-default px-4 py-2.5 text-white/45 sm:px-6 lg:px-8">
                  {link.label}
                </span>
                            )}
                        </li>
                    ))}
                </ul>
            </details>
        </>
    );
}

function GameModeSwitcher({
                              currentMode,
                              primaryMode,
                              userId,
                          }: {
    currentMode: Ruleset;
    primaryMode: Ruleset;
    userId: number;
}) {
    return (
        <ul className="absolute top-0 right-4 flex h-full items-center gap-x-5 gap-y-2.5 text-sm leading-normal sm:right-[50px]">
            {rankingModes.map((mode) => (
                <li key={mode}>
                    <Link
                        className={cn(
                            "group flex items-center gap-1 p-0 text-[20px] [text-shadow:0_1px_2px_rgba(0,0,0,0.45)]",
                            mode === currentMode && "font-bold",
                        )}
                        href={profileHref(userId, mode)}
                        title={modeLabels[mode]}
                    >
                        <span
                            className={cn(
                                `fa-extra-mode-${mode}`,
                                "text-osu-h1 transition-colors group-hover:text-white",
                                mode === currentMode && "text-white",
                            )}
                        />
                        {mode === primaryMode ? (
                            <Star
                                aria-hidden="true"
                                className={cn(
                                    "size-3 fill-current text-osu-h1 transition-colors group-hover:text-white",
                                    mode === currentMode && "text-white",
                                )}
                            />
                        ) : null}
                        <span className="sr-only">{modeLabels[mode]}</span>
                    </Link>
                </li>
            ))}
        </ul>
    );
}

export function Header({currentMode, user}: HeaderProps) {
    const backgroundUrl = resolveProfileAsset(user.cover.url ?? user.avatar_url);

    return (
        <PageHeader
            backgroundWrapperClassName="max-w-[1400px]"
            background={(
                <div className="absolute inset-0 overflow-hidden bg-osu-d5">
                    <div
                        className="absolute -inset-10 scale-110 opacity-25 blur-[50px]"
                        style={{
                            backgroundImage: `url('${backgroundUrl}')`,
                            backgroundPosition: "center",
                            backgroundSize: "cover",
                        }}
                    />
                    <div
                        className="absolute inset-0 bg-[linear-gradient(to_right,hsl(var(--hsl-d5)),transparent_18%,transparent_82%,hsl(var(--hsl-d5)))]"/>
                </div>
            )}
            icon={(
                <div className="flex w-10 flex-none items-center justify-center self-stretch">
                    <UserRound className="h-5 w-5 text-white"/>
                </div>
            )}
            title="profile"
            topClassName="bg-osu-d5/92 text-osu-c1"
            bottom={(
                <>
                    <HeaderNav links={profileHeaderLinks(user)}/>
                    {!user.is_bot ? <GameModeSwitcher currentMode={currentMode} primaryMode={user.playmode}
                                                      userId={user.id}/> : null}
                </>
            )}
            bottomClassName="relative bg-osu-d4/95"
        />
    );
}

"use client";

import type {Dispatch, FC, FocusEvent as ReactFocusEvent, FormEvent, ReactNode, SetStateAction} from "react";
import {useCallback, useEffect, useId, useMemo, useState} from "react";
import Link from "next/link";
import {
    ChevronDown,
    ChevronRight,
    LogOut,
    Network,
    Search,
    UserRound,
} from "lucide-react";
import {usePathname} from "next/navigation";
import {signOut} from "@/app/actions/auth";
import {useSession} from "@/components/auth/session-provider";
import {PageWrapper} from "@/components/page-wrapper";
import {RealtimeNavButtons} from "@/components/realtime/realtime-nav-buttons";
import {Field, FieldError, FieldGroup, FieldLabel} from "@/components/ui/field";
import {Popover, PopoverContent, PopoverTrigger} from "@/components/ui/popover";
import {cn} from "@/lib/utils";
import {signIn as clientSignIn} from "@/lib/auth/client";
import type {AuthActionState} from "@/lib/auth/types";

type NavItem = {
    href: string;
    label: string;
};

type NavSection = {
    href: string;
    id: string;
    items: NavItem[];
    label: string;
    matches: (pathname: string) => boolean;
};

type Locale = {
    badge: string;
    code: string;
    label: string;
};

const navSections: NavSection[] = [
    // Entries that pointed at osu!'s own news, wiki, forums, store, changelog, team page,
    // featured artists, packs, contests, tournaments and livestreams are gone: those are their
    // pages, not ours, and presenting them as our nav was misleading. Add local equivalents here
    // as they are built.
    {
        href: "/",
        id: "home",
        items: [
            {href: "/download", label: "download"},
            {href: "/home/search", label: "search"},
            {href: "/friends", label: "friends"},
        ],
        label: "home",
        matches: (pathname) => pathname === "/" || pathname === "/friends" || pathname.startsWith("/home"),
    },
    {
        href: "/beatmapsets",
        id: "beatmaps",
        items: [
            {href: "/beatmapsets", label: "listing"},
        ],
        label: "beatmaps",
        matches: (pathname) => pathname.startsWith("/beatmaps") || pathname.startsWith("/beatmapsets"),
    },
    {
        href: "/rankings/osu/global/performance",
        id: "rankings",
        items: [
            {href: "/rankings/osu/global/performance", label: "performance"},
            {href: "/rankings/osu/global/score", label: "score"},
            {href: "/rankings/osu/country/performance", label: "country"},
            {href: "/rankings/top-plays/osu", label: "top plays"},
            {href: "/rankings/osu/team/performance", label: "team"},
        ],
        label: "rankings",
        matches: (pathname) => pathname.startsWith("/rankings"),
    },
    {
        href: "/community/chat",
        id: "community",
        items: [
            {href: "/community/chat", label: "chat"},
        ],
        label: "community",
        matches: (pathname) => pathname.startsWith("/community"),
    },
];

const locales: Locale[] = [
    {badge: "EN", code: "en", label: "English"},
    {badge: "PL", code: "pl", label: "Polski"},
    {badge: "DE", code: "de", label: "Deutsch"},
    {badge: "FR", code: "fr", label: "Francais"},
    {badge: "JP", code: "ja", label: "Japanese"},
    {badge: "KR", code: "ko", label: "Korean"},
];

function isExternalHref(href: string) {
    return href.startsWith("http://") || href.startsWith("https://");
}

function getFallbackMobileLabel(pathname: string) {
    if (pathname.startsWith("/users")) {
        return "profile";
    }

    if (pathname.startsWith("/rankings")) {
        return "rankings";
    }

    return "cossu!";
}

interface NavLinkProps {
    ariaCurrent?: "page";
    children: ReactNode;
    className?: string;
    href: string;
    title?: string;
}

const NavLink: FC<NavLinkProps> = ({ariaCurrent, children, className, href, title}) => {
    if (isExternalHref(href)) {
        return (
            <a aria-current={ariaCurrent} className={className} href={href} title={title}>
                {children}
            </a>
        );
    }

    return (
        <Link aria-current={ariaCurrent} className={className} href={href} title={title}>
            {children}
        </Link>
    );
};

// A wordmark rather than the osu! logo mask that used to sit here.
const Logo = ({pinned}: { pinned: boolean }) => (
    <Link
        aria-label="home"
        className={cn(
            "flex items-center font-bold whitespace-nowrap text-white transition-colors duration-100 hover:text-osu-h1",
            pinned ? "text-lg" : "text-2xl",
        )}
        href="/"
    >
        novemo
    </Link>
);

interface DesktopDropdownProps {
    children: ReactNode;
    menuId: string;
    onClose: () => void;
    onOpen: () => void;
    openMenuId: string | null;
    popupAlign?: "center" | "left";
    trigger: ReactNode;
}

const DesktopDropdown: FC<DesktopDropdownProps> = ({
                                                       children,
                                                       menuId,
                                                       onClose,
                                                       onOpen,
                                                       openMenuId,
                                                       popupAlign = "left",
                                                       trigger,
                                                   }) => {
    const open = openMenuId === menuId;

    const handleBlur = (event: ReactFocusEvent<HTMLDivElement>) => {
        if (event.relatedTarget instanceof Node && event.currentTarget.contains(event.relatedTarget)) {
            return;
        }

        onClose();
    };

    return (
        <div
            className="relative flex h-full items-stretch"
            onBlurCapture={handleBlur}
            onFocusCapture={onOpen}
            onMouseEnter={onOpen}
            onMouseLeave={onClose}
        >
            {trigger}

            <div
                className={cn(
                    "pointer-events-none absolute top-full z-[60] flex flex-col whitespace-nowrap transition-[padding-top] duration-200",
                    popupAlign === "center" ? "left-1/2 -translate-x-1/2" : "-left-3",
                    "pt-0",
                )}
            >
                <div
                    className={cn(
                        "pointer-events-none translate-y-1.5 opacity-0 transition-all duration-150",
                        open && "pointer-events-auto translate-y-0 opacity-100",
                    )}
                >
                    {children}
                </div>
            </div>
        </div>
    );
};

const initialAuthActionState: AuthActionState = {
    status: "idle",
};

const loginInputClassName =
    "min-h-8 rounded-md border-0 bg-osu-b5 px-3 py-2 text-sm text-white shadow-[inset_0_0_0_1px_rgba(255,255,255,0.08)] outline-none transition-shadow placeholder:text-osu-f1 focus-visible:shadow-[inset_0_0_0_1px_hsl(var(--hsl-h1))]";

function LoginPanel({
                        callbackUrl,
                        className,
                    }: {
    callbackUrl: string;
    className?: string;
}) {
    const usernameId = useId();
    const passwordId = useId();
    const [state, setState] = useState<AuthActionState>(initialAuthActionState);
    const [pending, setPending] = useState(false);

    const handleSubmit = async (event: FormEvent<HTMLFormElement>) => {
        event.preventDefault();

        const formData = new FormData(event.currentTarget);
        const username = String(formData.get("username") ?? "").trim();
        const password = String(formData.get("password") ?? "");
        const fieldErrors: AuthActionState["fieldErrors"] = {};

        if (username.length === 0) {
            fieldErrors.username = "Username is required.";
        }

        if (password.length === 0) {
            fieldErrors.password = "Password is required.";
        }

        if (Object.keys(fieldErrors).length > 0) {
            setState({fieldErrors, status: "error"});
            return;
        }

        setPending(true);
        const result = await clientSignIn("credentials", {
            callbackUrl,
            password,
            redirect: false,
            username,
        });

        if (result.ok) {
            window.location.assign(callbackUrl);
            return;
        }

        setState({message: result.error ?? "Sign in failed.", status: "error"});
        setPending(false);
    };

    return (
        <form
            className={cn("w-72 rounded-lg bg-osu-b6 p-4 text-osu-c1 shadow-[0_12px_32px_rgba(0,0,0,0.32)]", className)}
            onSubmit={handleSubmit}
        >
            <input name="callbackUrl" type="hidden" value={callbackUrl}/>
            <FieldGroup className="gap-3">
                <Field data-invalid={Boolean(state.fieldErrors?.username)}>
                    <FieldLabel htmlFor={usernameId}>username</FieldLabel>
                    <input
                        aria-invalid={Boolean(state.fieldErrors?.username)}
                        autoComplete="username"
                        className={loginInputClassName}
                        disabled={pending}
                        id={usernameId}
                        name="username"
                        required
                    />
                    <FieldError>{state.fieldErrors?.username}</FieldError>
                </Field>

                <Field data-invalid={Boolean(state.fieldErrors?.password)}>
                    <FieldLabel htmlFor={passwordId}>password</FieldLabel>
                    <input
                        aria-invalid={Boolean(state.fieldErrors?.password)}
                        autoComplete="current-password"
                        className={loginInputClassName}
                        disabled={pending}
                        id={passwordId}
                        name="password"
                        required
                        type="password"
                    />
                    <FieldError>{state.fieldErrors?.password}</FieldError>
                </Field>

                {state.message ? (
                    <div className="rounded-md bg-osu-red-3/20 px-3 py-2 text-xs text-osu-red-2" role="alert">
                        {state.message}
                    </div>
                ) : null}

                <button
                    className="min-h-8 rounded-md bg-osu-h2 px-3 py-2 text-sm font-semibold text-osu-c1 transition-colors hover:bg-osu-h1 disabled:bg-osu-b3 disabled:text-osu-f1"
                    disabled={pending}
                    type="submit"
                >
                    {pending ? "signing in..." : "sign in"}
                </button>
            </FieldGroup>
        </form>
    );
}

function getAvatarUrl(user: { avatar_url?: unknown }) {
    return typeof user.avatar_url === "string" && user.avatar_url.length > 0
        ? user.avatar_url
        : null;
}

function ProfileMenuItem({
                             children,
                             href,
                         }: {
    children: ReactNode;
    href: string;
}) {
    return (
        <NavLink
            className="relative flex rounded-md px-[25px] py-[5px] text-[13px] text-osu-c1 no-underline transition-none hover:bg-osu-b5 hover:text-white before:absolute before:left-2.5 before:top-[7px] before:h-[calc(100%-14px)] before:w-[3px] before:rounded-full before:bg-osu-h1 before:opacity-0 hover:before:opacity-100"
            href={href}
        >
            {children}
        </NavLink>
    );
}

function ProfileMenu({
                         avatarUrl,
                         callbackUrl,
                         user,
                     }: {
    avatarUrl: string | null;
    callbackUrl: string;
    user: { id: number; username: string };
}) {
    const profileUrl = `/users/${user.id}`;

    return (
        <div className="flex min-w-[190px] flex-none flex-col rounded-lg bg-osu-b6 px-[5px] py-2.5 text-[13px] text-osu-c1 shadow-[0_12px_32px_rgba(0,0,0,0.32)]">
            <Link
                className="relative -mx-[5px] -mt-2.5 mb-2.5 flex min-h-[100px] flex-col items-center overflow-hidden rounded-t-lg bg-osu-b5 bg-cover bg-center p-5 text-white no-underline"
                href={profileUrl}
                style={avatarUrl == null ? undefined : {backgroundImage: `url(${JSON.stringify(avatarUrl)})`}}
            >
                <span className="absolute inset-0 bg-black/50" />
                <UserRound className="relative my-2.5 h-5 w-5" />
                <span className="relative max-w-44 truncate text-sm font-semibold">{user.username}</span>
            </Link>

            <ProfileMenuItem href={profileUrl}>profile</ProfileMenuItem>
            <ProfileMenuItem href="/friends">friends</ProfileMenuItem>
            <ProfileMenuItem href="/home/account/edit">account settings</ProfileMenuItem>

            <form action={signOut}>
                <input name="callbackUrl" type="hidden" value={callbackUrl}/>
                <button
                    className="relative flex w-full rounded-md px-[25px] py-[5px] text-left text-[13px] text-osu-c1 transition-none hover:bg-osu-b5 hover:text-white before:absolute before:left-2.5 before:top-[7px] before:h-[calc(100%-14px)] before:w-[3px] before:rounded-full before:bg-osu-h1 before:opacity-0 hover:before:opacity-100"
                    type="submit"
                >
                    sign out
                </button>
            </form>
        </div>
    );
}

function AuthenticatedUserMenu({
                                   callbackUrl,
                                   pinned,
                               }: {
    callbackUrl: string;
    pinned: boolean;
}) {
    const {data} = useSession();
    const [open, setOpen] = useState(false);

    if (data == null) {
        return null;
    }

    const avatarUrl = getAvatarUrl(data.user);

    return (
        <div className="flex items-center gap-2">
            <RealtimeNavButtons variant="desktop"/>

            <Popover onOpenChange={setOpen} open={open}>
                <PopoverTrigger asChild>
                    <button
                        aria-label={data.user.username}
                        className={cn(
                            "relative shrink-0 overflow-hidden rounded-full bg-osu-b4 bg-cover bg-center text-osu-c1 transition-[width,height,box-shadow] duration-200 hover:shadow-[inset_0_0_0_3px_hsl(var(--hsl-c1))] aria-expanded:shadow-[inset_0_0_0_3px_hsl(var(--hsl-c1))]",
                            pinned ? "h-10 w-10" : "h-[60px] w-[60px]",
                        )}
                        style={avatarUrl == null ? undefined : {backgroundImage: `url(${JSON.stringify(avatarUrl)})`}}
                        title={data.user.username}
                        type="button"
                    >
                        {avatarUrl == null ? (
                            <span className="flex h-full w-full items-center justify-center">
                                <UserRound className={cn(pinned ? "h-4 w-4" : "h-[22px] w-[22px]")}/>
                            </span>
                        ) : null}
                    </button>
                </PopoverTrigger>
                <PopoverContent
                    align="center"
                    className="w-auto border-0 bg-transparent p-0 shadow-none"
                    sideOffset={10}
                >
                    <ProfileMenu
                        avatarUrl={avatarUrl}
                        callbackUrl={callbackUrl}
                        user={data.user}
                    />
                </PopoverContent>
            </Popover>
        </div>
    );
}

function GuestAvatarButton({
                               open,
                               pinned,
                               status,
                               toggleOpen,
                           }: {
    open: boolean;
    pinned: boolean;
    status: "authenticated" | "loading" | "unauthenticated";
    toggleOpen: () => void;
}) {
    return (
        <button
            aria-expanded={open}
            aria-label="sign in / register"
            className={cn(
                "ml-1.5 inline-flex items-center justify-center overflow-hidden rounded-full bg-osu-b4 text-osu-c1 transition-[width,height,box-shadow] duration-200 hover:shadow-[inset_0_0_0_3px_hsl(var(--hsl-c1))] aria-expanded:shadow-[inset_0_0_0_3px_hsl(var(--hsl-c1))]",
                pinned ? "h-10 w-10" : "h-[60px] w-[60px]",
            )}
            disabled={status === "loading"}
            onClick={toggleOpen}
            title="sign in / register"
            type="button"
        >
            <UserRound className={cn("transition-[width,height] duration-150", pinned ? "h-4 w-4" : "h-[22px] w-[22px]")}/>
        </button>
    );
}

function DesktopAuthMenu({
                             callbackUrl,
                             pinned,
                         }: {
    callbackUrl: string;
    pinned: boolean;
}) {
    const {status} = useSession();
    const [open, setOpen] = useState(false);

    if (status === "authenticated") {
        return <AuthenticatedUserMenu callbackUrl={callbackUrl} pinned={pinned}/>;
    }

    return (
        <div
            className="relative"
            onBlurCapture={(event) => {
                if (event.relatedTarget instanceof Node && event.currentTarget.contains(event.relatedTarget)) {
                    return;
                }

                setOpen(false);
            }}
        >
            <GuestAvatarButton
                open={open}
                pinned={pinned}
                status={status}
                toggleOpen={() => setOpen((value) => !value)}
            />

            {open ? (
                <div className="absolute right-0 top-full z-[70] mt-3">
                    <LoginPanel callbackUrl={callbackUrl}/>
                </div>
            ) : null}
        </div>
    );
}

function MobileAuthRow({
                           activeSectionId,
                           callbackUrl,
                           setOpenSectionId,
                       }: {
    activeSectionId: string | null;
    callbackUrl: string;
    setOpenSectionId: Dispatch<SetStateAction<string | null>>;
}) {
    const {data, status} = useSession();
    const [open, setOpen] = useState(false);

    if (status === "authenticated" && data != null) {
        const avatarUrl = getAvatarUrl(data.user);

        return (
            <div className="flex bg-osu-d4">
                <Link
                    className="relative mr-auto flex min-w-0 flex-1 items-center px-2 py-2 text-osu-c1 before:absolute before:bottom-[-2px] before:left-2 before:right-2 before:h-1 before:rounded-full before:bg-osu-h1 before:content-['']"
                    href={`/users/${data.user.id}`}
                >
                    <span className="mr-2 h-6 w-6 shrink-0 overflow-hidden rounded-full bg-osu-b4 text-osu-c1">
                        {avatarUrl == null ? (
                            <span className="flex h-full w-full items-center justify-center">
                                <UserRound className="h-[14px] w-[14px]"/>
                            </span>
                        ) : (
                            <span
                                className="h-full w-full bg-cover bg-center"
                                style={{backgroundImage: `url(${JSON.stringify(avatarUrl)})`}}
                            />
                        )}
                    </span>
                    <span className="truncate">{data.user.username}</span>
                </Link>

                <RealtimeNavButtons variant="mobile"/>

                <form action={signOut}>
                    <input name="callbackUrl" type="hidden" value={callbackUrl}/>
                    <button
                        className="relative h-10 w-10 text-osu-c1 before:absolute before:bottom-[-2px] before:left-1/4 before:right-1/4 before:h-1 before:rounded-full before:bg-osu-h1 before:content-['']"
                        type="submit"
                    >
                        <LogOut className="m-auto h-[18px] w-[18px]"/>
                        <span className="sr-only">sign out</span>
                    </button>
                </form>
            </div>
        );
    }

    return (
        <div className="bg-osu-d4">
            <div className="flex">
                <button
                    aria-expanded={open}
                    className="relative mr-auto flex min-w-0 flex-1 items-center px-2 py-2 text-osu-c1 text-left before:absolute before:bottom-[-2px] before:left-2 before:right-2 before:h-1 before:rounded-full before:bg-osu-h1 before:content-['']"
                    disabled={status === "loading"}
                    onClick={() => setOpen((value) => !value)}
                    type="button"
                >
                    <span className="mr-2 h-6 w-6 shrink-0 rounded-full bg-osu-b4 text-osu-c1">
                        <span className="flex h-full w-full items-center justify-center">
                            <UserRound className="h-[14px] w-[14px]"/>
                        </span>
                    </span>
                    <span className="truncate">sign in / register</span>
                </button>

                <button
                    className="relative h-10 w-10 text-osu-c1 before:absolute before:bottom-[-2px] before:left-1/4 before:right-1/4 before:h-1 before:rounded-full before:bg-osu-h1 before:content-['']"
                    onClick={() => setOpenSectionId(activeSectionId)}
                    type="button"
                >
                    <Network className="m-auto h-[18px] w-[18px]"/>
                    <span className="sr-only">navigation</span>
                </button>
            </div>

            {open ? (
                <LoginPanel
                    callbackUrl={callbackUrl}
                    className="w-full rounded-none bg-osu-b6 shadow-none"
                />
            ) : null}
        </div>
    );
}

interface DesktopHeaderProps {
    activeSectionId: string | null;
    callbackUrl: string;
    openMenuId: string | null;
    pinned: boolean;
    setOpenMenuId: Dispatch<SetStateAction<string | null>>;
}

const DesktopHeader: FC<DesktopHeaderProps> = ({
                                                   activeSectionId,
                                                   callbackUrl,
                                                   openMenuId,
                                                   pinned,
                                                   setOpenMenuId,
                                               }) => (
    <div className="fixed inset-x-0 top-0 z-50 hidden print:hidden md:block">
        <div className="relative">
            <div
                className="pointer-events-none absolute inset-0 h-[308px] border-b border-osu-h1 bg-[rgb(17_17_17_/_0.9)] transition-opacity duration-150"
                data-visibility={openMenuId == null ? "hidden" : "visible"}
                style={{opacity: openMenuId == null ? 0 : 1}}
            />
            <div
                className="pointer-events-none absolute inset-0 bg-bottom bg-repeat-x"
                style={{
                    backgroundImage: "url('/layout/nav2-background-hue0.png')",
                    filter: "hue-rotate(var(--base-hue-deg)) saturate(0.6)",
                }}
            />
            <div
                className="pointer-events-none absolute inset-0 bg-osu-h2 transition-opacity duration-200"
                style={{opacity: pinned ? 1 : 0}}
            />

            <PageWrapper className="relative">
                <div
                    className={cn(
                        "flex w-full items-stretch text-sm font-medium transition-[height] duration-200",
                        pinned ? "h-[50px]" : "h-[90px]",
                    )}
                >
                    <div className="mr-auto flex min-w-0 items-center">
                        <div className="group/logo ml-2.5 mr-2.5 flex">
                            <Logo pinned={pinned}/>
                        </div>

                        {navSections.map((section) => {
                            const active = section.id === activeSectionId;
                            const open = openMenuId === section.id;

                            return (
                                <DesktopDropdown
                                    key={section.id}
                                    menuId={section.id}
                                    onClose={() => setOpenMenuId((current) => (current === section.id ? null : current))}
                                    onOpen={() => setOpenMenuId(section.id)}
                                    openMenuId={openMenuId}
                                    trigger={(
                                        <NavLink
                                            ariaCurrent={active ? "page" : undefined}
                                            className={cn(
                                                "relative flex h-full items-center px-2.5 text-white transition-colors duration-150",
                                                open || active ? "text-white" : "text-white hover:text-white",
                                            )}
                                            href={section.href}
                                        >
                                            <div className="relative leading-none">
                                                {section.label}
                                                {active ? (
                                                    <div
                                                        className="absolute left-0 top-full mt-[5px] block h-[3px] w-full rounded-full bg-osu-h1"/>
                                                ) : null}
                                            </div>
                                        </NavLink>
                                    )}
                                >
                                    <div
                                        className={cn("rounded-lg bg-osu-b6 px-1 py-2 text-sm text-osu-c1 shadow-[0_12px_32px_rgba(0,0,0,0.32)]", "bg-transparent p-0 shadow-none")}>
                                        <div
                                            className={cn(
                                                "rounded-lg bg-osu-b6 px-1 py-2 text-sm text-osu-c1 shadow-[0_12px_32px_rgba(0,0,0,0.32)]",
                                                "bg-transparent px-0 pb-2 shadow-none",
                                                pinned ? "pt-2" : "pt-0",
                                            )}
                                        >
                                            {section.items.map((item) => (
                                                <NavLink key={item.href}
                                                         className={"relative flex items-center rounded-md px-5 py-1 transition-colors duration-150 hover:bg-osu-b6 hover:text-white before:absolute before:left-[10px] before:top-[6px] before:h-[calc(100%-12px)] before:w-[3px] before:rounded-full before:bg-osu-h1 before:opacity-0 before:transition-opacity hover:before:opacity-100"}
                                                         href={item.href}>
                                                    {item.label}
                                                </NavLink>
                                            ))}
                                        </div>
                                    </div>
                                </DesktopDropdown>
                            );
                        })}

                        <a
                            aria-label="search"
                            className="ml-1 inline-flex h-10 w-10 items-center justify-center rounded-full text-osu-c1 transition-colors duration-150 hover:bg-white/20 hover:text-white"
                            href="/home/search"
                            title="search"
                        >
                            <Search className="h-[18px] w-[18px]"/>
                        </a>
                    </div>

                    <div className="mr-2.5 flex items-center">
                        <DesktopAuthMenu callbackUrl={callbackUrl} pinned={pinned}/>
                    </div>
                </div>
            </PageWrapper>
        </div>
    </div>
);

interface MobileDisclosureProps {
    children?: ReactNode;
    icon: ReactNode;
    label: ReactNode;
    onToggle: () => void;
    open: boolean;
}

const MobileDisclosure: FC<MobileDisclosureProps> = ({
                                                         children,
                                                         icon,
                                                         label,
                                                         onToggle,
                                                         open,
                                                     }) => (
    <div className="flex flex-col">
        <button
            aria-expanded={open}
            className={"flex w-full items-center bg-transparent px-2.5 py-2.5 text-left text-osu-c1/50 transition-colors hover:text-osu-c1 aria-[expanded=true]:text-osu-c1"}
            onClick={onToggle}
            type="button"
        >
            <div className="mr-2.5 inline-flex h-[30px] w-[30px] items-center justify-center">
                {open ? <ChevronDown className="h-4 w-4"/> : icon}
            </div>
            {label}
        </button>

        {open ? <ul className="m-0 list-none p-0">{children}</ul> : null}
    </div>
);

interface MobileHeaderProps {
    activeSectionId: string | null;
    activeSectionLabel: string;
    callbackUrl: string;
    selectedLocale: Locale;
    setSelectedLocaleCode: (localeCode: string) => void;
}

const MobileHeader: FC<MobileHeaderProps> = ({
                                                 activeSectionId,
                                                 activeSectionLabel,
                                                 callbackUrl,
                                                 selectedLocale,
                                                 setSelectedLocaleCode,
                                             }) => {
    const [menuOpen, setMenuOpen] = useState(false);
    const [openSectionId, setOpenSectionId] = useState<string | null>(activeSectionId);

    return (
        <div className="print:hidden md:hidden">
            <div className="fixed inset-x-0 top-0 z-50 flex h-[50px] items-center bg-osu-h2 px-2.5 text-white">
                <div className="flex min-w-0 flex-1 items-center">
                    <Link
                        aria-label="home"
                        className="shrink-0 font-bold whitespace-nowrap text-white transition-colors duration-100 hover:text-osu-h1"
                        href="/"
                    >
                        novemo
                    </Link>
                    <div className="truncate px-2.5 text-sm">{activeSectionLabel}</div>
                </div>

                <button
                    aria-expanded={menuOpen}
                    className="flex h-[30px] w-[30px] items-center justify-center bg-transparent text-osu-c1 transition-transform duration-200"
                    onClick={() => setMenuOpen((value) => !value)}
                    type="button"
                >
                    <div className="sr-only">Toggle navigation</div>
                    <ChevronDown className={cn("h-[18px] w-[18px] transition-transform", menuOpen && "rotate-180")}/>
                </button>
            </div>

            {menuOpen ? (
                <div className="fixed inset-x-0 top-[50px] z-40 text-osu-c1">
                    <div className="grid max-h-[calc(100svh-50px)] grid-rows-[auto_1fr] bg-osu-b5">
                        <MobileAuthRow
                            activeSectionId={activeSectionId}
                            callbackUrl={callbackUrl}
                            setOpenSectionId={setOpenSectionId}
                        />

                        <div className="overflow-y-auto">
                            {navSections.map((section) => {
                                const open = section.id === openSectionId;

                                return (
                                    <MobileDisclosure
                                        key={section.id}
                                        icon={<ChevronRight className="h-4 w-4"/>}
                                        label={section.label}
                                        onToggle={() => setOpenSectionId((value) => (value === section.id ? null : section.id))}
                                        open={open}
                                    >
                                        {section.items.map((item) => (
                                            <li key={item.href}>
                                                <NavLink
                                                    className={"flex w-full items-center gap-2.5 bg-transparent px-2.5 py-[5px] pl-[60px] text-left text-osu-c2 transition-colors hover:bg-osu-b3"}
                                                    href={item.href}>
                                                    {item.label}
                                                </NavLink>
                                            </li>
                                        ))}
                                    </MobileDisclosure>
                                );
                            })}

                            <div className="flex flex-col">
                                <a
                                    className={"flex w-full items-center bg-transparent px-2.5 py-2.5 text-left text-osu-c1/50 transition-colors hover:text-osu-c1 aria-[expanded=true]:text-osu-c1"}
                                    href="/home/search"
                                >
                                    <div className="mr-2.5 inline-flex h-[30px] w-[30px] items-center justify-center">
                                        <Search className="h-4 w-4"/>
                                    </div>
                                    search
                                </a>
                            </div>

                            <MobileDisclosure
                                icon={<ChevronRight className="h-4 w-4"/>}
                                label={(
                                    <div className="flex items-center gap-2.5">
                                        <div
                                            className="inline-flex min-w-7 items-center justify-center rounded-full bg-osu-b4 px-1.5 py-0.5 text-xs font-bold leading-none text-osu-c1">
                                            {selectedLocale.badge}
                                        </div>
                                        {selectedLocale.label}
                                    </div>
                                )}
                                onToggle={() => setOpenSectionId((value) => (value === "locale" ? null : "locale"))}
                                open={openSectionId === "locale"}
                            >
                                {locales.map((locale) => (
                                    <li key={locale.code}>
                                        <button
                                            className={"flex w-full items-center gap-2.5 bg-transparent px-2.5 py-[5px] pl-[60px] text-left text-osu-c2 transition-colors hover:bg-osu-b3"}
                                            onClick={() => {
                                                setSelectedLocaleCode(locale.code);
                                                setOpenSectionId(null);
                                            }}
                                            type="button"
                                        >
                                            <div
                                                className="inline-flex min-w-7 items-center justify-center rounded-full bg-osu-b4 px-1.5 py-0.5 text-xs font-bold leading-none text-osu-c1">
                                                {locale.badge}
                                            </div>
                                            {locale.label}
                                        </button>
                                    </li>
                                ))}
                            </MobileDisclosure>
                        </div>
                    </div>
                </div>
            ) : null}
        </div>
    );
};

export const TopHeader = () => {
    const pathname = usePathname();
    const [callbackUrl, setCallbackUrl] = useState(pathname);
    const [isPinned, setIsPinned] = useState(false);
    const [openMenuId, setOpenMenuId] = useState<string | null>(null);
    const [selectedLocaleCode, setSelectedLocaleCode] = useState(locales[0].code);

    const updateCallbackUrl = useCallback(() => {
        setCallbackUrl((current) => {
            const next = `${window.location.pathname}${window.location.search}${window.location.hash}`;

            return current === next ? current : next;
        });
    }, []);

    useEffect(() => {
        queueMicrotask(updateCallbackUrl);
    });

    useEffect(() => {
        window.addEventListener("hashchange", updateCallbackUrl);
        window.addEventListener("popstate", updateCallbackUrl);

        return () => {
            window.removeEventListener("hashchange", updateCallbackUrl);
            window.removeEventListener("popstate", updateCallbackUrl);
        };
    }, [updateCallbackUrl]);

    useEffect(() => {
        const handleScroll = () => {
            setIsPinned(window.scrollY > 30);
        };

        handleScroll();
        window.addEventListener("scroll", handleScroll, {passive: true});

        return () => {
            window.removeEventListener("scroll", handleScroll);
        };
    }, []);

    const activeSection = useMemo(
        () => navSections.find((section) => section.matches(pathname)) ?? null,
        [pathname],
    );
    const selectedLocale = useMemo(
        () => locales.find((locale) => locale.code === selectedLocaleCode) ?? locales[0],
        [selectedLocaleCode],
    );

    return (
        <>
            <DesktopHeader
                activeSectionId={activeSection?.id ?? null}
                callbackUrl={callbackUrl}
                openMenuId={openMenuId}
                pinned={isPinned}
                setOpenMenuId={setOpenMenuId}
            />
            <MobileHeader
                key={pathname}
                activeSectionId={activeSection?.id ?? null}
                activeSectionLabel={activeSection?.label ?? getFallbackMobileLabel(pathname)}
                callbackUrl={callbackUrl}
                selectedLocale={selectedLocale}
                setSelectedLocaleCode={setSelectedLocaleCode}
            />
        </>
    );
};

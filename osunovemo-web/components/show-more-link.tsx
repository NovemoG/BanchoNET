"use client";

import {ChevronDown, ChevronUp, LoaderCircle} from "lucide-react";
import {cn} from "@/lib/utils";

type ShowMoreDirection = "down" | "up";

type ShowMoreLinkProps = {
    className?: string;
    direction?: ShowMoreDirection;
    hasMore: boolean;
    label?: string;
    loading?: boolean;
    noIcon?: boolean;
    onClick?: () => void;
    remaining?: number;
    type?: "button" | "submit";
};

export function ShowMoreLink({
    className,
    direction = "down",
    hasMore,
    label = "Show more",
    loading = false,
    noIcon = false,
    onClick,
    remaining,
    type = "button",
}: ShowMoreLinkProps) {
    if (!hasMore && !loading) {
        return null;
    }

    const Icon = direction === "up" ? ChevronUp : ChevronDown;
    const icon = noIcon ? null : <Icon className="h-3.5 w-3.5" strokeWidth={2.25}/>;

    return (
        <button
            className={cn(
                "group relative mx-auto flex w-fit items-center justify-center overflow-hidden rounded-full bg-osu-b2 px-5 py-1 leading-normal text-white shadow transition-colors duration-150 hover:bg-osu-b1 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-osu-c2/40 disabled:hover:bg-osu-b2",
                className,
            )}
            disabled={loading}
            onClick={onClick}
            type={type}
        >
            <span
                aria-hidden={!loading}
                className={cn(
                    "pointer-events-none absolute inset-0 flex items-center justify-center transition-opacity duration-150",
                    loading ? "opacity-100" : "opacity-0",
                )}
            >
                <LoaderCircle className="h-4 w-4 animate-spin"/>
            </span>

            <span
                className={cn(
                    "flex items-center gap-0.5 transition-opacity duration-150",
                    loading ? "opacity-0" : "opacity-100",
                )}
            >
                <span className={cn("mx-1 text-osu-f1 transition-colors duration-150 group-hover:text-osu-l1", noIcon && "hidden")}>
                    {icon}
                </span>
                <span className=" mx-1 text-xs uppercase">
                    {label}
                    {remaining != null ? ` (${remaining})` : ""}
                </span>
                <span className={cn("mx-1 text-osu-f1 transition-colors duration-150 group-hover:text-osu-l1", noIcon && "hidden")}>
                    {icon}
                </span>
            </span>
        </button>
    );
}

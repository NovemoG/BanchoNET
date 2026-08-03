"use client";

import Link from "next/link";
import {
    CheckCircle2,
    ChevronDown,
    ChevronUp,
    Heart,
    Megaphone,
    Play,
    ThumbsUp,
} from "lucide-react";
import {BaseBeatmapsetPanel} from "@/components/beatmapset-panels/base-beatmapset-panel";
import {
    BeatmapPreviewButton,
    BeatmapsetCoverBackground,
    BeatmapsetDifficultyStrip,
    BeatmapsetMediaBadges,
    BeatmapsetStatusBadge,
    formatCompactNumber,
    formatFullNumber,
    PanelStat,
    type BeatmapsetPanelProps,
} from "@/components/beatmapset-panels/shared";
import {cn} from "@/lib/utils";

export function BeatmapsetPanelCover({
    beatmapset,
    className,
}: Omit<BeatmapsetPanelProps, "size">) {
    return (
        <BaseBeatmapsetPanel beatmapset={beatmapset}>
            {(panel) => {
                return (
                    <div
                        className={cn(
                            "group/panel relative min-w-0 overflow-hidden rounded-xl bg-osu-b2",
                            className,
                        )}
                        {...panel.rootProps}
                    >
                        <div
                            aria-hidden
                            className={cn(
                                "pointer-events-none absolute bottom-0 left-0 h-2.5 w-full bg-inherit transition-opacity duration-150",
                                panel.isBeatmapsPopupVisible ? "opacity-100" : "opacity-0",
                            )}
                        />

                        <div className="absolute inset-0 flex flex-col">
                            <Link
                                prefetch={false}
                                className="pointer-events-none z-1 block overflow-hidden rounded-lg bg-osu-b2 md:pointer-events-auto"
                                href={panel.data.panelUrl}
                            >
                                <div className="relative aspect-3/1 overflow-hidden bg-osu-b2">
                                    <BeatmapsetCoverBackground src={panel.data.coverSlimUrl}/>
                                    <div
                                        aria-hidden
                                        className={cn(
                                            "absolute inset-0 bg-[linear-gradient(180deg,rgba(0,0,0,0.1)_0%,rgba(0,0,0,0.38)_52%,rgba(0,0,0,0.8)_100%)] transition-opacity duration-150",
                                            "md:group-hover/panel:opacity-0 md:group-focus-within/panel:opacity-0",
                                            panel.isPlayUiActive && "opacity-0",
                                        )}
                                    />
                                    <div
                                        aria-hidden
                                        className={cn(
                                            "absolute inset-0 bg-[linear-gradient(180deg,rgba(0,0,0,0.3)_0%,rgba(0,0,0,0.72)_52%,rgba(0,0,0,1)_100%)] opacity-0 transition-opacity duration-150",
                                            "md:group-hover/panel:opacity-100 md:group-focus-within/panel:opacity-100",
                                            panel.isPlayUiActive && "opacity-100",
                                        )}
                                    />
                                </div>
                            </Link>
                        </div>

                        <div className="pointer-events-none relative z-10 flex h-full flex-col text-white">
                            <div
                                className={cn(
                                    "relative aspect-3/1 flex-none",
                                    "bg-transparent md:group-hover/panel:bg-osu-b6/10 md:group-focus-within/panel:bg-osu-b6/10",
                                    panel.isPlayUiActive && "bg-osu-b6/10",
                                )}
                            >
                                <div className="absolute left-2.5 top-2.5 flex flex-wrap items-center gap-1.5">
                                    <BeatmapsetMediaBadges
                                        badgeClassName="m-0 h-5 w-auto px-2 bg-osu-b6/70 [&_svg]:h-3.5 [&_svg]:w-3.5"
                                        beatmapset={beatmapset}
                                        className="static"
                                        playUiVisibilityClassName="opacity-100"
                                    />
                                    <BeatmapsetStatusBadge
                                        className="px-2 py-0.75 text-sm leading-none"
                                        status={beatmapset.status}
                                        statusTone={panel.data.statusTone}
                                    />
                                </div>

                                <div className="absolute inset-x-3 bottom-2.5 right-16">
                                    <div className="overflow-hidden text-2xl font-semibold leading-none text-white [display:-webkit-box] [-webkit-box-orient:vertical] [-webkit-line-clamp:2] [text-shadow:0_1px_0_rgba(0,0,0,0.3)]">
                                        {panel.data.title}
                                    </div>
                                    <div className="mt-1 truncate text-base font-semibold text-white [text-shadow:0_1px_0_rgba(0,0,0,0.3)]">
                                        {panel.data.artist}
                                    </div>
                                </div>

                                <BeatmapPreviewButton
                                    audioProps={panel.audioProps}
                                    audioRef={panel.audioRef}
                                    buttonClassName="inset-auto right-3 bottom-3 m-0 h-11 w-11"
                                    canPlayPreview={panel.data.canPlayPreview}
                                    isPlaying={panel.isPlaying}
                                    playUiVisibilityClassName={panel.playUiVisibilityClassName}
                                    progress={panel.progress}
                                    togglePreview={panel.togglePreview}
                                />
                            </div>

                            <div className="relative h-18 flex-none overflow-hidden bg-osu-b2">
                                <div className="relative flex h-full flex-col justify-between px-3 py-2">
                                    <div className="flex min-w-0 items-center gap-3 text-sm text-osu-c2">
                                        <div className="min-w-0 truncate font-semibold">
                                            <span className="text-osu-c2">by{" "}</span>
                                            <Link
                                                prefetch={false}
                                                className="pointer-events-auto text-osu-l3! no-underline"
                                                href={panel.data.mapperUrl}
                                            >
                                                {beatmapset.creator}
                                            </Link>
                                        </div>

                                        <div className="ml-auto flex shrink-0 items-center gap-3 text-sm font-semibold">
                                            <PanelStat
                                                icon={<Play className="h-2.5 w-2.5 fill-current"/>}
                                                title={`${formatFullNumber(beatmapset.play_count)} plays`}
                                                value={formatCompactNumber(beatmapset.play_count)}
                                            />
                                            <PanelStat
                                                icon={<Heart className="h-2.5 w-2.5"/>}
                                                title={`${formatFullNumber(beatmapset.favourite_count)} favourites`}
                                                value={formatCompactNumber(beatmapset.favourite_count)}
                                            />
                                        </div>
                                    </div>

                                    <div className="flex min-w-0 items-center gap-3 text-sm text-osu-c2">
                                        <div className="min-w-0 flex-1 truncate font-semibold">
                                            {beatmapset.source || "\u00A0"}
                                        </div>

                                        <div className="ml-auto flex shrink-0 items-center gap-2.5">
                                            {beatmapset.hype ? (
                                                <PanelStat
                                                    icon={<Megaphone className="h-2 w-2"/>}
                                                    title={`Hype ${formatFullNumber(beatmapset.hype.current)} / ${formatFullNumber(beatmapset.hype.required)}`}
                                                    value={formatCompactNumber(beatmapset.hype.current)}
                                                />
                                            ) : null}
                                            {panel.data.nominations ? (
                                                <PanelStat
                                                    icon={<ThumbsUp className="h-2 w-2"/>}
                                                    title={`Nominations ${formatFullNumber(panel.data.nominations.current)} / ${formatFullNumber(panel.data.nominations.required)}`}
                                                    value={formatCompactNumber(panel.data.nominations.current)}
                                                />
                                            ) : null}
                                            {panel.data.displayDate ? (
                                                <PanelStat
                                                    icon={<CheckCircle2 className="h-2 w-2 fill-none! stroke-[2.25]"/>}
                                                    title={panel.data.displayDateValue ?? undefined}
                                                    value={panel.data.displayDate}
                                                />
                                            ) : null}
                                        </div>
                                    </div>

                                    <BeatmapsetDifficultyStrip
                                        beatmapDotsCompact={panel.data.beatmapDotsCompact}
                                        className="min-w-0 text-xs"
                                        dotClassName="h-3 w-2"
                                        itemClassName="mx-0.5"
                                        modeClassName="text-sm"
                                        onMouseEnter={panel.triggerProps.onMouseEnter}
                                        onMouseLeave={panel.triggerProps.onMouseLeave}
                                        panelUrl={panel.data.panelUrl}
                                        showStatus={false}
                                        status={beatmapset.status}
                                        statusTone={panel.data.statusTone}
                                        visibleBeatmapGroups={panel.data.visibleBeatmapGroups}
                                    />
                                </div>
                            </div>
                        </div>

                        {panel.data.canShowPopup ? (
                            <button
                                aria-label={panel.mobileExpanded ? "Collapse beatmaps" : "Expand beatmaps"}
                                className="pointer-events-auto absolute right-0 bottom-0 flex h-5 w-full items-center justify-end border-0 bg-transparent pr-2.5 text-osu-c2 md:hidden"
                                onClick={() => panel.setMobileExpanded((value) => !value)}
                                type="button"
                            >
                                {panel.mobileExpanded ? (
                                    <ChevronUp className="h-4 w-4"/>
                                ) : (
                                    <ChevronDown className="h-4 w-4"/>
                                )}
                            </button>
                        ) : null}
                    </div>
                );
            }}
        </BaseBeatmapsetPanel>
    );
}

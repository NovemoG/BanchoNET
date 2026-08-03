"use client";

import Link from "next/link";
import {ChevronDown, ChevronRight, ChevronUp, Heart, Play} from "lucide-react";
import {BaseBeatmapsetPanel} from "@/components/beatmapset-panels/base-beatmapset-panel";
import {
    BeatmapPreviewButton,
    BeatmapsetCoverBackground,
    BeatmapsetDifficultyStrip,
    BeatmapsetMediaBadges,
    formatCompactNumber,
    PanelStat,
    type BeatmapsetPanelProps,
} from "@/components/beatmapset-panels/shared";
import {cn} from "@/lib/utils";

export function BeatmapsetPanelNano({
    beatmapset,
    className,
}: Omit<BeatmapsetPanelProps, "size">) {
    return (
        <BaseBeatmapsetPanel beatmapset={beatmapset}>
            {(panel) => (
                <div
                    className={cn(
                        "group/panel relative min-w-0 overflow-hidden rounded-lg bg-osu-b2",
                        "h-14",
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

                    <div className="absolute inset-0 flex">
                        <Link
                            prefetch={false}
                            className="pointer-events-none z-1 flex flex-1 overflow-hidden rounded-lg bg-osu-b2 -mr-2.5 md:h-full md:pointer-events-auto"
                            href={panel.data.panelUrl}
                        >
                            <div
                                aria-hidden
                                className="absolute inset-y-0 left-25 right-0 bg-osu-b3 md:hidden"
                            />
                            <div className="relative h-full aspect-video flex-none overflow-hidden">
                                <BeatmapsetCoverBackground src={panel.data.coverListUrl}/>
                            </div>
                            <div className="relative min-w-0 flex-1 overflow-hidden rounded-lg -mx-2">
                                <BeatmapsetCoverBackground src={panel.data.coverCardUrl}/>
                            </div>
                        </Link>

                        <div
                            className={cn(
                                "relative w-0 flex-none overflow-hidden bg-transparent pl-2.5 transition-all duration-150 md:w-5 md:overflow-visible md:bg-osu-b3",
                                "md:group-hover/panel:w-10 md:group-focus-within/panel:w-10",
                                panel.isPanelExpanded && "w-10 bg-osu-b3 md:w-10",
                            )}
                        >
                            <div
                                className={cn(
                                    "flex h-full items-center justify-center overflow-hidden text-osu-c2 opacity-0 transition-opacity duration-150",
                                    "md:group-hover/panel:opacity-100 md:group-focus-within/panel:opacity-100",
                                    panel.isPanelExpanded && "opacity-100",
                                )}
                            >
                                <ChevronRight className="h-4 w-4 text-white/45"/>
                            </div>
                        </div>
                    </div>

                    <div className="pointer-events-none relative z-10 flex h-full w-full">
                        <div
                            className={cn(
                                "relative h-full aspect-video flex-none",
                                "bg-transparent md:group-hover/panel:bg-osu-b6/80 md:group-focus-within/panel:bg-osu-b6/80",
                                panel.isPlayUiActive && "bg-osu-b6/80",
                            )}
                        >
                            <BeatmapPreviewButton
                                audioProps={panel.audioProps}
                                audioRef={panel.audioRef}
                                buttonClassName="h-9 w-9"
                                canPlayPreview={panel.data.canPlayPreview}
                                isPlaying={panel.isPlaying}
                                playUiVisibilityClassName={panel.playUiVisibilityClassName}
                                progress={panel.progress}
                                togglePreview={panel.togglePreview}
                            />

                            <BeatmapsetMediaBadges
                                badgeClassName="h-4.5 w-4.5 [&_svg]:h-2 [&_svg]:w-2"
                                beatmapset={beatmapset}
                                playUiVisibilityClassName={panel.playUiVisibilityClassName}
                            />
                        </div>

                        <div
                            className={cn(
                                "relative flex min-w-0 flex-1 flex-col justify-center overflow-hidden rounded-lg -ml-2.5 mr-2.5 px-2.5 py-1.5 text-white transition-all duration-150",
                                "bg-transparent md:bg-linear-to-r md:from-osu-b2 md:to-osu-b2/70",
                                "md:group-hover/panel:mr-7.5 md:group-hover/panel:to-osu-b2/90",
                                "md:group-focus-within/panel:mr-7.5 md:group-focus-within/panel:to-osu-b2/90",
                                panel.isPanelExpanded && "md:mr-7.5 md:to-osu-b2/90",
                            )}
                            onMouseEnter={panel.triggerProps.onMouseEnter}
                        >
                            <div className="relative min-h-4.5">
                                <div
                                    className={cn(
                                        "flex min-w-0 items-baseline gap-1 text-white transition-opacity duration-150",
                                        "md:group-hover/panel:opacity-0 md:group-focus-within/panel:opacity-0",
                                        panel.isPanelExpanded && "opacity-0",
                                    )}
                                >
                                    <Link
                                        prefetch={false}
                                        className="pointer-events-auto min-w-0 truncate text-base font-semibold leading-tight text-white no-underline hover:text-white [text-shadow:0_1px_0_rgba(0,0,0,0.3)]"
                                        href={panel.data.panelUrl}
                                    >
                                        {panel.data.title}
                                    </Link>
                                    <Link
                                        prefetch={false}
                                        className="pointer-events-auto min-w-0 truncate text-sm font-semibold leading-tight text-white/70 no-underline hover:text-white [text-shadow:0_1px_0_rgba(0,0,0,0.3)]"
                                        href={panel.data.panelUrl}
                                    >
                                        {panel.data.artist}
                                    </Link>
                                </div>

                                <div
                                    className={cn(
                                        "absolute inset-0 flex min-w-0 items-center gap-2 text-sm text-osu-c2 opacity-0 transition-opacity duration-150 [text-shadow:0_1px_0_rgba(0,0,0,0.3)]",
                                        "md:group-hover/panel:opacity-100 md:group-focus-within/panel:opacity-100",
                                        panel.isPanelExpanded && "opacity-100",
                                    )}
                                >
                                    <PanelStat
                                        className="min-w-0"
                                        icon={<Play className="fill-current"/>}
                                        title={`${formatCompactNumber(beatmapset.play_count)} plays`}
                                        value={formatCompactNumber(beatmapset.play_count)}
                                    />
                                    <PanelStat
                                        className="min-w-0"
                                        icon={<Heart/>}
                                        title={`${formatCompactNumber(beatmapset.favourite_count)} favourites`}
                                        value={formatCompactNumber(beatmapset.favourite_count)}
                                    />
                                </div>
                            </div>

                            <div className="relative mt-1 min-h-4">
                                <div
                                    className={cn(
                                        "truncate text-sm text-osu-c2 font-semibold transition-opacity duration-150 [text-shadow:0_1px_0_rgba(0,0,0,0.3)]",
                                        "md:group-hover/panel:opacity-0 md:group-focus-within/panel:opacity-0",
                                        panel.isPanelExpanded && "opacity-0",
                                    )}
                                >
                                    by{" "}
                                    <Link
                                        prefetch={false}
                                        className="pointer-events-auto text-osu-l3! no-underline"
                                        href={panel.data.mapperUrl}
                                    >
                                        {beatmapset.creator}
                                    </Link>
                                </div>

                                <BeatmapsetDifficultyStrip
                                    beatmapDotsCompact={panel.data.beatmapDotsCompact}
                                    className={cn(
                                        "pointer-events-none absolute inset-0 min-w-0 text-sm opacity-0 transition-opacity duration-150",
                                        "md:group-hover/panel:pointer-events-auto md:group-hover/panel:opacity-100",
                                        "md:group-focus-within/panel:pointer-events-auto md:group-focus-within/panel:opacity-100",
                                        panel.isPanelExpanded && "pointer-events-auto opacity-100",
                                    )}
                                    countClassName="text-sm"
                                    dotClassName="h-2.5 w-1.25"
                                    panelUrl={panel.data.panelUrl}
                                    status={beatmapset.status}
                                    statusPillClassName="shrink-0 px-1 text-[0.7rem] leading-3"
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
            )}
        </BaseBeatmapsetPanel>
    );
}

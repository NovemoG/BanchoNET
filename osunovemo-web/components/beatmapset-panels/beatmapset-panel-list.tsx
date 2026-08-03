"use client";

import Link from "next/link";
import {CheckCircle2, ChevronDown, ChevronUp, Download, Heart, Megaphone, Play, ThumbsUp} from "lucide-react";
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

export function BeatmapsetPanelList({
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
                                    "md:group-hover/panel:w-16 md:group-focus-within/panel:w-16",
                                    panel.isPanelExpanded && "w-16 bg-osu-b3 md:w-16",
                                )}
                            >
                                <div
                                    className={cn(
                                        "pointer-events-auto flex h-full items-center justify-center gap-3 overflow-hidden text-osu-c2 opacity-0 transition-opacity duration-150",
                                        "md:group-hover/panel:opacity-100 md:group-focus-within/panel:opacity-100",
                                        panel.isPanelExpanded && "opacity-100",
                                    )}
                                >
                                    <div
                                        className={cn(
                                            "pointer-events-auto inline-flex items-center justify-center py-0.75 text-osu-l1",
                                            panel.data.favouriteActive && "text-osu-h1",
                                        )}
                                        title="Favourite unavailable"
                                    >
                                        <Heart
                                            className={cn(
                                                "h-3.5 w-3.5",
                                                panel.data.favouriteActive && "fill-current",
                                            )}
                                        />
                                    </div>

                                    {panel.data.downloadLink == null ? (
                                        <div
                                            className="pointer-events-auto inline-flex items-center justify-center py-0.75 text-osu-l1"
                                            title={beatmapset.availability?.more_information ?? "Download unavailable"}
                                        >
                                            <Download className="h-3.5 w-3.5"/>
                                        </div>
                                    ) : (
                                        <a
                                            className="pointer-events-auto inline-flex items-center justify-center py-0.75 text-osu-l1 no-underline transition-colors duration-150 hover:text-osu-c1"
                                            href={panel.data.downloadLink.url}
                                            rel="noreferrer"
                                            target="_blank"
                                            title={panel.data.downloadLink.title}
                                        >
                                            <Download className="h-3.5 w-3.5"/>
                                        </a>
                                    )}
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
                                    badgeClassName="h-5 w-auto px-2 [&_svg]:h-3 [&_svg]:w-3"
                                    beatmapset={beatmapset}
                                    playUiVisibilityClassName={panel.playUiVisibilityClassName}
                                />
                            </div>

                            <div
                                className={cn(
                                    "relative flex min-w-0 flex-1 flex-col justify-center overflow-hidden rounded-lg -ml-2.5 mr-2.5 px-2.5 py-1.5 text-white transition-all duration-150",
                                    "bg-transparent md:bg-linear-to-r md:from-osu-b2 md:to-osu-b2/70",
                                    "md:group-hover/panel:mr-13.5 md:group-hover/panel:to-osu-b2/90",
                                    "md:group-focus-within/panel:mr-13.5 md:group-focus-within/panel:to-osu-b2/90",
                                    panel.isPanelExpanded && "md:mr-13.5 md:to-osu-b2/90",
                                )}
                            >
                                <div className="flex min-w-0 items-baseline gap-3">
                                    <div className="flex min-w-0 items-baseline gap-1 text-white [text-shadow:0_1px_0_rgba(0,0,0,0.3)]">
                                        <Link
                                            prefetch={false}
                                            className="pointer-events-auto min-w-0 truncate text-base font-semibold leading-tight text-white no-underline hover:text-white"
                                            href={panel.data.panelUrl}
                                        >
                                            {panel.data.title}
                                        </Link>
                                        <Link
                                            prefetch={false}
                                            className="pointer-events-auto min-w-0 truncate text-sm font-semibold leading-tight text-white/70 no-underline hover:text-white"
                                            href={panel.data.panelUrl}
                                        >
                                            {panel.data.artist}
                                        </Link>
                                    </div>

                                    {panel.data.visibleBeatmapGroups.length > 0 ? (
                                        <BeatmapsetDifficultyStrip
                                            beatmapDotsCompact={panel.data.beatmapDotsCompact}
                                            className="ml-auto shrink-0 text-sm"
                                            countClassName="text-sm"
                                            dotClassName="h-2.5 w-1.25"
                                            itemClassName="mx-0 ml-1.5 first:ml-0"
                                            onMouseEnter={panel.triggerProps.onMouseEnter}
                                            onMouseLeave={panel.triggerProps.onMouseLeave}
                                            panelUrl={panel.data.panelUrl}
                                            showStatus={false}
                                            status={beatmapset.status}
                                            statusTone={panel.data.statusTone}
                                            visibleBeatmapGroups={panel.data.visibleBeatmapGroups}
                                        />
                                    ) : null}
                                </div>

                                <div className="mt-1 flex min-w-0 items-center gap-3 text-sm [text-shadow:0_1px_0_rgba(0,0,0,0.3)]">
                                    <div className="flex min-w-0 flex-1 items-center gap-1.5 overflow-hidden whitespace-nowrap">
                                        <BeatmapsetStatusBadge
                                            className="shrink-0"
                                            status={beatmapset.status}
                                            statusTone={panel.data.statusTone}
                                        />
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
                                        {beatmapset.source && (
                                            <>
                                                <div aria-hidden className="shrink-0 text-osu-c2">·</div>
                                                <div className="min-w-0 truncate text-osu-c2">{beatmapset.source}</div>
                                            </>
                                        )}
                                    </div>

                                    <div className="ml-auto flex shrink-0 items-center justify-end gap-2 overflow-hidden whitespace-nowrap">
                                        <PanelStat
                                            className="shrink-0"
                                            icon={<Play className="h-2 w-2 fill-current"/>}
                                            title={`${formatFullNumber(beatmapset.play_count)} plays`}
                                            value={formatCompactNumber(beatmapset.play_count)}
                                        />
                                        <PanelStat
                                            className="shrink-0"
                                            icon={<Heart className="h-2 w-2"/>}
                                            title={`${formatFullNumber(beatmapset.favourite_count)} favourites`}
                                            value={formatCompactNumber(beatmapset.favourite_count)}
                                        />
                                        {beatmapset.hype ? (
                                            <PanelStat
                                                className="shrink-0"
                                                icon={<Megaphone className="h-2 w-2"/>}
                                                title={`Hype ${formatFullNumber(beatmapset.hype.current)} / ${formatFullNumber(beatmapset.hype.required)}`}
                                                value={formatCompactNumber(beatmapset.hype.current)}
                                            />
                                        ) : null}
                                        {panel.data.nominations ? (
                                            <PanelStat
                                                className="shrink-0"
                                                icon={<ThumbsUp className="h-2 w-2"/>}
                                                title={`Nominations ${formatFullNumber(panel.data.nominations.current)} / ${formatFullNumber(panel.data.nominations.required)}`}
                                                value={formatCompactNumber(panel.data.nominations.current)}
                                            />
                                        ) : null}
                                        {panel.data.displayDate ? (
                                            <PanelStat
                                                className="shrink-0"
                                                icon={<CheckCircle2 className="h-2 w-2 fill-none! stroke-[2.25]"/>}
                                                title={panel.data.displayDateValue ?? undefined}
                                                value={panel.data.displayDate}
                                            />
                                        ) : null}
                                    </div>
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

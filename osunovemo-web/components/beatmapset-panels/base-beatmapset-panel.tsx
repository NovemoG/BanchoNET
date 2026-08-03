"use client";

import {createPortal} from "react-dom";
import type {CSSProperties, ComponentPropsWithoutRef, Dispatch, MouseEvent as ReactMouseEvent, ReactNode, RefObject, SetStateAction} from "react";
import {useCallback, useEffect, useMemo, useRef, useState} from "react";
import {useMiniPlayer} from "@/components/audio/mini-player-provider";
import {BeatmapPopupRow, getBeatmapsetPanelData, type BeatmapsetPanelData} from "@/components/beatmapset-panels/shared";
import type {Beatmapset} from "@/lib/profile";
import {cn} from "@/lib/utils";

type PopupPosition = {
    left: string;
    panelHeight: string;
    top: string;
    width: string;
};

type AudioProps = Pick<
    ComponentPropsWithoutRef<"audio">,
    "onEnded" | "onPause" | "onPlay" | "onTimeUpdate" | "preload" | "src"
>;

export type BaseBeatmapsetPanelRenderProps = {
    audioProps: AudioProps;
    audioRef: RefObject<HTMLAudioElement | null>;
    beatmapset: Beatmapset;
    data: BeatmapsetPanelData;
    isBeatmapsPopupVisible: boolean;
    isPanelExpanded: boolean;
    isPlaying: boolean;
    isPlayUiActive: boolean;
    isPreviewLoading: boolean;
    mobileExpanded: boolean;
    playUiVisibilityClassName: string;
    progress: number;
    rootProps: {
        onMouseLeave?: () => void;
        ref: RefObject<HTMLDivElement | null>;
    };
    setMobileExpanded: Dispatch<SetStateAction<boolean>>;
    togglePreview: (event: ReactMouseEvent<HTMLButtonElement>) => Promise<void>;
    triggerProps: {
        onMouseEnter?: () => void;
        onMouseLeave?: () => void;
    };
};

const beatmapsPopupHideDelay = 500;
// Opening the popup pulls a larger cover for that set, so at 100ms a quick sweep across the
// listing fired a request per card passed over. Long enough to require settling on one card.
const beatmapsPopupShowDelay = 300;

export function BaseBeatmapsetPanel({
    beatmapset,
    children,
}: {
    beatmapset: Beatmapset;
    children: (props: BaseBeatmapsetPanelRenderProps) => ReactNode;
}) {
    const miniPlayer = useMiniPlayer();
    const [isPlaying, setIsPlaying] = useState(false);
    const [isPreviewLoading, setIsPreviewLoading] = useState(false);
    const [mobileExpanded, setMobileExpanded] = useState(false);
    const [beatmapsPopupHover, setBeatmapsPopupHover] = useState(false);
    const [progress, setProgress] = useState(0);
    const [popupPosition, setPopupPosition] = useState<PopupPosition | null>(null);
    const audioRef = useRef<HTMLAudioElement | null>(null);
    const blockRef = useRef<HTMLDivElement | null>(null);
    const popupRef = useRef<HTMLDivElement | null>(null);
    const hideTimeoutRef = useRef<number | null>(null);
    const showTimeoutRef = useRef<number | null>(null);
    const data = useMemo(() => getBeatmapsetPanelData(beatmapset), [beatmapset]);
    const isPanelExpanded = mobileExpanded || beatmapsPopupHover;
    const isPlayUiActive = isPanelExpanded || isPlaying || isPreviewLoading;
    const isBeatmapsPopupVisible = data.canShowPopup && (beatmapsPopupHover || mobileExpanded);
    const popupStyle: CSSProperties | undefined =
        popupPosition == null
            ? undefined
            : {
                left: popupPosition.left,
                top: popupPosition.top,
                width: popupPosition.width,
            };
    const playUiVisibilityClassName = cn(
        "opacity-0 transition-opacity duration-150 md:group-hover/panel:opacity-100 md:group-focus-within/panel:opacity-100",
        isPlayUiActive && "opacity-100",
    );

    const clearPopupTimeouts = useCallback(() => {
        if (hideTimeoutRef.current != null) {
            window.clearTimeout(hideTimeoutRef.current);
            hideTimeoutRef.current = null;
        }

        if (showTimeoutRef.current != null) {
            window.clearTimeout(showTimeoutRef.current);
            showTimeoutRef.current = null;
        }
    }, []);

    const updatePopupPosition = useCallback(() => {
        const block = blockRef.current;
        if (block == null) {
            return;
        }

        const rect = block.getBoundingClientRect();
        const left = Math.round(window.scrollX + rect.left);
        const right = Math.round(window.scrollX + rect.right);
        const top = Math.round(window.scrollY + rect.bottom);
        setPopupPosition({
            left: `${left}px`,
            panelHeight: `${Math.round(rect.height)}px`,
            top: `${top}px`,
            width: `${Math.max(0, right - left)}px`,
        });
    }, []);

    const beatmapsPopupKeep = useCallback(() => {
        clearPopupTimeouts();
        setBeatmapsPopupHover(true);
    }, [clearPopupTimeouts]);

    const beatmapsPopupDelayedHide = useCallback(() => {
        clearPopupTimeouts();
        hideTimeoutRef.current = window.setTimeout(() => {
            setBeatmapsPopupHover(false);
            hideTimeoutRef.current = null;
        }, beatmapsPopupHideDelay);
    }, [clearPopupTimeouts]);

    const beatmapsPopupDelayedShow = useCallback(() => {
        clearPopupTimeouts();
        showTimeoutRef.current = window.setTimeout(() => {
            setBeatmapsPopupHover(true);
            showTimeoutRef.current = null;
        }, beatmapsPopupShowDelay);
    }, [clearPopupTimeouts]);

    useEffect(
        () => () => {
            clearPopupTimeouts();
        },
        [clearPopupTimeouts],
    );

    useEffect(() => {
        if (!mobileExpanded) {
            return undefined;
        }

        const handleDocumentClick = (event: MouseEvent) => {
            const target = event.target as Node;

            if (blockRef.current?.contains(target) || popupRef.current?.contains(target)) {
                return;
            }

            setMobileExpanded(false);
        };

        document.addEventListener("click", handleDocumentClick);
        return () => document.removeEventListener("click", handleDocumentClick);
    }, [mobileExpanded]);

    useEffect(() => {
        if (!isBeatmapsPopupVisible) {
            return undefined;
        }

        updatePopupPosition();

        const handlePositionChange = () => updatePopupPosition();
        window.addEventListener("resize", handlePositionChange);
        window.addEventListener("scroll", handlePositionChange, true);

        return () => {
            window.removeEventListener("resize", handlePositionChange);
            window.removeEventListener("scroll", handlePositionChange, true);
        };
    }, [isBeatmapsPopupVisible, updatePopupPosition]);

    useEffect(() => {
        const audio = audioRef.current;
        if (audio == null) {
            return undefined;
        }
        audio.volume = 0.1;

        return () => {
            audio.pause();
            audio.currentTime = 0;
        };
    }, []);

    const togglePreview = useCallback(
        async (event: ReactMouseEvent<HTMLButtonElement>) => {
            event.preventDefault();
            event.stopPropagation();

            const audio = audioRef.current;
            if (audio == null) {
                return;
            }

            if (audio.paused) {
                setIsPreviewLoading(true);
                try {
                    await audio.play();
                } catch {
                    setIsPreviewLoading(false);
                    setIsPlaying(false);
                }
                return;
            }

            audio.pause();
        },
        [],
    );

    const audioProps: AudioProps = {
        onEnded: (event) => {
            miniPlayer?.endPlayback(event.currentTarget);
            event.currentTarget.currentTime = 0;
            setIsPlaying(false);
            setIsPreviewLoading(false);
            setProgress(0);
        },
        onPause: (event) => {
            miniPlayer?.endPlayback(event.currentTarget);
            setIsPlaying(false);
            setIsPreviewLoading(false);
        },
        onPlay: (event) => {
            // Hands this card's audio element to the mini player, which also stops whatever else
            // was playing so only one preview runs at a time.
            miniPlayer?.startPlayback(event.currentTarget, {
                artist: beatmapset.artist_unicode || beatmapset.artist,
                coverUrl: data.coverListUrl,
                href: `/beatmapsets/${beatmapset.id}`,
                id: beatmapset.id,
                title: beatmapset.title_unicode || beatmapset.title,
            });
            setIsPlaying(true);
            setIsPreviewLoading(false);
        },
        onTimeUpdate: (event) => {
            const {currentTime, duration} = event.currentTarget;
            setProgress(duration > 0 ? currentTime / duration : 0);
        },
        preload: "none",
        src: data.previewUrl,
    };

    return (
        <>
            {children({
                audioProps,
                audioRef,
                beatmapset,
                data,
                isBeatmapsPopupVisible,
                isPanelExpanded,
                isPlaying,
                isPlayUiActive,
                isPreviewLoading,
                mobileExpanded,
                playUiVisibilityClassName,
                progress,
                rootProps: {
                    onMouseLeave: data.canShowPopup ? beatmapsPopupDelayedHide : undefined,
                    ref: blockRef,
                },
                setMobileExpanded,
                togglePreview,
                triggerProps: {
                    onMouseEnter: data.canShowPopup ? beatmapsPopupDelayedShow : undefined,
                    onMouseLeave: data.canShowPopup ? beatmapsPopupDelayedHide : undefined,
                },
            })}

            {typeof document !== "undefined" && popupPosition != null && data.canShowPopup
                ? createPortal(
                    <div
                        className={cn(
                            "pointer-events-none absolute z-40 w-full rounded-b-lg bg-osu-b3 pt-1.25 pb-2.5 opacity-0 transition-opacity duration-150 md:pt-2.5",
                            isBeatmapsPopupVisible && "pointer-events-auto opacity-100",
                        )}
                        onMouseEnter={beatmapsPopupKeep}
                        onMouseLeave={beatmapsPopupDelayedHide}
                        ref={popupRef}
                        style={popupStyle}
                    >
                        <div
                            aria-hidden
                            className="pointer-events-none absolute bottom-0 left-0 w-full rounded-lg ring-2 ring-inset ring-osu-h1 shadow-[0_18px_34px_rgba(0,0,0,0.36)]"
                            style={{height: `calc(100% + ${popupPosition.panelHeight})`}}
                        />
                        <div className="relative z-10 grid max-h-[50vh] gap-2.5 overflow-auto px-3 py-0.5">
                            {data.visibleBeatmapGroups.map(([mode, beatmaps]) => (
                                <div className="grid gap-0.5" key={mode}>
                                    {beatmaps.map((beatmap) => (
                                        <BeatmapPopupRow
                                            beatmap={beatmap}
                                            beatmapsetId={beatmapset.id}
                                            key={beatmap.id}
                                        />
                                    ))}
                                </div>
                            ))}
                        </div>
                    </div>,
                    document.body,
                )
                : null}
        </>
    );
}

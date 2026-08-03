"use client";

import { useCallback, useEffect, useId, useMemo, useRef, useState } from "react";
import { ChevronUp, Pause, Play, RotateCcw } from "lucide-react";
import { getTrackBackground, Range } from "react-range";
import {
  beatmapInfoSliderClassNames,
  beatmapInfoSliderTrackColors,
} from "@/components/beatmapsets-show/slider-theme";
import { Button } from "@/components/ui/button";
import { Skeleton } from "@/components/ui/skeleton";
import type { Ruleset } from "@/lib/rankings";
import { cn } from "@/lib/utils";

type BeatmapPreviewMetadata = {
  artist: string;
  beatmapSetId: number;
  creator: string;
  title: string;
  version: string;
};

type BeatmapPreviewerInstance = {
  getMetadata(): BeatmapPreviewMetadata | null;
  getPreviewTime(): number;
  getTotalLength(): number;
  loadBeatmapFromString(data: string, mods?: number): Promise<void> | void;
  loadBeatmapFromUrl(url: string, mods?: number): Promise<void>;
  render(time: number): void;
  resize(): void;
};

type BeatmapCanvasPreviewProps = {
  autoPlay?: boolean;
  backgroundUrl?: string | null;
  beatmapId: number;
  difficultyName: string;
  fallbackArtist: string;
  fallbackCreator: string;
  fallbackTitle: string;
  initialBeatmapFile?: string | null;
  mode: Ruleset;
  onCollapse?: () => void;
};

const defaultLoopLength = 30000;

function sanitizeId(value: string) {
  return value.replace(/[^a-zA-Z0-9_-]/g, "");
}

function formatPlaybackTime(value: number) {
  const totalSeconds = Math.max(0, Math.floor(value / 1000));
  const minutes = Math.floor(totalSeconds / 60);
  const seconds = totalSeconds % 60;
  return `${minutes}:${seconds.toString().padStart(2, "0")}`;
}

function normalizePreviewStart(previewTime: number, totalLength: number) {
  if (!Number.isFinite(previewTime) || previewTime <= 0) {
    return 0;
  }

  if (!Number.isFinite(totalLength) || totalLength <= 0) {
    return previewTime;
  }

  return Math.min(previewTime, Math.max(totalLength - 1, 0));
}

export function BeatmapCanvasPreview({
  autoPlay = false,
  backgroundUrl,
  beatmapId,
  difficultyName,
  fallbackArtist,
  fallbackCreator,
  fallbackTitle,
  initialBeatmapFile,
  mode,
  onCollapse,
}: BeatmapCanvasPreviewProps) {
  const canvasRef = useRef<HTMLCanvasElement>(null);
  const animationFrameRef = useRef<number | null>(null);
  const baseTimeRef = useRef(0);
  const currentTimeRef = useRef(0);
  const isPlayingRef = useRef(false);
  const previewStartRef = useRef(0);
  const previewerRef = useRef<BeatmapPreviewerInstance | null>(null);
  const startedAtRef = useRef(0);
  const totalLengthRef = useRef(defaultLoopLength);
  const rawId = useId();
  const canvasId = useMemo(() => `beatmap-preview-${sanitizeId(rawId)}`, [rawId]);
  const [currentTime, setCurrentTime] = useState(0);
  const [error, setError] = useState<string | null>(null);
  const [isPlaying, setIsPlaying] = useState(false);
  const [loading, setLoading] = useState(true);
  const [metadata, setMetadata] = useState<BeatmapPreviewMetadata | null>(null);
  const [totalLength, setTotalLength] = useState(defaultLoopLength);

  const pausePlayback = useCallback(() => {
    if (!isPlayingRef.current) {
      return;
    }

    const elapsed = performance.now() - startedAtRef.current;
    const nextTime = Math.min(baseTimeRef.current + elapsed, totalLengthRef.current);
    baseTimeRef.current = nextTime;
    currentTimeRef.current = nextTime;
    setCurrentTime(nextTime);
    isPlayingRef.current = false;
    setIsPlaying(false);

    if (animationFrameRef.current != null) {
      cancelAnimationFrame(animationFrameRef.current);
      animationFrameRef.current = null;
    }
  }, []);

  const seekTo = useCallback((value: number) => {
    const nextTime = Math.max(0, Math.min(value, totalLengthRef.current));
    currentTimeRef.current = nextTime;
    baseTimeRef.current = nextTime;
    startedAtRef.current = performance.now();
    setCurrentTime(nextTime);
    previewerRef.current?.render(nextTime);
  }, []);

  const playFromCurrentTime = useCallback(() => {
    if (previewerRef.current == null || isPlayingRef.current) {
      return;
    }

    if (currentTimeRef.current >= totalLengthRef.current) {
      seekTo(previewStartRef.current);
    }

    isPlayingRef.current = true;
    setIsPlaying(true);
    startedAtRef.current = performance.now();

    const tick = (now: number) => {
      if (!isPlayingRef.current || previewerRef.current == null) {
        return;
      }

      const nextTime = baseTimeRef.current + (now - startedAtRef.current);
      if (nextTime >= totalLengthRef.current) {
        currentTimeRef.current = totalLengthRef.current;
        baseTimeRef.current = totalLengthRef.current;
        setCurrentTime(totalLengthRef.current);
        previewerRef.current.render(totalLengthRef.current);
        isPlayingRef.current = false;
        setIsPlaying(false);
        animationFrameRef.current = null;
        return;
      }

      currentTimeRef.current = nextTime;
      setCurrentTime(nextTime);
      previewerRef.current.render(nextTime);
      animationFrameRef.current = requestAnimationFrame(tick);
    };

    animationFrameRef.current = requestAnimationFrame(tick);
  }, [seekTo]);

  useEffect(() => {
    let disposed = false;

    if (animationFrameRef.current != null) {
      cancelAnimationFrame(animationFrameRef.current);
      animationFrameRef.current = null;
    }

    baseTimeRef.current = 0;
    previewerRef.current = null;
    currentTimeRef.current = 0;
    isPlayingRef.current = false;
    previewStartRef.current = 0;
    startedAtRef.current = 0;
    totalLengthRef.current = defaultLoopLength;

    async function loadPreview() {
      try {
        const previewModule = (await import("@/lib/beatmap-preview/previewers/StandardBeatmapPreviewer")) as {
          default: new (id: string) => BeatmapPreviewerInstance;
        };

        if (disposed || canvasRef.current == null) {
          return;
        }

        const previewer = new previewModule.default(canvasId);
        previewerRef.current = previewer;

        if (initialBeatmapFile != null) {
          await previewer.loadBeatmapFromString(initialBeatmapFile);
        } else {
          await previewer.loadBeatmapFromUrl(`/api/beatmaps/${beatmapId}/file`);
        }

        if (disposed || previewerRef.current !== previewer) {
          return;
        }

        const nextMetadata = previewer.getMetadata();
        const nextTotalLength = Math.max(0, previewer.getTotalLength());
        const resolvedTotalLength = nextTotalLength > 0 ? nextTotalLength : defaultLoopLength;
        const previewStart = normalizePreviewStart(previewer.getPreviewTime(), resolvedTotalLength);

        previewStartRef.current = previewStart;
        totalLengthRef.current = resolvedTotalLength;
        baseTimeRef.current = previewStart;
        currentTimeRef.current = previewStart;
        setMetadata(nextMetadata);
        setCurrentTime(previewStart);
        setLoading(false);
        setTotalLength(resolvedTotalLength);
        previewer.render(previewStart);
        if (autoPlay) {
          playFromCurrentTime();
        }
      } catch (loadError) {
        if (disposed) {
          return;
        }

        console.error("Failed to load beatmap preview", loadError);
        setMetadata(null);
        setError("Preview unavailable for this difficulty.");
        setLoading(false);
      }
    }

    void loadPreview();

    return () => {
      disposed = true;

      if (animationFrameRef.current != null) {
        cancelAnimationFrame(animationFrameRef.current);
        animationFrameRef.current = null;
      }

      isPlayingRef.current = false;
      previewerRef.current = null;
    };
  }, [autoPlay, beatmapId, canvasId, initialBeatmapFile, playFromCurrentTime]);

  useEffect(() => {
    const canvas = canvasRef.current;
    if (canvas == null) {
      return;
    }

    const observer = new ResizeObserver(() => {
      previewerRef.current?.resize();
      previewerRef.current?.render(currentTimeRef.current);
    });

    observer.observe(canvas);
    return () => observer.disconnect();
  }, []);

  const displayArtist = metadata?.artist ?? fallbackArtist;
  const displayCreator = metadata?.creator ?? fallbackCreator;
  const displayDifficulty = metadata?.version ?? difficultyName;
  const displayTitle = metadata?.title ?? fallbackTitle;
  const sliderValues = useMemo(
    () => [(!Number.isFinite(totalLength) || totalLength <= 0 ? 0 : Math.max(0, Math.min(currentTime / totalLength, 1)))],
    [currentTime, totalLength],
  );
  const resolvedBackgroundUrl =
    backgroundUrl ??
    (metadata != null && metadata.beatmapSetId > 0
      ? `https://assets.ppy.sh/beatmaps/${metadata.beatmapSetId}/covers/cover.jpg`
      : null);

  return (
    <div className="overflow-hidden rounded-2xl bg-osu-b4 shadow-[0_12px_28px_rgba(0,0,0,0.28)]">
      <div className="relative aspect-video min-h-56 w-full overflow-hidden bg-osu-b6">
        {resolvedBackgroundUrl != null ? (
          <>
            {/* eslint-disable-next-line @next/next/no-img-element */}
            <img
              alt=""
              className="absolute inset-0 h-full w-full scale-105 object-cover opacity-30 blur-md"
              src={resolvedBackgroundUrl}
            />
            <div className="absolute inset-0 bg-[linear-gradient(to_bottom,rgba(0,0,0,0.72),rgba(0,0,0,0.18)_38%,rgba(0,0,0,0.86))]" />
          </>
        ) : (
          <div className="absolute inset-0 bg-[linear-gradient(180deg,rgba(38,38,38,0.92),rgba(7,7,7,0.96))]" />
        )}

        <div className="absolute top-4 left-4 z-20 flex max-w-[min(34rem,calc(100%-10rem))] flex-col drop-shadow-lg">
          <div className="line-clamp-2 text-xl leading-tight font-bold text-white sm:text-2xl">
            {displayArtist} - {displayTitle}
          </div>
          <div className="mt-1 text-sm font-medium text-white/88 sm:text-base">
            <span className="text-white/60">Mapped by</span> {displayCreator}
          </div>
        </div>

        <div className="absolute top-4 right-4 z-20 flex flex-col items-end gap-2">
          <div className="rounded-md bg-black/45 px-3 py-2 text-white shadow-[0_12px_28px_rgba(0,0,0,0.3)] backdrop-blur-md">
            <div className="text-[11px] text-white/55">Difficulty</div>
            <div className="mt-1 flex items-center gap-2 text-sm font-semibold">
              <div className={`fa-extra-mode-${mode} text-lg leading-none`} />
              <span className="max-w-44 truncate">{displayDifficulty}</span>
            </div>
          </div>
        </div>

        <div className="absolute bottom-4 left-4 z-20 text-xs font-semibold text-white/55">
          Beatmap preview
        </div>

        {onCollapse != null ? (
          <Button
            aria-label="Collapse preview"
            className="absolute right-4 bottom-4 z-40 rounded-full bg-black/45 text-white shadow-[0_12px_28px_rgba(0,0,0,0.3)] backdrop-blur-md hover:bg-black/65 focus-visible:bg-black/65 active:bg-black/70"
            onClick={onCollapse}
            size="icon-sm"
            title="Collapse preview"
            type="button"
            variant="ghost"
          >
            <ChevronUp className="h-4 w-4" />
          </Button>
        ) : null}

        {loading ? (
          <div className="absolute inset-0 z-30 bg-black/50 backdrop-blur-sm">
            <div className="flex h-full flex-col justify-between p-4">
              <div className="flex items-start justify-between gap-4">
                <div className="flex max-w-[min(32rem,calc(100%-7rem))] flex-col gap-2">
                  <Skeleton className="h-8 w-80 max-w-full bg-white/15" />
                  <Skeleton className="h-5 w-48 max-w-[70%] bg-white/10" />
                </div>
                <div className="flex flex-col items-end gap-2">
                  <Skeleton className="h-10 w-48 bg-white/15" />
                </div>
              </div>
              <div className="flex flex-col items-center gap-2 pb-6">
                <Skeleton className="h-4 w-28 bg-white/10" />
                <span className="text-sm text-white/80">Loading beatmap...</span>
              </div>
            </div>
          </div>
        ) : null}

        {error != null ? (
          <div className="absolute inset-0 z-30 flex items-center justify-center bg-black/55 px-6 text-center text-sm font-medium text-white/72 backdrop-blur-sm">
            {error}
          </div>
        ) : null}

        <canvas id={canvasId} ref={canvasRef} className="absolute inset-0 z-10 h-full w-full" />
      </div>

      <div className="grid gap-3 bg-osu-b4 px-4 py-3 sm:grid-cols-[4rem_minmax(0,1fr)_7.125rem] sm:items-center">
        <div className="flex items-center gap-2 sm:w-16">
          <Button
            aria-label={isPlaying ? "Pause preview" : "Play preview"}
            className="shadow-none"
            onClick={() => {
              if (isPlaying) {
                pausePlayback();
              } else {
                playFromCurrentTime();
              }
            }}
            size="icon-sm"
            type="button"
            variant="secondary"
          >
            {isPlaying ? <Pause className="h-4 w-4" /> : <Play className="h-4 w-4 fill-current" />}
          </Button>
          <Button
            aria-label="Restart preview"
            className="shadow-none"
            onClick={() => {
              seekTo(previewStartRef.current);
              playFromCurrentTime();
            }}
            size="icon-sm"
            type="button"
            variant="secondary"
          >
            <RotateCcw className="h-4 w-4" />
          </Button>
        </div>

        <div className="min-w-0">
          <Range
            label="Beatmap preview seek"
            max={1}
            min={0}
            onChange={(values) => {
              seekTo((values[0] ?? 0) * totalLength);
            }}
            step={0.001}
            values={sliderValues}
            renderThumb={({ props, isDragged }) => (
              <div
                {...props}
                key={props.key}
                className="flex items-center justify-center rounded-full"
                style={props.style}
              >
                <div
                  className={cn(
                    "flex h-4 w-10 items-center justify-center rounded-full shadow-sm ring-2 transition-all duration-200",
                    beatmapInfoSliderClassNames.thumb,
                    isDragged ? beatmapInfoSliderClassNames.thumbActive : beatmapInfoSliderClassNames.thumbHover,
                  )}
                >
                  <div className={cn("mx-0.5 h-2 w-0.5 rounded-full", beatmapInfoSliderClassNames.valueAccent)} />
                  <div className={cn("mx-0.5 h-2 w-0.5 rounded-full", beatmapInfoSliderClassNames.valueAccent)} />
                </div>
              </div>
            )}
            renderTrack={({ props, children }) => (
              <div
                className="group flex w-full grow items-center justify-center bg-transparent px-4 py-3 sm:px-6"
                onMouseDown={props.onMouseDown}
                onTouchStart={props.onTouchStart}
                style={props.style}
              >
                <div
                  ref={props.ref}
                  className="h-1.5 w-full self-center rounded-full"
                  style={{
                    background: getTrackBackground({
                      colors: [beatmapInfoSliderTrackColors.value, beatmapInfoSliderTrackColors.remainder],
                      max: 1,
                      min: 0,
                      values: sliderValues,
                    }),
                  }}
                >
                  {children}
                </div>
              </div>
            )}
          />
        </div>

        <div className="w-[7.125rem] justify-self-end text-right font-mono text-sm font-semibold tabular-nums text-white/82">
          {formatPlaybackTime(currentTime)} / {formatPlaybackTime(totalLength)}
        </div>
      </div>
    </div>
  );
}

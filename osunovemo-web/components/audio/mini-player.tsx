"use client";

import Link from "next/link";
import { Music, Pause, Play, X } from "lucide-react";
import { useMiniPlayer } from "@/components/audio/mini-player-provider";
import { cn } from "@/lib/utils";

function formatTime(seconds: number) {
  if (!Number.isFinite(seconds) || seconds < 0) {
    return "0:00";
  }

  const whole = Math.floor(seconds);

  return `${Math.floor(whole / 60)}:${String(whole % 60).padStart(2, "0")}`;
}

export function MiniPlayer() {
  const player = useMiniPlayer();

  if (player?.current == null) {
    return null;
  }

  const { current, duration, isPlaying, pause, position, resume, seek, stop } = player;
  const ratio = duration > 0 ? Math.min(1, position / duration) : 0;

  return (
    <div className="pointer-events-none fixed inset-x-0 bottom-0 z-100 flex justify-center p-3">
      <div className="pointer-events-auto flex w-full max-w-3xl items-center gap-3 rounded-xl bg-osu-b4/95 px-3 py-2 shadow-[0_12px_32px_rgba(0,0,0,0.45)] backdrop-blur">
        <span
          className="size-10 shrink-0 rounded-md bg-osu-b5 bg-cover bg-center"
          style={
            current.coverUrl == null
              ? undefined
              : { backgroundImage: `url(${JSON.stringify(current.coverUrl)})` }
          }
        >
          {current.coverUrl == null ? (
            <span className="flex size-full items-center justify-center text-osu-f1">
              <Music aria-hidden className="size-5" />
            </span>
          ) : null}
        </span>

        <button
          aria-label={isPlaying ? "pause preview" : "play preview"}
          className="flex size-9 shrink-0 items-center justify-center rounded-full bg-osu-h2 text-white transition-colors hover:bg-osu-h1"
          onClick={() => (isPlaying ? pause() : resume())}
          type="button"
        >
          {isPlaying ? (
            <Pause aria-hidden className="size-4 fill-current" />
          ) : (
            <Play aria-hidden className="size-4 fill-current" />
          )}
        </button>

        <div className="min-w-0 flex-1">
          <div className="flex items-baseline gap-2">
            {current.href == null ? (
              <span className="truncate text-sm font-semibold text-white">{current.title}</span>
            ) : (
              <Link
                className="truncate text-sm font-semibold text-white hover:underline"
                href={current.href}
              >
                {current.title}
              </Link>
            )}
            <span className="truncate text-xs text-osu-f1">{current.artist}</span>
          </div>

          <div className="mt-1 flex items-center gap-2">
            <span className="w-8 shrink-0 text-right text-[10px] tabular-nums text-osu-f1">
              {formatTime(position)}
            </span>
            <input
              aria-label="seek"
              className={cn(
                "h-1 min-w-0 flex-1 cursor-pointer appearance-none rounded-full bg-osu-b6",
                "[&::-webkit-slider-thumb]:size-2.5 [&::-webkit-slider-thumb]:appearance-none",
                "[&::-webkit-slider-thumb]:rounded-full [&::-webkit-slider-thumb]:bg-osu-h1",
              )}
              max={1}
              min={0}
              onChange={(event) => seek(Number(event.currentTarget.value))}
              step={0.001}
              style={{
                background: `linear-gradient(to right, var(--color-osu-h1) ${ratio * 100}%, var(--color-osu-b6) ${ratio * 100}%)`,
              }}
              type="range"
              value={ratio}
            />
            <span className="w-8 shrink-0 text-[10px] tabular-nums text-osu-f1">
              {formatTime(duration)}
            </span>
          </div>
        </div>

        <button
          aria-label="close player"
          className="flex size-8 shrink-0 items-center justify-center rounded-full text-osu-f1 transition-colors hover:bg-white/10 hover:text-white"
          onClick={stop}
          type="button"
        >
          <X aria-hidden className="size-4" />
        </button>
      </div>
    </div>
  );
}

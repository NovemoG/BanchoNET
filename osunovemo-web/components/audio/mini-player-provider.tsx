"use client";

import {
  createContext,
  type ReactNode,
  useCallback,
  useContext,
  useEffect,
  useMemo,
  useRef,
  useState,
} from "react";

export type MiniPlayerTrack = {
  artist: string;
  coverUrl: string | null;
  href: string | null;
  id: number;
  title: string;
};

type MiniPlayerContextValue = {
  current: MiniPlayerTrack | null;
  duration: number;
  isPlaying: boolean;
  pause: () => void;
  position: number;
  resume: () => void;
  seek: (ratio: number) => void;
  stop: () => void;
  /** Called by whichever card just started playing; it hands over its own audio element. */
  startPlayback: (audio: HTMLAudioElement, track: MiniPlayerTrack) => void;
  /** Called when that card stops, so the bar does not outlive the sound. */
  endPlayback: (audio: HTMLAudioElement) => void;
};

const MiniPlayerContext = createContext<MiniPlayerContextValue | null>(null);

/**
 * Adopts the audio element of whichever beatmapset card is currently playing rather than owning
 * one of its own. The cards already each render an <audio> for their inline play button, so this
 * avoids either duplicating playback or rewiring all six panel variants.
 */
export function MiniPlayerProvider({ children }: { children: ReactNode }) {
  const audioRef = useRef<HTMLAudioElement | null>(null);
  const [current, setCurrent] = useState<MiniPlayerTrack | null>(null);
  const [isPlaying, setIsPlaying] = useState(false);
  const [position, setPosition] = useState(0);
  const [duration, setDuration] = useState(0);

  const startPlayback = useCallback((audio: HTMLAudioElement, track: MiniPlayerTrack) => {
    // Only one preview at a time, so hand over cleanly if another card was playing.
    if (audioRef.current != null && audioRef.current !== audio) {
      audioRef.current.pause();
      audioRef.current.currentTime = 0;
    }

    audioRef.current = audio;
    setCurrent(track);
    setIsPlaying(true);
    setDuration(Number.isFinite(audio.duration) ? audio.duration : 0);
  }, []);

  const endPlayback = useCallback((audio: HTMLAudioElement) => {
    if (audioRef.current !== audio) {
      return;
    }

    setIsPlaying(false);
  }, []);

  useEffect(() => {
    const audio = audioRef.current;

    if (audio == null || current == null) {
      return undefined;
    }

    const handleTimeUpdate = () => {
      setPosition(audio.currentTime);
      setDuration(Number.isFinite(audio.duration) ? audio.duration : 0);
    };
    const handlePlay = () => setIsPlaying(true);
    const handlePause = () => setIsPlaying(false);

    audio.addEventListener("timeupdate", handleTimeUpdate);
    audio.addEventListener("play", handlePlay);
    audio.addEventListener("pause", handlePause);

    return () => {
      audio.removeEventListener("timeupdate", handleTimeUpdate);
      audio.removeEventListener("play", handlePlay);
      audio.removeEventListener("pause", handlePause);
    };
  }, [current]);

  const value = useMemo<MiniPlayerContextValue>(
    () => ({
      current,
      duration,
      endPlayback,
      isPlaying,
      pause: () => audioRef.current?.pause(),
      position,
      resume: () => void audioRef.current?.play().catch(() => undefined),
      seek: (ratio) => {
        const audio = audioRef.current;

        if (audio == null || !Number.isFinite(audio.duration)) {
          return;
        }

        audio.currentTime = Math.max(0, Math.min(1, ratio)) * audio.duration;
      },
      startPlayback,
      stop: () => {
        const audio = audioRef.current;

        if (audio != null) {
          audio.pause();
          audio.currentTime = 0;
        }

        audioRef.current = null;
        setCurrent(null);
        setIsPlaying(false);
        setPosition(0);
      },
    }),
    [current, duration, endPlayback, isPlaying, position, startPlayback],
  );

  return <MiniPlayerContext.Provider value={value}>{children}</MiniPlayerContext.Provider>;
}

/** Null outside the provider, so cards rendered in isolation keep working. */
export function useMiniPlayer() {
  return useContext(MiniPlayerContext);
}

import Link from "next/link";
import type {BeatmapsetShowBeatmap} from "@/lib/beatmapset-types";
import {cn} from "@/lib/utils";

type BeatmapPickerProps = {
    beatmaps: BeatmapsetShowBeatmap[];
    currentBeatmapId: number;
    currentMode: BeatmapsetShowBeatmap["mode"];
    beatmapsetId: number;
};

export function BeatmapPicker({
                                  beatmaps,
                                  currentBeatmapId,
                                  currentMode,
                                  beatmapsetId,
                              }: BeatmapPickerProps) {
    const visibleBeatmaps = beatmaps.filter((beatmap) => beatmap.mode === currentMode);

    return (
        <div className="flex flex-wrap items-center gap-2">
            {visibleBeatmaps.map((beatmap) => {
                const active = beatmap.id === currentBeatmapId;

                return (
                    <Link
                        className={cn(
                            "inline-flex h-10 w-10 items-center justify-center rounded-full border text-base transition-colors",
                            active
                                ? "border-osu-h1 bg-osu-h1 text-osu-b6"
                                : "border-white/10 bg-white/5 text-osu-h1 hover:border-white/20 hover:bg-white/10 hover:text-white",
                        )}
                        href={`/beatmapsets/${beatmapsetId}?beatmap=${beatmap.id}`}
                        key={beatmap.id}
                        title={beatmap.version}
                    >
                        <span className={`fa-extra-mode-${beatmap.mode}`}/>
                        <span className="sr-only">{beatmap.version}</span>
                    </Link>
                );
            })}
        </div>
    );
}

"use client";

import {useMemo} from "react";
import {Slider} from "@/components/ui/slider";
import {scoreHighlightOptions} from "@/lib/score-highlight";

type HighlightPeriodSelectorProps = {
    selectedIndex: number[];
    onSelectedIndexChange: (value: number[]) => void;
};

export function HighlightPeriodSelector({
    selectedIndex,
    onSelectedIndexChange,
}: HighlightPeriodSelectorProps) {
    const selectedOption = useMemo(
        () => scoreHighlightOptions[selectedIndex[0]] ?? scoreHighlightOptions[0],
        [selectedIndex],
    );

    return (
        <div className="flex min-w-60 items-center gap-3 rounded-sm bg-black/15 px-3 py-1.5 text-xs text-osu-f1 max-sm:w-full">
            <span className="shrink-0 text-white/60">Highlight</span>
            <Slider
                className="w-28 sm:w-40"
                max={scoreHighlightOptions.length - 1}
                min={0}
                onValueChange={onSelectedIndexChange}
                step={1}
                value={selectedIndex}
            />
            <span className="w-12 text-right font-semibold text-white">{selectedOption.label}</span>
        </div>
    );
}

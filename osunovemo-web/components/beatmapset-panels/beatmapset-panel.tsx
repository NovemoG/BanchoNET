"use client";

import {BeatmapsetPanelCover} from "@/components/beatmapset-panels/beatmapset-panel-cover";
import {BeatmapsetPanelExtra} from "@/components/beatmapset-panels/beatmapset-panel-extra";
import {BeatmapsetPanelList} from "@/components/beatmapset-panels/beatmapset-panel-list";
import {BeatmapsetPanelMini} from "@/components/beatmapset-panels/beatmapset-panel-mini";
import {BeatmapsetPanelNano} from "@/components/beatmapset-panels/beatmapset-panel-nano";
import {BeatmapsetPanelNormal} from "@/components/beatmapset-panels/beatmapset-panel-normal";
import type {BeatmapsetPanelProps} from "@/components/beatmapset-panels/shared";

export {type BeatmapsetPanelProps, type BeatmapsetPanelSize} from "@/components/beatmapset-panels/shared";

export function BeatmapsetPanel({
    beatmapset,
    className,
    size = "normal",
}: BeatmapsetPanelProps) {
    if (size === "cover") {
        return <BeatmapsetPanelCover beatmapset={beatmapset} className={className}/>;
    }

    if (size === "nano") {
        return <BeatmapsetPanelNano beatmapset={beatmapset} className={className}/>;
    }

    if (size === "mini") {
        return <BeatmapsetPanelMini beatmapset={beatmapset} className={className}/>;
    }

    if (size === "extra") {
        return <BeatmapsetPanelExtra beatmapset={beatmapset} className={className}/>;
    }

    if (size === "list") {
        return <BeatmapsetPanelList beatmapset={beatmapset} className={className}/>;
    }

    return <BeatmapsetPanelNormal beatmapset={beatmapset} className={className}/>;
}

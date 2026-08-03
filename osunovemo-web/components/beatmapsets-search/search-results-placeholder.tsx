import {LoaderCircle} from "lucide-react";
import {BeatmapsetPanel} from "@/components/beatmapset-panels/beatmapset-panel";
import {type BeatmapsetSearchView} from "@/lib/beatmapset-search";
import type {Beatmapset} from "@/lib/profile";

export function BeatmapsetSearchCardPreview({
    beatmapset,
    view,
}: {
    beatmapset: Beatmapset;
    view: BeatmapsetSearchView;
}) {
    if (view === "list") {
        return <BeatmapsetPanel beatmapset={beatmapset} size="list"/>;
    }

    if (view === "cover") {
        return <BeatmapsetPanel beatmapset={beatmapset} size="cover"/>;
    }

    return <BeatmapsetPanel beatmapset={beatmapset} size={view}/>;
}

export function BeatmapsetSearchResultsPlaceholder({view}: { view: BeatmapsetSearchView }) {
    return (
        <div
            aria-label="Loading beatmapsets"
            className="flex min-h-36 items-center justify-center py-10 text-osu-f1"
            data-view={view}
            role="status"
        >
            <LoaderCircle aria-hidden className="h-6 w-6 animate-spin"/>
            <span className="sr-only">Loading beatmapsets</span>
        </div>
    );
}

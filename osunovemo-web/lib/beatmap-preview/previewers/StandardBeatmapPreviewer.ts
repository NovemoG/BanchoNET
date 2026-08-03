import BeatmapPreviewer from "./BeatmapPreviewer";
import {StandardBeatmap, StandardRuleset} from "osu-standard-stable"
import StandardRenderer from "../renderers/StandardRenderer";

export default class StandardBeatmapPreviewer extends BeatmapPreviewer<StandardBeatmap, StandardRenderer> {
    constructor(id: string) {
        super(id, new StandardRuleset(), (ctx, beatmap) => new StandardRenderer(ctx, beatmap));
    }
}


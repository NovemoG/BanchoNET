import {HitObject, Ruleset, RulesetBeatmap} from "osu-classes";
import Renderer from "../renderers/Renderer";
import DrawableHitObject from "../drawables/DrawableHitObject";
import {BeatmapDecoder} from "osu-parsers";

export default abstract class BeatmapPreviewer<TBeatmap extends RulesetBeatmap, TRenderer extends Renderer<TBeatmap, DrawableHitObject<HitObject>>> {
    protected readonly canvas: HTMLCanvasElement;
    protected readonly ctx: CanvasRenderingContext2D;

    private decoder: BeatmapDecoder;
    private renderer!: TRenderer;

    private startTime: number = 0;

    protected constructor(
        private id: string,
        private ruleset: Ruleset,
        private createRenderer: (ctx: CanvasRenderingContext2D, beatmap: TBeatmap) => TRenderer
    ) {
        this.canvas = document.querySelector(`#${id}`) as HTMLCanvasElement;

        if (!this.canvas) {
            this.canvas = document.createElement("canvas") as HTMLCanvasElement;
            this.canvas.setAttribute("id", this.id);
            document.body.appendChild(this.canvas);
        }

        if (!(this.canvas instanceof HTMLCanvasElement)) {
            throw new Error("Queried element is not a HTMLCanvasElement");
        }

        const ctx = this.canvas.getContext("2d");
        if (!ctx) {
            throw new Error("Canvas not supported");
        }

        this.ctx = ctx;
        this.resize();

        this.startTime = performance.now();
        this.decoder = new BeatmapDecoder();
    }

    public resize() {
        const width = this.canvas.clientWidth || 640;
        const height = this.canvas.clientHeight || 480;

        if (this.canvas.width !== width || this.canvas.height !== height) {
            this.canvas.width = width;
            this.canvas.height = height;
        }
    }

    public get getBeatmap() {
        return this.renderer?.getBeatmap;
    }

    public async loadBeatmapFromUrl(url: string, mods: number = 0) {
        const response = await fetch(url, {cache: "no-store"});

        if (!response.ok) {
            throw new Error(`Failed to load beatmap preview from ${url}`);
        }

        const data = await response.text();
        this.loadBeatmapFromString(data, mods);
    }

    public loadBeatmapFromString(data: string, mods: number = 0) {
        const rawBeatmap = this.decoder.decodeFromString(data);
        const appliedMods = this.ruleset.createModCombination(mods);
        const appliedBeatmap = this.ruleset.applyToBeatmapWithMods(rawBeatmap, appliedMods) as TBeatmap;

        this.renderer = this.createRenderer(this.ctx, appliedBeatmap);
    }

    public async loadBeatmap(beatmap: TBeatmap, mods: number = 0) {
        const appliedMods = this.ruleset.createModCombination(mods);
        const appliedBeatmap = this.ruleset.applyToBeatmapWithMods(beatmap, appliedMods) as TBeatmap;

        this.renderer = this.createRenderer(this.ctx, appliedBeatmap);
    }

    public render(time: number) {
        this.clearScreen();

        if (!this.renderer) return;

        const scaleX = this.canvas.width / 640;
        const scaleY = this.canvas.height / 480;
        const scale = Math.min(scaleX, scaleY);

        const offsetX = (this.canvas.width - 640 * scale) / 2;
        const offsetY = (this.canvas.height - 480 * scale) / 2;

        this.ctx.save();
        this.ctx.translate(offsetX + 64 * scale, offsetY + 48 * scale);
        this.ctx.scale(scale, scale);
        this.renderer.render(time);
        this.ctx.restore();
    }

    public getMetadata() {
        const metadata = this.renderer?.getBeatmap?.metadata;
        if (!metadata) return null;

        return {
            artist: metadata.artist,
            title: metadata.title,
            creator: metadata.creator,
            version: metadata.version,
            beatmapSetId: metadata.beatmapSetId
        };
    }

    public getPreviewTime() {
        const previewTime = this.renderer?.getBeatmap?.general?.previewTime || 0;
        const clockRate = this.renderer?.getBeatmap?.difficulty?.clockRate || 1;

        return previewTime / clockRate;
    }

    public getTotalLength() {
        const beatmap = this.renderer?.getBeatmap as unknown as RulesetBeatmap & { totalLength?: number };
        if (!beatmap) return 0;
        
        return beatmap.totalLength || 0;
    }

    private clearScreen() {
        this.ctx.clearRect(0, 0, this.canvas.width, this.canvas.height);
    }
}

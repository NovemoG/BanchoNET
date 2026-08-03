export default abstract class Renderer<TBeatmap, THitObject> {
    protected ctx: CanvasRenderingContext2D;
    protected beatmap: TBeatmap;
    protected drawableHitObjects: THitObject[] = [];
    protected time_multiplier: number = 1;

    get getBeatmap(): Readonly<TBeatmap> {
        return this.beatmap;
    }

    constructor(ctx: CanvasRenderingContext2D, beatmap: TBeatmap) {
        this.ctx = ctx;
        this.beatmap = beatmap;
        this.initializeDrawableHitObjects();
    }

    /**
     * Initializes drawable hit objects for the specific renderer.
     */
    protected abstract initializeDrawableHitObjects(): void;

    /**
     * Renders the frame for the given time.
     */
    public abstract render(time: number): void;
}


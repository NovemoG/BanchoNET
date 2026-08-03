import {DrawableStandardHitObject} from "./DrawableStandardHitObject";
import {Slider, SliderTail} from "osu-standard-stable";
import {DrawableSlider} from "./DrawableSlider";

export class DrawableSliderTail extends DrawableStandardHitObject<SliderTail> {
    get slider(): Slider {
        return this.drawableSlider?.hitObject;
    }

    protected get drawableSlider() {
        return this.parentHitObject as DrawableSlider;
    }

    constructor(hitObject: SliderTail) {
        super(hitObject);
    }

    draw(ctx: CanvasRenderingContext2D, time: number) {
        void ctx;
        void time;
        // Slider tail is not explicitly drawn in standard ruleset, but its logic and hit detection exist.
    }

    opacity(time: number): number {
        let opacity = super.opacity(time);

        if (time > this.hitObject.startTime) {
            opacity = 1 - (time - this.hitObject.startTime) / this.HIT_DURATION;
        }

        return opacity;
    }
}

import {DrawableCircle} from "./DrawableCircle";
import {Color4} from "osu-classes";
import {Slider} from "osu-standard-stable";

export class DrawableSliderHead extends DrawableCircle {
    color: Color4;

    constructor(hitObject: Slider, color: Color4) {
        super(hitObject, color);
        this.color = color;
    }

    draw(ctx: CanvasRenderingContext2D, time: number) {
        super.draw(ctx, time);
    }
}


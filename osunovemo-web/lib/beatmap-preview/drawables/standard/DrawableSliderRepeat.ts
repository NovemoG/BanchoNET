import {DrawableStandardHitObject} from "./DrawableStandardHitObject";
import {Slider, SliderRepeat} from "osu-standard-stable";
import {DrawableSlider} from "./DrawableSlider";
import {CIRCLE_BORDER_WIDTH} from "../../renderers/StandardRenderer";

export class DrawableSliderRepeat extends DrawableStandardHitObject<SliderRepeat> {
    get slider(): Slider {
        return this.drawableSlider?.hitObject;
    }

    protected get drawableSlider() {
        return this.parentHitObject as DrawableSlider;
    }

    private animDuration: number;

    constructor(hitObject: SliderRepeat) {
        super(hitObject);
        this.animDuration = Math.min(300, hitObject.spanDuration)
    }

    draw(ctx: CanvasRenderingContext2D, time: number) {
        const {radius, stackedStartPosition} = this.hitObject;
        const {x, y} = stackedStartPosition;

        const opacity = this.opacity(time);
        if (opacity <= 0) return;


        const renderedRadius = radius * (50 / 58) - 2;

        ctx.save();
        ctx.translate(x, y);

        ctx.lineWidth = renderedRadius * CIRCLE_BORDER_WIDTH;
        ctx.strokeStyle = `rgba(255,255,255,${opacity})`;
        ctx.fillStyle = `rgba(${this.drawableSlider.color.red},${this.drawableSlider.color.green},${this.drawableSlider.color.blue},${opacity})`;
        ctx.beginPath();
        ctx.arc(0, 0, renderedRadius, 0, Math.PI * 2);
        ctx.fill();
        ctx.stroke();

        const slider = this.slider;
        const isAtEnd = (this.hitObject.repeatIndex % 2 === 0);
        const progress = isAtEnd ? 1 : 0;
        const eps = 0.01;
        const pos1 = slider.path.positionAt(progress);
        const pos2 = slider.path.positionAt(isAtEnd ? progress - eps : progress + eps);
        const angle = Math.atan2(pos2.y - pos1.y, pos2.x - pos1.x);

        ctx.rotate(angle);
        ctx.beginPath();
        ctx.moveTo(renderedRadius * 0.4, 0);
        ctx.lineTo(-renderedRadius * 0.4, renderedRadius * 0.4);
        ctx.lineTo(-renderedRadius * 0.2, 0);
        ctx.lineTo(-renderedRadius * 0.4, -renderedRadius * 0.4);
        ctx.closePath();
        ctx.fill();

        ctx.restore();
    }

    opacity(time: number): number {
        let opacity = super.opacity(time);

        opacity *= Math.min(1, Math.max(0, 1 - (time - this.hitObject.startTime) / this.animDuration));

        return opacity;
    }
}


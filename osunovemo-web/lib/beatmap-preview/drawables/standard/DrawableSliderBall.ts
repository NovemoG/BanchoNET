import MathUtils from "../../utils/MathUtils";
import {CIRCLE_BORDER_WIDTH} from "../../renderers/StandardRenderer";
import {DrawableSlider} from "./DrawableSlider";

export class DrawableSliderBall {
    FOLLOW_CIRCLE_FACTOR = 2;
    FOLLOW_CIRCLE_WIDTH = 3;

    private drawableSlider: DrawableSlider;

    constructor(drawableSlider: DrawableSlider) {
        this.drawableSlider = drawableSlider;
    }

    draw(ctx: CanvasRenderingContext2D, time: number) {
        const {radius, duration, startTime, spans, path, stackedStartPosition} = this.drawableSlider.hitObject;

        let currentProgress = MathUtils.clamp((time - startTime) / duration, 0, 1);
        currentProgress = path.progressAt(currentProgress, spans)
        let {x, y} = path.positionAt(currentProgress);

        x += stackedStartPosition.x;
        y += stackedStartPosition.y;

        const opacity = this.opacity(time);
        const scale = this.scale(time);

        const circleSize = (radius - 2) * (1 - CIRCLE_BORDER_WIDTH);
        ctx.strokeStyle = `rgba(255,255,255,${opacity})`;
        ctx.lineWidth = radius * CIRCLE_BORDER_WIDTH;
        ctx.beginPath();
        ctx.arc(x, y, circleSize, 0, Math.PI * 2);
        ctx.stroke();
        ctx.lineWidth = this.FOLLOW_CIRCLE_WIDTH;
        ctx.beginPath();
        ctx.arc(x, y, (circleSize * this.FOLLOW_CIRCLE_FACTOR) * scale, 0, Math.PI * 2);
        ctx.stroke();
    }

    opacity(time: number): number {
        let opacity = 0;

        if (time > this.drawableSlider.hitObject.startTime) {
            opacity = 1;
        }

        if (time > this.drawableSlider.hitObject.endTime) {
            opacity = 1 - (time - this.drawableSlider.hitObject.endTime) / 150;
        }

        return opacity;
    }

    scale(time: number): number {
        let scale = 0;

        if (time > this.drawableSlider.hitObject.startTime) {
            scale = Math.min(0.5 + (time - this.drawableSlider.hitObject.startTime) / 150, 1);
        }

        if (time > this.drawableSlider.hitObject.endTime) {
            scale = Math.max(1 - (time - this.drawableSlider.hitObject.endTime) / 450, 0.8);
        }

        return scale;
    }
}


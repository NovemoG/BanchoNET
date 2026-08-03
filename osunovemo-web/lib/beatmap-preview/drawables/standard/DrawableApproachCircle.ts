import {DrawableCircle} from "./DrawableCircle";

export class DrawableApproachCircle {
    APPROACH_CIRCLE_WIDTH = 0.09;
    APPROACH_CIRCLE_SIZE = 4;

    drawableCircle: DrawableCircle;

    constructor(hitObject: DrawableCircle) {
        this.drawableCircle = hitObject;
    }

    draw(ctx: CanvasRenderingContext2D, time: number) {
        const {radius, stackedStartPosition} = this.drawableCircle.hitObject;
        const {x, y} = stackedStartPosition;

        const opacity = this.opacity(time);
        const scale = this.scale(time)

        ctx.lineWidth = radius * this.APPROACH_CIRCLE_WIDTH;
        ctx.strokeStyle = `rgba(${this.drawableCircle.color.red},${this.drawableCircle.color.green},${this.drawableCircle.color.blue},${opacity})`;
        ctx.beginPath();
        ctx.arc(x, y, radius * (1 + this.APPROACH_CIRCLE_SIZE * scale), 0, Math.PI * 2);
        ctx.stroke();
    }

    opacity(time: number): number {
        let opacity = 0;

        if (time <= this.drawableCircle.hitObject.startTime) {
            opacity = Math.max(0, time - (this.drawableCircle.hitObject.startTime - this.drawableCircle.hitObject.timePreempt)) / this.drawableCircle.hitObject.timeFadeIn;
        }

        return opacity;
    }

    scale(time: number): number {
        return Math.max(0, this.drawableCircle.hitObject.startTime - time) / this.drawableCircle.hitObject.timePreempt;
    }
}


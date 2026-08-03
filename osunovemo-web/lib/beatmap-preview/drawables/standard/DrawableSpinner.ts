import {Spinner} from "osu-standard-stable";
import MathUtils from "../../utils/MathUtils";
import {DrawableStandardHitObject} from "./DrawableStandardHitObject";

export class DrawableSpinner extends DrawableStandardHitObject<Spinner> {
    SPINNER_SIZE = 180;
    SPINNER_CENTER_SIZE = 10;

    draw(ctx: CanvasRenderingContext2D, time: number) {
        const {startTime, endTime, stackedStartPosition} = this.hitObject;
        const {x, y} = stackedStartPosition;

        const opacity = this.opacity(time);
        const scale = MathUtils.clamp((endTime - time) / (endTime - startTime), 0, 1);

        ctx.lineWidth = 5;
        ctx.strokeStyle = `rgba(255,255,255,${opacity})`;
        ctx.beginPath();
        ctx.arc(x, y, this.SPINNER_SIZE * scale, 0, Math.PI * 2);
        ctx.stroke();
        ctx.beginPath();
        ctx.arc(x, y, this.SPINNER_CENTER_SIZE, 0, Math.PI * 2);
        ctx.stroke();
    }

    opacity(time: number): number {
        let opacity = super.opacity(time);

        if (time > this.hitObject.endTime) {
            opacity = 1 - (time - this.hitObject.endTime) / this.HIT_DURATION;
        }

        return opacity;
    }
}


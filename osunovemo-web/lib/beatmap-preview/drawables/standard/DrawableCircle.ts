import {Circle} from "osu-standard-stable";
import {Color4} from "osu-classes";
import {DrawableStandardHitObject} from "./DrawableStandardHitObject";
import {DrawableApproachCircle} from "./DrawableApproachCircle";

export class DrawableCircle extends DrawableStandardHitObject<Circle> {
    color: Color4;

    approachCircle: DrawableApproachCircle;

    constructor(hitObject: Circle, color: Color4) {
        super(hitObject);
        this.color = color;
        this.approachCircle = new DrawableApproachCircle(this);
    }

    draw(ctx: CanvasRenderingContext2D, time: number) {
        const {radius, stackedStartPosition, currentComboIndex} = this.hitObject;
        const {x, y} = stackedStartPosition;

        const scale = this.scale(time);
        const opacity = this.opacity(time);
        const circleSize = radius * scale;

        const borderThickness = (circleSize * 2) * (2 / 58);
        const gradientThickness = borderThickness * 2.5;
        const outerGradientSize = (circleSize * 2) - borderThickness * 4;
        const innerGradientSize = outerGradientSize - gradientThickness * 2;
        const innerFillSize = innerGradientSize - gradientThickness * 2;

        ctx.lineWidth = borderThickness;
        ctx.strokeStyle = `rgba(255,255,255,${opacity})`;
        ctx.beginPath();
        ctx.arc(x, y, circleSize, 0, Math.PI * 2);
        ctx.stroke();

        ctx.fillStyle = `rgba(${this.color.red},${this.color.green},${this.color.blue},${opacity})`;
        ctx.beginPath();
        ctx.arc(x, y, outerGradientSize / 2, 0, Math.PI * 2);
        ctx.fill();

        ctx.fillStyle = `rgba(${this.color.red / 1.5},${this.color.green / 1.5},${this.color.blue / 1.5},${opacity})`;
        ctx.beginPath();
        ctx.arc(x, y, innerGradientSize / 2, 0, Math.PI * 2);
        ctx.fill();

        ctx.fillStyle = `rgba(${this.color.red / 4},${this.color.green / 4},${this.color.blue / 4},${opacity})`;
        ctx.beginPath();
        ctx.arc(x, y, innerFillSize / 2, 0, Math.PI * 2);
        ctx.fill();

        ctx.font = `700 ${circleSize * 0.7}px "Torus"`;
        ctx.textBaseline = 'middle';
        ctx.textAlign = 'center';
        ctx.fillStyle = `rgba(255,255,255,${opacity})`;
        ctx.fillText((currentComboIndex + 1).toString(), x, y + 1);
    }

    opacity(time: number): number {
        let opacity = super.opacity(time);

        if (time > this.hitObject.startTime) {
            opacity = 1 - (time - this.hitObject.startTime) / this.HIT_DURATION;
        }

        return opacity;
    }
}



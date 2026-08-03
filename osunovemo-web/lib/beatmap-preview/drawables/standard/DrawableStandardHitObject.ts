import {StandardHitObject} from "osu-standard-stable";
import DrawableHitObject from "../DrawableHitObject";

/**
 * Abstract class representing a drawable standard hit object.
 *
 * @template T - The type of the hit object, extending {@link StandardHitObject}.
 */
export abstract class DrawableStandardHitObject<T extends StandardHitObject> extends DrawableHitObject<T> {
    HIT_FACTOR = 1.33;
    HIT_DURATION = 150;

    parentHitObject?: DrawableStandardHitObject<StandardHitObject>;

    opacity(time: number): number {
        return Math.max(0, time - (this.hitObject.startTime - this.hitObject.timePreempt)) / this.hitObject.timeFadeIn;
    };

    scale(time: number): number {
        if (time <= this.hitObject.startTime) return 1;

        const t = (time - this.hitObject.startTime) / this.HIT_DURATION;
        return 1 - t + t * this.HIT_FACTOR;
    };

    x() {
        return this.hitObject.stackedStartPosition.x;
    }

    y() {
        return this.hitObject.stackedStartPosition.y;
    }

    abstract draw(ctx: CanvasRenderingContext2D, time: number): void;
}


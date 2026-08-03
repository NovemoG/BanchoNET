import {Circle, Slider, Spinner, StandardBeatmap, StandardHitObject} from "osu-standard-stable";
import {Color4} from "osu-classes";
import {DrawableSpinner} from "../drawables/standard/DrawableSpinner";
import {DrawableCircle} from "../drawables/standard/DrawableCircle";
import {DrawableSlider} from "../drawables/standard/DrawableSlider";
import RulesetRenderer from "./Renderer";
import {DrawableStandardHitObject} from "../drawables/standard/DrawableStandardHitObject";

export const CIRCLE_BORDER_WIDTH = 0.15;
const DEFAULT_COMBO_COLORS = [
    new Color4(0, 202, 0),
    new Color4(18, 124, 255),
    new Color4(242, 24, 57),
    new Color4(255, 192, 0),
];

export default class StandardRenderer extends RulesetRenderer<StandardBeatmap, DrawableStandardHitObject<StandardHitObject>> {
    protected initializeDrawableHitObjects(): void {
        const rawComboColors = this.beatmap.colors?.comboColors;
        const comboColors = rawComboColors != null && rawComboColors.length > 0
            ? rawComboColors
            : DEFAULT_COMBO_COLORS;

        this.beatmap.hitObjects.forEach((object) => {
            const comboIndex = Math.abs(object.comboIndexWithOffsets % comboColors.length);
            const color = comboColors[comboIndex] ?? DEFAULT_COMBO_COLORS[comboIndex % DEFAULT_COMBO_COLORS.length];

            if (object instanceof Spinner) {
                this.drawableHitObjects.push(new DrawableSpinner(object));
            } else if (object instanceof Slider) {
                this.drawableHitObjects.push(new DrawableSlider(object, color));
            } else if (object instanceof Circle) {
                this.drawableHitObjects.push(new DrawableCircle(object, color));
            }
        });

    }

    public render(time: number): void {
        time = time * this.beatmap.difficulty.clockRate;

        const hitObjects = this.drawableHitObjects.filter(object => {
            if (time < object.hitObject.startTime - object.hitObject.timePreempt) return false;

            if (object instanceof DrawableSlider || object instanceof DrawableSpinner) {
                return time <= object.hitObject.endTime + object.HIT_DURATION;
            }

            return time <= object.hitObject.startTime + object.HIT_DURATION;
        }).reverse();

        hitObjects.forEach(object => object.draw(this.ctx, time));

        hitObjects.forEach((object) => {
            if (object instanceof DrawableSlider) {
                object.sliderHead.approachCircle.draw(this.ctx, time);
            }

            if (object instanceof DrawableCircle) {
                object.approachCircle.draw(this.ctx, time);
            }
        });
    }
}


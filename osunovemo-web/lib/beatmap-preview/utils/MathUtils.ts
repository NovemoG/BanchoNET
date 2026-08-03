export default class MathUtils {
    public static clamp(value: number, min: number, max: number): number {
        return Math.min(Math.max(value, min), max);
    }

    public static lerp(t: number, a: number, b: number): number {
        return a + (b - a) * t;
    }
}


export type HighlightOption = {
    days: number | null;
    label: string;
};

export const scoreHighlightOptions: HighlightOption[] = [
    {days: null, label: "All"},
    ...Array.from({length: 30}, (_, index) => ({
        days: index + 1,
        label: `${index + 1}d`,
    })),
    {days: 90, label: "3m"},
    {days: 180, label: "6m"},
    {days: 365, label: "12m"},
];

export function getScoreHighlightCutoff(highlightPeriodDays: number | null | undefined, now: number) {
    if (highlightPeriodDays == null) {
        return null;
    }

    return now - (highlightPeriodDays * 24 * 60 * 60 * 1000);
}

export function isScoreHighlighted(endedAt: string | null | undefined, highlightCutoff: number | null) {
    if (highlightCutoff == null) {
        return false;
    }

    const endedAtTime = new Date(endedAt ?? "").getTime();
    return !Number.isNaN(endedAtTime) && endedAtTime >= highlightCutoff;
}

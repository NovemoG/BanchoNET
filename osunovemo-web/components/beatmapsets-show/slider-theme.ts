export const beatmapInfoSliderClassNames = {
  remainder: "bg-black/25",
  thumb: "bg-osu-b6 ring-osu-h1",
  thumbActive: "scale-95 bg-osu-b5",
  thumbHover: "hover:bg-osu-b5",
  value: "bg-osu-h1",
  valueAccent: "bg-osu-h1",
} as const;

export const beatmapInfoSliderTrackColors = {
  remainder: "rgba(0, 0, 0, 0.25)",
  value: "hsl(var(--hsl-h1))",
} as const;

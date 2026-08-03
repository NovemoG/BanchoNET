import modDefinitions, { type OsuModDefinition } from "@/lib/osu-mod-definitions";
import type { Score, ScoreMod } from "@/lib/profile";

export const rankAssets: Record<string, string> = {
  A: "/badges/score-ranks-v2019/GradeSmall-A.svg",
  B: "/badges/score-ranks-v2019/GradeSmall-B.svg",
  C: "/badges/score-ranks-v2019/GradeSmall-C.svg",
  D: "/badges/score-ranks-v2019/GradeSmall-D.svg",
  F: "/badges/score-ranks-v2019/GradeSmall-F.svg",
  S: "/badges/score-ranks-v2019/GradeSmall-S.svg",
  SH: "/badges/score-ranks-v2019/GradeSmall-S-Silver.svg",
  X: "/badges/score-ranks-v2019/GradeSmall-SS.svg",
  XH: "/badges/score-ranks-v2019/GradeSmall-SS-Silver.svg",
};

export const modTypeColors = {
  Automation: {
    background: "#00b5ff",
    foreground: "#0b2936",
  },
  Conversion: {
    background: "#8866ff",
    foreground: "#1d1638",
  },
  DifficultyIncrease: {
    background: "#ff5c7a",
    foreground: "#3a1320",
  },
  DifficultyReduction: {
    background: "#8adc20",
    foreground: "#20330a",
  },
  Fun: {
    background: "#ff66aa",
    foreground: "#3b1428",
  },
  System: {
    background: "#ffcc22",
    foreground: "#3a2a08",
  },
} as const;

export const modBadgeMask = "/badges/mods/blanks/mod-icon.svg";
export const modExtenderMask = "/badges/mods/blanks/mod-icon-extender.svg";

const modSettingLabels: Partial<Record<string, string>> = {
  adjust_pitch: "Adjust pitch",
  approach_rate: "Approach Rate",
  circle_size: "Circle Size",
  drain_rate: "HP Drain",
  extended_limits: "Extended Limits",
  hard_rock_offsets: "Spicy Patterns",
  overall_difficulty: "Accuracy",
  scroll_speed: "Scroll Speed",
};

const noModDefinition: OsuModDefinition = {
  filename: "mod-no-mod.svg",
  name: "No Mod",
  type: "System",
};

function formatFixed(value: number, precision: number) {
  return value.toLocaleString("en-US", {
    maximumFractionDigits: precision,
    minimumFractionDigits: precision,
  });
}

function formatTrimmedFixed(value: number, precision: number) {
  return value.toLocaleString("en-US", {
    maximumFractionDigits: precision,
    minimumFractionDigits: 0,
  });
}

function formatSettingValue(key: string, value: boolean | number | string) {
  if (key === "speed_change" && typeof value === "number") {
    return `${formatFixed(value, 2)}x`;
  }

  if (typeof value === "boolean") {
    return value ? "on" : "off";
  }

  if (typeof value === "number") {
    if (["approach_rate", "circle_size", "drain_rate", "overall_difficulty", "scroll_speed"].includes(key)) {
      return formatTrimmedFixed(value, key === "scroll_speed" ? 2 : 1);
    }

    return formatFixed(value, 2);
  }

  return String(value);
}

export function normalizeRank(rank: string) {
  switch (rank) {
    case "SS":
      return "X";
    case "SSH":
      return "XH";
    default:
      return rank;
  }
}

export function getDisplayedMods(score: Score) {
  return score.mods?.filter((mod) => mod.acronym.length > 0) ?? [];
}

export function getExtendedContent(mod: ScoreMod): string {
  switch (mod.acronym) {
    case "HT":
    case "DC":
    case "DT":
    case "NC": {
      const speedChange = mod.settings?.speed_change;
      return typeof speedChange === "number" ? `${formatFixed(speedChange, 2)}x` : "";
    }
    case "DA": {
      const displayCandidates = {
        approach_rate: { acronym: "AR", significantDigits: 1 },
        circle_size: { acronym: "CS", significantDigits: 1 },
        drain_rate: { acronym: "HP", significantDigits: 1 },
        overall_difficulty: { acronym: "OD", significantDigits: 1 },
        scroll_speed: { acronym: "SS", significantDigits: 2 },
      } as const;

      let displayValue: number | undefined;
      let displayAcronym: string | undefined;
      let displayDigits: number | undefined;

      for (const [key, candidate] of Object.entries(displayCandidates)) {
        const value = mod.settings?.[key];
        if (typeof value === "number") {
          if (displayValue != null) {
            return "";
          }

          displayValue = value;
          displayAcronym = candidate.acronym;
          displayDigits = candidate.significantDigits;
        }
      }

      if (displayValue != null && displayAcronym != null && displayDigits != null) {
        return `${displayAcronym}${formatTrimmedFixed(displayValue, displayDigits)}`;
      }

      return "";
    }
    default:
      return "";
  }
}

export function getExtendedFontSize(text: string) {
  if (text.length <= 4) {
    return "0.95rem";
  }

  if (text.length === 5) {
    return "0.85rem";
  }

  if (text.length === 6) {
    return "0.75rem";
  }

  return "0.65rem";
}

export function getModDefinition(acronym: string): OsuModDefinition {
  if (acronym === "NM") {
    return noModDefinition;
  }

  return modDefinitions[acronym] ?? {
    ...noModDefinition,
    name: acronym,
  };
}

export function getModTitle(mod: ScoreMod, definition: OsuModDefinition) {
  const entries = Object.entries(mod.settings ?? {});
  if (entries.length === 0) {
    return definition.name;
  }

  const summary = entries
    .map(([key, value]) =>
      key === "speed_change"
        ? formatSettingValue(key, value).replace(/x$/, "×")
        : `${modSettingLabels[key] ?? key.replaceAll("_", " ")}: ${formatSettingValue(key, value)}`,
    )
    .join(", ");

  return `${definition.name} (${summary})`;
}

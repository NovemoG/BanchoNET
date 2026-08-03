import type { Ruleset } from "@/lib/rankings";

export type BeatmapPpCalculatorMod = {
  acronym: string;
  settings?: Record<string, boolean | number | string>;
};

export type BeatmapPpCalculatorPerformanceBreakdown = {
  effectiveMissCount: number | null;
  ppAccuracy: number | null;
  ppAim: number | null;
  ppDifficulty: number | null;
  ppFlashlight: number | null;
  ppSpeed: number | null;
};

export type BeatmapPpCalculatorRequest = {
  accuracy?: number;
  combo?: number | null;
  largeTickHits?: number;
  misses?: number;
  mode: Ruleset;
  mods?: BeatmapPpCalculatorMod[];
  n50?: number;
  n100?: number;
  n300?: number;
  nGeki?: number;
  nKatu?: number;
  sliderEndHits?: number;
};

export type BeatmapPpCalculatorResponse = {
  accuracyPp: Array<{
    accuracy: number;
    pp: number;
  }>;
  calculatedAccuracy: number | null;
  comparisonPp: Array<{
    accuracyPp: Array<{
      accuracy: number;
      pp: number;
    }>;
    label: string;
    mods: BeatmapPpCalculatorMod[];
    stars: number;
  }>;
  fcPp: number;
  attributes: {
    ar: number | null;
    arHitWindow: number | null;
    baseAr: number | null;
    baseOd: number | null;
    clockRate: number;
    cs: number | null;
    hp: number | null;
    od: number | null;
    odGreatHitWindow: number | null;
    odOkHitWindow: number | null;
    odMehHitWindow: number | null;
    odPerfectHitWindow: number | null;
  };
  difficulty: {
    aim: number | null;
    flashlight: number | null;
    maxCombo: number;
    nLargeTicks: number | null;
    nSliders: number | null;
    speed: number | null;
    stars: number;
  };
  maxPp: number;
  maxPerformance: BeatmapPpCalculatorPerformanceBreakdown;
  missLossPp: number;
  performance: BeatmapPpCalculatorPerformanceBreakdown;
  pp: number;
  state: {
    maxCombo?: number;
    misses?: number;
    n50?: number;
    n100?: number;
    n300?: number;
    nGeki?: number;
    nKatu?: number;
    osuLargeTickHits?: number;
    osuSmallTickHits?: number;
    sliderEndHits?: number;
  } | null;
};

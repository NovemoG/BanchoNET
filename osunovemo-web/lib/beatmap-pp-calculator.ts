import "server-only";

import * as rosu from "rosu-pp-js";
import { fetchBeatmapOsu } from "@/lib/beatmapset";
import type {
  BeatmapPpCalculatorMod,
  BeatmapPpCalculatorRequest,
  BeatmapPpCalculatorResponse,
} from "@/lib/beatmap-pp-calculator-types";
import type { Ruleset } from "@/lib/rankings";

function getRosuGameMode(mode: Ruleset) {
  switch (mode) {
    case "taiko":
      return rosu.GameMode.Taiko;
    case "fruits":
      return rosu.GameMode.Catch;
    case "mania":
      return rosu.GameMode.Mania;
    default:
      return rosu.GameMode.Osu;
  }
}

function finiteOrNull(value: number | undefined) {
  return typeof value === "number" && Number.isFinite(value) ? value : null;
}

function clamp(value: number, min: number, max: number) {
  return Math.max(min, Math.min(max, value));
}

function normalizeNumber(value: unknown, fallback: number, min: number, max: number) {
  return typeof value === "number" && Number.isFinite(value)
    ? clamp(value, min, max)
    : fallback;
}

function normalizeInteger(value: unknown, fallback: number, min: number, max: number) {
  return Math.round(normalizeNumber(value, fallback, min, max));
}

function normalizeOptionalInteger(value: unknown, min: number, max: number) {
  return typeof value === "number" && Number.isFinite(value)
    ? Math.round(clamp(value, min, max))
    : undefined;
}

function hasManualHitResults(request: BeatmapPpCalculatorRequest) {
  return [
    request.largeTickHits,
    request.n300,
    request.n100,
    request.n50,
    request.nGeki,
    request.nKatu,
    request.sliderEndHits,
  ].some((value) => typeof value === "number" && Number.isFinite(value));
}

function normalizeMods(value: BeatmapPpCalculatorRequest["mods"]) {
  if (!Array.isArray(value)) {
    return [];
  }

  const seenMods = new Set<string>();
  const mods: BeatmapPpCalculatorMod[] = [];

  for (const mod of value.slice(0, 32)) {
    if (typeof mod?.acronym !== "string") {
      continue;
    }

    const acronym = mod.acronym.trim().toUpperCase();
    if (!/^[A-Z0-9]{1,4}$/.test(acronym) || seenMods.has(acronym)) {
      continue;
    }

    seenMods.add(acronym);

    const settings: Record<string, boolean | number | string> = {};
    for (const [key, rawValue] of Object.entries(mod.settings ?? {}).slice(0, 16)) {
      if (!/^[a-z0-9_]{1,40}$/.test(key)) {
        continue;
      }

      if (typeof rawValue === "boolean") {
        settings[key] = rawValue;
      } else if (typeof rawValue === "number" && Number.isFinite(rawValue)) {
        settings[key] = clamp(rawValue, -100, 100);
      } else if (typeof rawValue === "string" && rawValue.length <= 80) {
        settings[key] = rawValue;
      }
    }

    mods.push(Object.keys(settings).length > 0 ? { acronym, settings } : { acronym });
  }

  return mods;
}

function buildDifficultyArgs(request: BeatmapPpCalculatorRequest): rosu.DifficultyArgs {
  return {
    lazer: true,
    mods: normalizeMods(request.mods),
  };
}

function buildPerformanceArgs(
  request: BeatmapPpCalculatorRequest,
  options: { combo?: number | null; maxCombo: number },
): rosu.PerformanceArgs {
  const accuracy = normalizeNumber(request.accuracy, 100, 0, 100);
  const misses = normalizeInteger(request.misses, 0, 0, 100000);
  const manualHitResults = hasManualHitResults(request);
  const combo =
    options.combo === null
      ? null
      : normalizeInteger(options.combo ?? request.combo, options.maxCombo, 0, Math.max(0, options.maxCombo));

  return {
    ...buildDifficultyArgs(request),
    combo,
    hitresultPriority: rosu.HitResultPriority.BestCase,
    misses,
    ...(manualHitResults
      ? {
          largeTickHits: normalizeOptionalInteger(request.largeTickHits, 0, 100000),
          n50: normalizeOptionalInteger(request.n50, 0, 100000),
          n100: normalizeOptionalInteger(request.n100, 0, 100000),
          n300: normalizeOptionalInteger(request.n300, 0, 100000),
          nGeki: normalizeOptionalInteger(request.nGeki, 0, 100000),
          nKatu: normalizeOptionalInteger(request.nKatu, 0, 100000),
          sliderEndHits: normalizeOptionalInteger(request.sliderEndHits, 0, 100000),
        }
      : {
          accuracy,
        }),
  };
}

function calculateStateAccuracy(
  state: NonNullable<BeatmapPpCalculatorResponse["state"]> | undefined,
  mode: Ruleset,
) {
  if (state == null) {
    return null;
  }

  const misses = state.misses ?? 0;
  const n50 = state.n50 ?? 0;
  const n100 = state.n100 ?? 0;
  const n300 = state.n300 ?? 0;
  const nGeki = state.nGeki ?? 0;
  const nKatu = state.nKatu ?? 0;

  if (mode === "mania") {
    const total = n50 + n100 + nKatu + n300 + nGeki + misses;
    return total > 0 ? ((n50 * 50 + n100 * 100 + nKatu * 200 + n300 * 300 + nGeki * 320) / (total * 320)) * 100 : null;
  }

  if (mode === "fruits") {
    const total = n300 + n100 + n50 + nKatu + misses;
    return total > 0 ? ((n300 + n100 + n50) / total) * 100 : null;
  }

  const total = n300 + n100 + n50 + misses;
  return total > 0 ? ((n300 * 300 + n100 * 100 + n50 * 50) / (total * 300)) * 100 : null;
}

function setHitresultGenerator(performance: rosu.Performance, mode: rosu.GameMode) {
  performance.setHitresultGenerator(
    mode === rosu.GameMode.Mania ? rosu.HitResultGenerator.Fast : rosu.HitResultGenerator.Closest,
    mode,
  );
}

const comparisonModSets: Array<{
  label: string;
  mods: BeatmapPpCalculatorMod[];
}> = [
  { label: "NM", mods: [] },
  { label: "HD", mods: [{ acronym: "HD" }] },
  { label: "HR", mods: [{ acronym: "HR" }] },
  { label: "HDHR", mods: [{ acronym: "HD" }, { acronym: "HR" }] },
  { label: "DT", mods: [{ acronym: "DT" }] },
  { label: "HDDT", mods: [{ acronym: "HD" }, { acronym: "DT" }] },
  { label: "HRDT", mods: [{ acronym: "HR" }, { acronym: "DT" }] },
  { label: "HDDTHR", mods: [{ acronym: "HD" }, { acronym: "DT" }, { acronym: "HR" }] },
  { label: "EZ", mods: [{ acronym: "EZ" }] },
  { label: "EZDT", mods: [{ acronym: "EZ" }, { acronym: "DT" }] },
];

function freeAll(resources: Array<{ free: () => void }>) {
  for (const resource of resources) {
    resource.free();
  }
}

function serializePerformanceBreakdown(performance: rosu.PerformanceAttributes) {
  return {
    effectiveMissCount: finiteOrNull(performance.effectiveMissCount),
    ppAccuracy: finiteOrNull(performance.ppAccuracy),
    ppAim: finiteOrNull(performance.ppAim),
    ppDifficulty: finiteOrNull(performance.ppDifficulty),
    ppFlashlight: finiteOrNull(performance.ppFlashlight),
    ppSpeed: finiteOrNull(performance.ppSpeed),
  };
}

function buildResponse(
  attributes: rosu.BeatmapAttributes,
  difficulty: rosu.DifficultyAttributes,
  accuracyPp: BeatmapPpCalculatorResponse["accuracyPp"],
  comparisonPp: BeatmapPpCalculatorResponse["comparisonPp"],
  fcPerformance: rosu.PerformanceAttributes,
  maxPerformance: rosu.PerformanceAttributes,
  mode: Ruleset,
  performance: rosu.PerformanceAttributes,
): BeatmapPpCalculatorResponse {
  const maxPp = accuracyPp.find((entry) => entry.accuracy === 100)?.pp ?? performance.pp;
  const fcPp = fcPerformance.pp;

  return {
    accuracyPp,
    calculatedAccuracy: calculateStateAccuracy(performance.state, mode),
    comparisonPp,
    fcPp,
    attributes: {
      ar: finiteOrNull(attributes.ar),
      arHitWindow: finiteOrNull(attributes.arHitWindow),
      baseAr: finiteOrNull(attributes.baseAr),
      baseOd: finiteOrNull(attributes.baseOd),
      clockRate: attributes.clockRate,
      cs: finiteOrNull(attributes.cs),
      hp: finiteOrNull(attributes.hp),
      od: finiteOrNull(attributes.od),
      odGreatHitWindow: finiteOrNull(attributes.odGreatHitWindow),
      odMehHitWindow: finiteOrNull(attributes.odMehHitWindow),
      odOkHitWindow: finiteOrNull(attributes.odOkHitWindow),
      odPerfectHitWindow: finiteOrNull(attributes.odPerfectHitWindow),
    },
    difficulty: {
      aim: finiteOrNull(difficulty.aim),
      flashlight: finiteOrNull(difficulty.flashlight),
      maxCombo: difficulty.maxCombo,
      nLargeTicks: finiteOrNull(difficulty.nLargeTicks),
      nSliders: finiteOrNull(difficulty.nSliders),
      speed: finiteOrNull(difficulty.speed),
      stars: difficulty.stars,
    },
    maxPp,
    maxPerformance: serializePerformanceBreakdown(maxPerformance),
    missLossPp: Math.max(0, fcPp - performance.pp),
    performance: serializePerformanceBreakdown(performance),
    pp: performance.pp,
    state: performance.state ?? null,
  };
}

export async function calculateBeatmapPp(
  beatmapId: number | string,
  request: BeatmapPpCalculatorRequest,
): Promise<BeatmapPpCalculatorResponse> {
  const beatmapContent = await fetchBeatmapOsu(beatmapId);
  const mode = getRosuGameMode(request.mode);
  const mods = normalizeMods(request.mods);

  let beatmap: rosu.Beatmap | undefined;
  let attributes: rosu.BeatmapAttributes | undefined;
  let difficultyCalculator: rosu.Difficulty | undefined;
  let difficulty: rosu.DifficultyAttributes | undefined;
  let performanceCalculator: rosu.Performance | undefined;
  let performance: rosu.PerformanceAttributes | undefined;
  let fcPerformanceCalculator: rosu.Performance | undefined;
  let fcPerformance: rosu.PerformanceAttributes | undefined;
  const comparisonDifficultyCalculators: rosu.Difficulty[] = [];
  const comparisonDifficulties: rosu.DifficultyAttributes[] = [];
  const referencePerformanceCalculators: rosu.Performance[] = [];
  const referencePerformances: rosu.PerformanceAttributes[] = [];
  let maxPerformance: rosu.PerformanceAttributes | undefined;

  try {
    beatmap = new rosu.Beatmap(beatmapContent);

    if (beatmap.isSuspicious()) {
      throw new Error("Suspicious beatmap.");
    }

    if (beatmap.mode !== mode) {
      beatmap.convert(mode, mods);
    }

    const difficultyArgs = {
      lazer: true,
      mods,
    } satisfies rosu.DifficultyArgs;

    const attributesBuilder = new rosu.BeatmapAttributesBuilder({
      map: beatmap,
      mode,
      mods,
    });
    attributes = attributesBuilder.build();

    difficultyCalculator = new rosu.Difficulty(difficultyArgs);
    difficulty = difficultyCalculator.calculate(beatmap);
    const calculatedDifficulty = difficulty;

    performanceCalculator = new rosu.Performance(
      buildPerformanceArgs(
        {
          ...request,
          mods,
        },
        { maxCombo: calculatedDifficulty.maxCombo },
      ),
    );
    setHitresultGenerator(performanceCalculator, mode);
    performance = performanceCalculator.calculate(calculatedDifficulty);
    const calculatedAccuracy = calculateStateAccuracy(performance.state, request.mode) ?? normalizeNumber(request.accuracy, 100, 0, 100);

    const calculateAccuracyPp = (
      accuracy: number,
      options: {
        difficultyAttributes: rosu.DifficultyAttributes;
        maxCombo: number;
        mods: BeatmapPpCalculatorMod[];
      } = {
        difficultyAttributes: calculatedDifficulty,
        maxCombo: calculatedDifficulty.maxCombo,
        mods,
      },
    ) => {
      const referenceCalculator = new rosu.Performance(
        buildPerformanceArgs(
          {
            ...request,
            accuracy,
            combo: options.maxCombo,
            largeTickHits: undefined,
            misses: 0,
            mods: options.mods,
            n50: undefined,
            n100: undefined,
            n300: undefined,
            nGeki: undefined,
            nKatu: undefined,
            sliderEndHits: undefined,
          },
          { maxCombo: options.maxCombo },
        ),
      );
      referencePerformanceCalculators.push(referenceCalculator);
      setHitresultGenerator(referenceCalculator, mode);

      const referencePerformance = referenceCalculator.calculate(options.difficultyAttributes);
      referencePerformances.push(referencePerformance);
      if (accuracy === 100 && options.difficultyAttributes === calculatedDifficulty) {
        maxPerformance = referencePerformance;
      }

      return {
        accuracy,
        pp: referencePerformance.pp,
      };
    };

    const accuracyPp = [100, 99, 98].map((accuracy) => calculateAccuracyPp(accuracy));
    fcPerformanceCalculator = new rosu.Performance(
      buildPerformanceArgs(
        {
          ...request,
          accuracy: calculatedAccuracy,
          combo: calculatedDifficulty.maxCombo,
          largeTickHits: undefined,
          misses: 0,
          mods,
          n50: undefined,
          n100: undefined,
          n300: undefined,
          nGeki: undefined,
          nKatu: undefined,
          sliderEndHits: undefined,
        },
        { maxCombo: calculatedDifficulty.maxCombo },
      ),
    );
    setHitresultGenerator(fcPerformanceCalculator, mode);
    fcPerformance = fcPerformanceCalculator.calculate(calculatedDifficulty);

    const comparisonPp = comparisonModSets.flatMap((comparison) => {
      try {
        const comparisonMods = normalizeMods(comparison.mods);
        const comparisonDifficultyCalculator = new rosu.Difficulty({
          lazer: true,
          mods: comparisonMods,
        });
        comparisonDifficultyCalculators.push(comparisonDifficultyCalculator);

        const comparisonDifficulty = comparisonDifficultyCalculator.calculate(beatmap!);
        comparisonDifficulties.push(comparisonDifficulty);

        return [
          {
            accuracyPp: [100, 99, 98].map((accuracy) =>
              calculateAccuracyPp(accuracy, {
                difficultyAttributes: comparisonDifficulty,
                maxCombo: comparisonDifficulty.maxCombo,
                mods: comparisonMods,
              }),
            ),
            label: comparison.label,
            mods: comparisonMods,
            stars: comparisonDifficulty.stars,
          },
        ];
      } catch {
        return [];
      }
    });

    return buildResponse(
      attributes,
      calculatedDifficulty,
      accuracyPp,
      comparisonPp,
      fcPerformance,
      maxPerformance ?? performance,
      request.mode,
      performance,
    );
  } finally {
    freeAll(referencePerformances);
    freeAll(referencePerformanceCalculators);
    freeAll(comparisonDifficulties);
    freeAll(comparisonDifficultyCalculators);
    fcPerformance?.free();
    fcPerformanceCalculator?.free();
    performance?.free();
    performanceCalculator?.free();
    difficulty?.free();
    difficultyCalculator?.free();
    attributes?.free();
    beatmap?.free();
  }
}

"use client";

import { useEffect, useId, useMemo, useState } from "react";
import { Activity, ChevronDown, ChevronUp, Gauge, Settings2 } from "lucide-react";
import useSWR from "swr";
import { BeatmapDifficultyPill } from "@/components/beatmapset-panels/shared";
import { ModBadge } from "@/components/mod-badge";
import { Field, FieldGroup, FieldLabel } from "@/components/ui/field";
import { Input } from "@/components/ui/input";
import {
  Popover,
  PopoverContent,
  PopoverHeader,
  PopoverTitle,
  PopoverTrigger,
} from "@/components/ui/popover";
import { Separator } from "@/components/ui/separator";
import { Slider } from "@/components/ui/slider";
import { Spinner } from "@/components/ui/spinner";
import { Switch } from "@/components/ui/switch";
import type { BeatmapLeaderboardResponse, BeatmapsetShowBeatmap } from "@/lib/beatmapset-types";
import type {
  BeatmapPpCalculatorMod,
  BeatmapPpCalculatorResponse,
} from "@/lib/beatmap-pp-calculator-types";
import type { Ruleset } from "@/lib/rankings";
import { getModDefinition } from "@/lib/osu-score-card";
import { cn } from "@/lib/utils";

type BeatmapPpCalculatorProps = {
  beatmap: BeatmapsetShowBeatmap;
  /**
   * Hides the calculator body only. The header keeps the difficulty stats and the mods popover
   * visible, since mods are needed to read the tile at all.
   */
  collapsed?: boolean;
  onToggleCollapsed?: () => void;
};

type SettingValue = boolean | number | string;
type SettingsByMod = Record<string, Record<string, SettingValue>>;

type NumberSetting = {
  defaultValue: number;
  key: string;
  label: string;
  max: number;
  min: number;
  precision: number;
  step: number;
  type: "number";
};

type BooleanSetting = {
  defaultValue: boolean;
  key: string;
  label: string;
  type: "boolean";
};

type SettingSchema = BooleanSetting | NumberSetting;
type HitResultMode = "accuracy" | "manual";
type ManualHitResults = {
  n50: number;
  n100: number;
  n300: number;
  nGeki: number;
  nKatu: number;
};
type ManualHitResultKey = keyof ManualHitResults;

const numberFormatter = new Intl.NumberFormat("en-US", {
  maximumFractionDigits: 2,
  minimumFractionDigits: 0,
});
const integerFormatter = new Intl.NumberFormat("en-US", {
  maximumFractionDigits: 0,
});
const ppCalculationDebounceMs = 160;
const modConfigurationDebounceMs = 350;
const referenceAccuracyColumns = [100, 99, 98] as const;
const customizeButtonClassName =
  "inline-flex h-6 shrink-0 items-center justify-center gap-1.5 rounded-full border-0 bg-osu-h2 px-3.5 text-[11px] font-semibold leading-none text-osu-c1 transition-colors hover:bg-osu-h1 disabled:cursor-default disabled:bg-osu-b3 disabled:text-osu-f1 disabled:hover:bg-osu-b3";
const numberInputClassName =
  "h-8 rounded-md border-0 bg-osu-b6 px-1 py-1 text-right text-sm font-semibold tabular-nums text-white caret-osu-h1 shadow-[inset_0_0_0_1px_rgba(255,255,255,0.06)] placeholder:text-osu-f1 focus-visible:border-transparent focus-visible:ring-0 focus-visible:shadow-[inset_0_0_0_1px_hsl(var(--hsl-h1))] disabled:bg-osu-b6/70 disabled:text-white/60 disabled:opacity-50";
const comparisonModSets = [
  { label: "NM", mods: [{ acronym: "NM" }] },
  { label: "HD", mods: [{ acronym: "HD" }] },
  { label: "HR", mods: [{ acronym: "HR" }] },
  { label: "HDHR", mods: [{ acronym: "HD" }, { acronym: "HR" }] },
  { label: "DT", mods: [{ acronym: "DT" }] },
  { label: "HDDT", mods: [{ acronym: "HD" }, { acronym: "DT" }] },
  { label: "HRDT", mods: [{ acronym: "HR" }, { acronym: "DT" }] },
  { label: "HDDTHR", mods: [{ acronym: "HD" }, { acronym: "DT" }, { acronym: "HR" }] },
  { label: "EZ", mods: [{ acronym: "EZ" }] },
  { label: "EZDT", mods: [{ acronym: "EZ" }, { acronym: "DT" }] },
] satisfies Array<{
  label: string;
  mods: BeatmapPpCalculatorMod[];
}>;

const defaultMods = ["NM", "EZ", "NF", "HT", "DC", "HR", "SD", "PF", "DT", "NC", "HD", "FL", "AC", "DA", "CL"] as const;
const modsByMode: Record<Ruleset, string[]> = {
  fruits: [...defaultMods, "MR", "RX"],
  mania: [
    "NM",
    "EZ",
    "NF",
    "HT",
    "DC",
    "HR",
    "SD",
    "PF",
    "DT",
    "NC",
    "FI",
    "HD",
    "FL",
    "MR",
    "AC",
    "DA",
    "CL",
    "1K",
    "2K",
    "3K",
    "4K",
    "5K",
    "6K",
    "7K",
    "8K",
    "9K",
    "10K",
    "DS",
    "IN",
    "HO",
    "NR",
  ],
  osu: [...defaultMods, "RX", "AP", "SO", "TD"],
  taiko: [...defaultMods, "RX"],
};
const maniaConvertMods = new Set(["1K", "2K", "3K", "4K", "5K", "6K", "7K", "8K", "9K", "10K", "DS"]);
const speedMods = new Set(["HT", "DC", "DT", "NC"]);
const maniaKeyMods = new Set(["1K", "2K", "3K", "4K", "5K", "6K", "7K", "8K", "9K", "10K"]);
const modConflicts: Record<string, string[]> = {
  AC: ["EZ", "NF", "PF"],
  AP: ["AT", "CN", "RX", "SO", "TD"],
  CL: ["ST"],
  DA: ["EZ", "HR"],
  EZ: ["HR", "AC", "DA"],
  FI: ["HD", "FL"],
  FL: ["FI"],
  HD: ["FI"],
  HO: ["IN", "NR"],
  HR: ["EZ", "DA", "MR"],
  IN: ["HO"],
  MR: ["HR"],
  NF: ["SD", "PF", "AC"],
  NR: ["HO"],
  PF: ["NF", "SD", "AC"],
  RX: ["AP"],
  SD: ["NF", "PF"],
  SO: ["AP"],
  TD: ["AP"],
};

function clamp(value: number, min: number, max: number) {
  return Math.max(min, Math.min(max, value));
}

function balanceManualHitResults(
  results: ManualHitResults,
  options: {
    balancedKeys?: ManualHitResultKey[];
    editedKey?: ManualHitResultKey;
    hitResultMax: number;
    misses: number;
  },
) {
  const capacity = Math.max(0, options.hitResultMax - options.misses);
  const balancedKeys = options.balancedKeys ?? ["n300", "n100", "n50"];
  const getMaxForKey = (key: ManualHitResultKey) => (balancedKeys.includes(key) ? capacity : 100000);
  const balancedResults: ManualHitResults = {
    n50: Math.round(clamp(results.n50, 0, getMaxForKey("n50"))),
    n100: Math.round(clamp(results.n100, 0, getMaxForKey("n100"))),
    n300: Math.round(clamp(results.n300, 0, getMaxForKey("n300"))),
    nGeki: Math.round(clamp(results.nGeki, 0, getMaxForKey("nGeki"))),
    nKatu: Math.round(clamp(results.nKatu, 0, getMaxForKey("nKatu"))),
  };
  let overflow = balancedKeys.reduce((total, key) => total + balancedResults[key], 0) - capacity;

  if (overflow <= 0) {
    return balancedResults;
  }

  const reduceOrder = [
    ...balancedKeys.filter((key) => key !== options.editedKey),
    ...balancedKeys.filter((key) => key === options.editedKey),
  ];

  for (const key of reduceOrder) {
    if (overflow <= 0) {
      break;
    }

    const reduction = Math.min(balancedResults[key], overflow);
    balancedResults[key] -= reduction;
    overflow -= reduction;
  }

  return balancedResults;
}

function formatNumber(value: number | null | undefined, digits = 2) {
  if (typeof value !== "number" || !Number.isFinite(value)) {
    return "-";
  }

  return value.toLocaleString("en-US", {
    maximumFractionDigits: digits,
    minimumFractionDigits: digits,
  });
}

function formatTrimmed(value: number | null | undefined) {
  if (typeof value !== "number" || !Number.isFinite(value)) {
    return "-";
  }

  return numberFormatter.format(value);
}

function getAvailableMods(mode: Ruleset, isConvert: boolean) {
  const mods = modsByMode[mode];

  if (mode === "mania" && !isConvert) {
    return mods.filter((mod) => !maniaConvertMods.has(mod));
  }

  return mods;
}

function getLeaderboardHref(beatmapId: number, mode: Ruleset) {
  const searchParams = new URLSearchParams();
  searchParams.set("mode", mode);
  searchParams.set("type", "global");

  return `/api/beatmaps/${beatmapId}/leaderboard?${searchParams.toString()}`;
}

function getSettingValue(setting: SettingSchema, settings: Record<string, SettingValue> | undefined) {
  const value = settings?.[setting.key];

  if (setting.type === "boolean") {
    return typeof value === "boolean" ? value : setting.defaultValue;
  }

  return typeof value === "number" && Number.isFinite(value)
    ? clamp(value, setting.min, setting.max)
    : setting.defaultValue;
}

function getSpeedDefault(acronym: string) {
  return acronym === "HT" || acronym === "DC" ? 0.75 : 1.5;
}

function getSpeedSettings(acronym: string): SettingSchema[] {
  const isSlow = acronym === "HT" || acronym === "DC";
  const settings: SettingSchema[] = [
    {
      defaultValue: getSpeedDefault(acronym),
      key: "speed_change",
      label: "Rate",
      max: isSlow ? 0.99 : 2,
      min: isSlow ? 0.5 : 1.01,
      precision: 2,
      step: 0.01,
      type: "number",
    },
  ];

  if (acronym === "HT" || acronym === "DT") {
    settings.push({
      defaultValue: false,
      key: "adjust_pitch",
      label: "Adjust pitch",
      type: "boolean",
    });
  }

  return settings;
}

function getDifficultyAdjustSettings(beatmap: BeatmapsetShowBeatmap, settings: Record<string, SettingValue> | undefined): SettingSchema[] {
  const extendedLimits = settings?.extended_limits === true;
  const min = extendedLimits ? -20 : 0;
  const max = extendedLimits ? 20 : 10;
  const schemas: SettingSchema[] = [];

  if (beatmap.mode === "osu" || beatmap.mode === "fruits") {
    schemas.push(
      {
        defaultValue: beatmap.cs,
        key: "circle_size",
        label: "Circle Size",
        max,
        min,
        precision: 1,
        step: 0.1,
        type: "number",
      },
      {
        defaultValue: beatmap.ar,
        key: "approach_rate",
        label: "Approach Rate",
        max,
        min,
        precision: 1,
        step: 0.1,
        type: "number",
      },
    );
  }

  if (beatmap.mode === "taiko") {
    schemas.push({
      defaultValue: 1,
      key: "scroll_speed",
      label: "Scroll Speed",
      max: 2,
      min: 0.5,
      precision: 2,
      step: 0.01,
      type: "number",
    });
  }

  schemas.push(
    {
      defaultValue: beatmap.drain,
      key: "drain_rate",
      label: "HP Drain",
      max,
      min,
      precision: 1,
      step: 0.1,
      type: "number",
    },
    {
      defaultValue: beatmap.accuracy,
      key: "overall_difficulty",
      label: "Accuracy",
      max,
      min,
      precision: 1,
      step: 0.1,
      type: "number",
    },
  );

  if (beatmap.mode === "fruits") {
    schemas.push({
      defaultValue: false,
      key: "hard_rock_offsets",
      label: "Spicy Patterns",
      type: "boolean",
    });
  }

  schemas.push({
    defaultValue: false,
    key: "extended_limits",
    label: "Extended Limits",
    type: "boolean",
  });

  return schemas;
}

function getSettingSchemas(
  acronym: string,
  beatmap: BeatmapsetShowBeatmap,
  settings: Record<string, SettingValue> | undefined,
): SettingSchema[] {
  if (speedMods.has(acronym)) {
    return getSpeedSettings(acronym);
  }

  if (acronym === "DA") {
    return getDifficultyAdjustSettings(beatmap, settings);
  }

  if (acronym === "CL" && beatmap.mode === "osu") {
    return [
      {
        defaultValue: false,
        key: "no_slider_head_accuracy",
        label: "No slider head accuracy",
        type: "boolean",
      },
      {
        defaultValue: false,
        key: "classic_note_lock",
        label: "Classic note lock",
        type: "boolean",
      },
      {
        defaultValue: false,
        key: "classic_health",
        label: "Classic health",
        type: "boolean",
      },
      {
        defaultValue: false,
        key: "always_play_tail_sample",
        label: "Tail samples",
        type: "boolean",
      },
      {
        defaultValue: false,
        key: "fade_hit_circle_early",
        label: "Early fade",
        type: "boolean",
      },
    ];
  }

  if (acronym === "HD" && beatmap.mode === "osu") {
    return [
      {
        defaultValue: false,
        key: "only_fade_approach_circles",
        label: "Only fade approach circles",
        type: "boolean",
      },
    ];
  }

  if (acronym === "FL") {
    return [
      {
        defaultValue: 1,
        key: "size_multiplier",
        label: "Flashlight size",
        max: 2,
        min: 0.5,
        precision: 2,
        step: 0.05,
        type: "number",
      },
      {
        defaultValue: true,
        key: "combo_based_size",
        label: "Combo size",
        type: "boolean",
      },
    ];
  }

  if (acronym === "SD" && beatmap.mode === "osu") {
    return [
      {
        defaultValue: false,
        key: "fail_on_slider_tail",
        label: "Slider tail fail",
        type: "boolean",
      },
    ];
  }

  if (acronym === "PF" && beatmap.mode === "mania") {
    return [
      {
        defaultValue: false,
        key: "require_perfect_hits",
        label: "Perfect hits",
        type: "boolean",
      },
    ];
  }

  return [];
}

function getConflicts(acronym: string) {
  const conflicts = new Set(modConflicts[acronym] ?? []);

  if (speedMods.has(acronym)) {
    for (const mod of speedMods) {
      if (mod !== acronym) {
        conflicts.add(mod);
      }
    }
  }

  if (maniaKeyMods.has(acronym)) {
    for (const mod of maniaKeyMods) {
      if (mod !== acronym) {
        conflicts.add(mod);
      }
    }
  }

  return conflicts;
}

function buildModsPayload(
  selectedMods: string[],
  settingsByMod: SettingsByMod,
  beatmap: BeatmapsetShowBeatmap,
) {
  return selectedMods.map((acronym) => {
    const modSettings = settingsByMod[acronym];
    const settings: Record<string, SettingValue> = {};

    for (const schema of getSettingSchemas(acronym, beatmap, modSettings)) {
      const value = getSettingValue(schema, modSettings);
      if (value !== schema.defaultValue) {
        settings[schema.key] = value;
      }
    }

    return Object.keys(settings).length > 0 ? { acronym, settings } : { acronym };
  }) satisfies BeatmapPpCalculatorMod[];
}

function formatCompactPp(value: number | null | undefined) {
  if (typeof value !== "number" || !Number.isFinite(value)) {
    return "-";
  }

  return value >= 100 ? integerFormatter.format(Math.round(value)) : formatNumber(value, 2);
}

function getAccuracyPp(result: BeatmapPpCalculatorResponse | null, accuracy: number) {
  return result?.accuracyPp.find((entry) => entry.accuracy === accuracy)?.pp;
}

function getComparisonAccuracyPp(
  entry: {
    accuracyPp: BeatmapPpCalculatorResponse["accuracyPp"];
  },
  accuracy: number,
) {
  return entry.accuracyPp.find((item) => item.accuracy === accuracy)?.pp;
}

function getModeBalancedHitResultKeys(mode: Ruleset): ManualHitResultKey[] {
  switch (mode) {
    case "fruits":
      return ["n300", "n100"];
    case "mania":
      return ["nGeki", "n300", "nKatu", "n100", "n50"];
    case "taiko":
      return ["n300", "n100"];
    default:
      return ["n300", "n100", "n50"];
  }
}

function getManualHitResultsFromState(
  state: BeatmapPpCalculatorResponse["state"],
  fallback: ManualHitResults,
) {
  if (state == null) {
    return fallback;
  }

  return {
    n50: state.n50 ?? 0,
    n100: state.n100 ?? 0,
    n300: state.n300 ?? 0,
    nGeki: state.nGeki ?? 0,
    nKatu: state.nKatu ?? 0,
  };
}

function getHitResultInputs(mode: Ruleset): Array<{
  key: ManualHitResultKey;
  label: string;
}> {
  switch (mode) {
    case "fruits":
      return [
        { key: "n300", label: "Fruits" },
        { key: "n100", label: "Droplets" },
        { key: "n50", label: "Tiny Droplets" },
        { key: "nKatu", label: "Tiny Droplet Misses" },
      ];
    case "mania":
      return [
        { key: "nGeki", label: "320" },
        { key: "n300", label: "300" },
        { key: "nKatu", label: "200" },
        { key: "n100", label: "100" },
        { key: "n50", label: "50" },
      ];
    case "taiko":
      return [
        { key: "n300", label: "300" },
        { key: "n100", label: "100" },
      ];
    default:
      return [
        { key: "n300", label: "300" },
        { key: "n100", label: "100" },
        { key: "n50", label: "50" },
      ];
  }
}

function getEstimatedLeaderboardRank(
  leaderboard: BeatmapLeaderboardResponse | null,
  result: BeatmapPpCalculatorResponse | null,
) {
  if (leaderboard == null || result == null || !Number.isFinite(result.pp)) {
    return "-";
  }

  const scores = leaderboard.scores
    .filter((score) => typeof score.pp === "number" && Number.isFinite(score.pp))
    .sort((left, right) => (right.pp ?? 0) - (left.pp ?? 0));

  if (scores.length === 0) {
    return "#1";
  }

  const rankIndex = scores.findIndex((score) => (score.pp ?? 0) < result.pp);
  return rankIndex === -1 ? `>${integerFormatter.format(scores.length)}` : `#${integerFormatter.format(rankIndex + 1)}`;
}

function DifficultyStat({
  baseValue,
  label,
  value,
}: {
  baseValue: number | null | undefined;
  label: string;
  value: number | null | undefined;
}) {
  const delta =
    typeof baseValue === "number" && typeof value === "number" && Number.isFinite(value)
      ? value - baseValue
      : 0;
  const tone = Math.abs(delta) < 0.01 ? "text-white" : delta > 0 ? "text-osu-red-2" : "text-osu-green-2";
  const baseRangeValue = typeof baseValue === "number" && Number.isFinite(baseValue) ? clamp(baseValue, 0, 10) : 0;
  const currentRangeValue = typeof value === "number" && Number.isFinite(value) ? clamp(value, 0, 10) : baseRangeValue;
  const lowerValue = Math.min(baseRangeValue, currentRangeValue);
  const upperValue = Math.max(baseRangeValue, currentRangeValue);
  const hasDelta = Math.abs(delta) >= 0.01;
  const baseWidth = `${(lowerValue / 10) * 100}%`;
  const deltaLeft = `${(lowerValue / 10) * 100}%`;
  const deltaWidth = `${((upperValue - lowerValue) / 10) * 100}%`;
  const DeltaIcon = delta > 0 ? ChevronUp : ChevronDown;

  return (
    <div className="min-w-0">
      <div className="relative mb-1.5 h-1 overflow-hidden rounded-full bg-osu-b6/95">
        <div
          className={cn("absolute inset-y-0 left-0 bg-osu-h1/85", hasDelta ? "rounded-l-full" : "rounded-full")}
          style={{ width: baseWidth }}
        />
        {hasDelta ? (
          <div
            className={cn(
              "absolute inset-y-0 rounded-r-full",
              lowerValue <= 0 ? "rounded-l-full" : null,
              delta > 0 ? "bg-osu-red-2" : "bg-osu-green-2",
            )}
            style={{
              left: deltaLeft,
              width: deltaWidth,
            }}
          />
        ) : null}
      </div>
      <div className="truncate text-[11px] font-semibold leading-tight text-white/70 sm:text-xs">{label}</div>
      <div className={cn("mt-px flex items-center gap-0.5 text-lg font-semibold tabular-nums leading-none", tone)}>
        {formatTrimmed(value)}
        {Math.abs(delta) >= 0.01 ? (
          <DeltaIcon aria-hidden="true" className="size-3.5 stroke-[3]" />
        ) : null}
      </div>
    </div>
  );
}

function CompactNumberInput({
  inputMode = "decimal",
  label,
  max,
  min,
  onChange,
  precision = 0,
  step = 1,
  value,
}: {
  inputMode?: "decimal" | "numeric";
  label: string;
  max: number;
  min: number;
  onChange: (value: number) => void;
  precision?: number;
  step?: number;
  value: number;
}) {
  const clampedValue = clamp(value, min, max);
  const inputId = useId();

  return (
    <Field className="gap-1">
      <FieldLabel className="max-w-full truncate text-[11px] uppercase leading-none text-white/48" htmlFor={inputId}>
        {label}
      </FieldLabel>
      <Input
        className={numberInputClassName}
        id={inputId}
        inputMode={inputMode}
        max={max}
        min={min}
        onChange={(event) => {
          const nextValue = Number(event.currentTarget.value);
          if (Number.isFinite(nextValue)) {
            onChange(clamp(nextValue, min, max));
          }
        }}
        step={step}
        type="number"
        value={clampedValue.toFixed(precision)}
      />
    </Field>
  );
}

function PpValueCard({
  label,
  tone = "default",
  value,
}: {
  label: string;
  tone?: "default" | "loss";
  value: number | null | undefined;
}) {
  const isLoss = tone === "loss" && typeof value === "number" && Number.isFinite(value) && value > 0;

  return (
    <div className="rounded-md bg-osu-b5/82 px-3 py-2 shadow-[inset_0_0_0_1px_rgba(255,255,255,0.05)]">
      <div className="truncate text-[11px] font-semibold uppercase text-white/48">{label}</div>
      <div
        className={cn(
          "mt-1 text-lg font-semibold tabular-nums leading-none",
          isLoss ? "text-osu-red-2" : "text-white",
        )}
      >
        {formatCompactPp(value)}
        <span className={cn("text-[11px]", isLoss ? "text-osu-red-2/70" : "text-white/48")}>pp</span>
      </div>
    </div>
  );
}

function BreakdownRow({
  available,
  label,
  value,
}: {
  available: number;
  label: string;
  value: number;
}) {
  const width = available > 0 ? clamp((value / available) * 100, 0, 100) : 0;

  return (
    <div>
      <div className="mb-1 flex items-center justify-between gap-2 text-xs font-semibold">
        <span className="text-white/58">{label}</span>
        <span className="tabular-nums text-white">{formatNumber(value, 2)}pp</span>
      </div>
      <div className="h-1.5 overflow-hidden rounded-full bg-osu-b6/80">
        <div
          className="h-full rounded-full bg-osu-h1/85"
          style={{
            width: `${width}%`,
          }}
        />
      </div>
    </div>
  );
}

function NumberControl({
  disabled,
  label,
  max,
  min,
  onChange,
  precision,
  step,
  value,
}: {
  disabled?: boolean;
  label: string;
  max: number;
  min: number;
  onChange: (value: number) => void;
  precision: number;
  step: number;
  value: number;
}) {
  const clampedValue = clamp(value, min, max);
  const inputId = useId();

  return (
    <Field className="gap-1.5" data-disabled={disabled ? true : undefined}>
      <FieldLabel className="max-w-full truncate text-[11px] uppercase leading-none text-white/48" htmlFor={inputId}>
        {label}
      </FieldLabel>
      <Input
        className={numberInputClassName}
        disabled={disabled}
        id={inputId}
        inputMode="decimal"
        max={max}
        min={min}
        onChange={(event) => {
          const nextValue = Number(event.currentTarget.value);
          if (Number.isFinite(nextValue)) {
            onChange(clamp(nextValue, min, max));
          }
        }}
        step={step}
        type="number"
        value={clampedValue.toFixed(precision)}
      />
      <Slider
        disabled={disabled}
        max={max}
        min={min}
        onValueChange={(values) => {
          const nextValue = values[0];
          if (nextValue != null) {
            onChange(nextValue);
          }
        }}
        step={step}
        value={[clampedValue]}
      />
    </Field>
  );
}

function ModSettingControl({
  acronym,
  onChange,
  schema,
  settings,
}: {
  acronym: string;
  onChange: (acronym: string, schema: SettingSchema, value: SettingValue) => void;
  schema: SettingSchema;
  settings: Record<string, SettingValue> | undefined;
}) {
  const controlId = useId();
  const value = getSettingValue(schema, settings);

  if (schema.type === "boolean") {
    return (
      <Field className="flex-row items-center justify-between gap-3 rounded-md bg-osu-b6/70 px-2 py-2">
        <FieldLabel className="max-w-full truncate text-xs font-semibold text-white/70" htmlFor={controlId}>
          {schema.label}
        </FieldLabel>
        <Switch
          checked={value as boolean}
          id={controlId}
          onCheckedChange={(checked) => onChange(acronym, schema, checked)}
          size="sm"
        />
      </Field>
    );
  }

  return (
    <NumberControl
      label={schema.label}
      max={schema.max}
      min={schema.min}
      onChange={(nextValue) => onChange(acronym, schema, nextValue)}
      precision={schema.precision}
      step={schema.step}
      value={value as number}
    />
  );
}

function getPerformanceRows(result: BeatmapPpCalculatorResponse | null, showFlashlight: boolean) {
  if (result == null) {
    return [];
  }

  const rows: Array<[string, number | null, number | null]> = [
    ["Aim", result.performance.ppAim, result.maxPerformance.ppAim],
    ["Speed", result.performance.ppSpeed, result.maxPerformance.ppSpeed],
    ["Acc", result.performance.ppAccuracy, result.maxPerformance.ppAccuracy],
    ["Difficulty", result.performance.ppDifficulty, result.maxPerformance.ppDifficulty],
  ];

  if (showFlashlight) {
    rows.splice(3, 0, ["Flashlight", result.performance.ppFlashlight, result.maxPerformance.ppFlashlight]);
  }

  return rows
    .filter((entry): entry is [string, number, number | null] => typeof entry[1] === "number" && Number.isFinite(entry[1]))
    .map(([label, value, available]) => [
      label,
      value,
      typeof available === "number" && Number.isFinite(available) && available > 0 ? available : value,
    ] as const);
}

export function BeatmapPpCalculator({
  beatmap,
  collapsed = false,
  onToggleCollapsed,
}: BeatmapPpCalculatorProps) {
  const hitResultMax = Math.max(1, beatmap.count_circles + beatmap.count_sliders + beatmap.count_spinners);
  const availableMods = useMemo(() => getAvailableMods(beatmap.mode, beatmap.convert), [beatmap.convert, beatmap.mode]);
  const balancedHitResultKeys = useMemo(() => getModeBalancedHitResultKeys(beatmap.mode), [beatmap.mode]);
  const hitResultInputs = useMemo(() => getHitResultInputs(beatmap.mode), [beatmap.mode]);
  const [accuracy, setAccuracy] = useState(100);
  const [combo, setCombo] = useState(beatmap.max_combo ?? 0);
  const [comboLockedToMax, setComboLockedToMax] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [hitResultMode, setHitResultMode] = useState<HitResultMode>("accuracy");
  const [largeTickMisses, setLargeTickMisses] = useState(0);
  const [loading, setLoading] = useState(true);
  const [manualHitResults, setManualHitResults] = useState<ManualHitResults>({
    n50: 0,
    n100: 0,
    n300: hitResultMax,
    nGeki: 0,
    nKatu: 0,
  });
  const [misses, setMisses] = useState(0);
  const [result, setResult] = useState<BeatmapPpCalculatorResponse | null>(null);
  const [selectedMods, setSelectedMods] = useState<string[]>([]);
  const [settingsByMod, setSettingsByMod] = useState<SettingsByMod>({});
  const [settledCalculationKey, setSettledCalculationKey] = useState<string | null>(null);
  const [sliderTailMisses, setSliderTailMisses] = useState(0);

  const selectedModSet = useMemo(() => new Set(selectedMods), [selectedMods]);
  const leaderboardHref = useMemo(() => getLeaderboardHref(beatmap.id, beatmap.mode), [beatmap.id, beatmap.mode]);
  const { data: leaderboard = null } = useSWR<BeatmapLeaderboardResponse>(leaderboardHref);
  const modsPayload = useMemo(
    () => buildModsPayload(selectedMods, settingsByMod, beatmap),
    [beatmap, selectedMods, settingsByMod],
  );
  const modsPayloadKey = useMemo(() => JSON.stringify(modsPayload), [modsPayload]);
  const [debouncedModsPayload, setDebouncedModsPayload] = useState(modsPayload);
  const debouncedModsPayloadKey = useMemo(() => JSON.stringify(debouncedModsPayload), [debouncedModsPayload]);
  const maxCombo = result?.difficulty.maxCombo ?? beatmap.max_combo ?? 0;
  const displayedCombo = comboLockedToMax ? maxCombo : clamp(combo, 0, Math.max(0, maxCombo));
  const modsDebouncing = modsPayloadKey !== debouncedModsPayloadKey;
  const largeTickMax = Math.max(0, result?.difficulty.nLargeTicks ?? 0);
  const sliderTailMax = Math.max(0, result?.difficulty.nSliders ?? beatmap.count_sliders ?? 0);
  const displayedLargeTickMisses = Math.round(clamp(largeTickMisses, 0, largeTickMax));
  const displayedSliderTailMisses = Math.round(clamp(sliderTailMisses, 0, sliderTailMax));
  const displayedManualHitResults = getManualHitResultsFromState(
    hitResultMode === "accuracy" ? result?.state ?? null : null,
    manualHitResults,
  );
  const calculationInputKey = useMemo(
    () =>
      JSON.stringify({
        accuracy,
        beatmapId: beatmap.id,
        combo: comboLockedToMax ? null : displayedCombo,
        largeTickMisses: displayedLargeTickMisses,
        maxLargeTicks: largeTickMax,
        maxSliderTails: sliderTailMax,
        misses,
        mode: beatmap.mode,
        mods: debouncedModsPayload,
        n50: manualHitResults.n50,
        n100: manualHitResults.n100,
        n300: manualHitResults.n300,
        nGeki: manualHitResults.nGeki,
        nKatu: manualHitResults.nKatu,
        resultMode: hitResultMode,
        sliderTailMisses: displayedSliderTailMisses,
      }),
    [
      accuracy,
      beatmap.id,
      beatmap.mode,
      comboLockedToMax,
      debouncedModsPayload,
      displayedCombo,
      displayedLargeTickMisses,
      displayedSliderTailMisses,
      hitResultMode,
      largeTickMax,
      manualHitResults.n50,
      manualHitResults.n100,
      manualHitResults.n300,
      manualHitResults.nGeki,
      manualHitResults.nKatu,
      misses,
      sliderTailMax,
    ],
  );
  const leaderboardRank = getEstimatedLeaderboardRank(leaderboard, result);

  useEffect(() => {
    const timeout = window.setTimeout(() => {
      setDebouncedModsPayload(modsPayload);
    }, modConfigurationDebounceMs);

    return () => {
      window.clearTimeout(timeout);
    };
  }, [modsPayload, modsPayloadKey]);

  useEffect(() => {
    const controller = new AbortController();
    const timeout = window.setTimeout(() => {
      setLoading(true);
      setError(null);
      const manualMode = hitResultMode === "manual";

      void fetch(`/api/beatmaps/${beatmap.id}/pp`, {
        body: JSON.stringify({
          accuracy: manualMode ? undefined : accuracy,
          combo: comboLockedToMax ? null : displayedCombo,
          largeTickHits:
            manualMode && beatmap.mode === "osu" && displayedLargeTickMisses > 0
              ? Math.max(0, largeTickMax - displayedLargeTickMisses)
              : undefined,
          misses,
          mode: beatmap.mode,
          mods: debouncedModsPayload,
          n50: manualMode && beatmap.mode !== "taiko" ? manualHitResults.n50 : undefined,
          n100: manualMode ? manualHitResults.n100 : undefined,
          n300: manualMode ? manualHitResults.n300 : undefined,
          nGeki: manualMode && beatmap.mode === "mania" ? manualHitResults.nGeki : undefined,
          nKatu: manualMode && (beatmap.mode === "fruits" || beatmap.mode === "mania") ? manualHitResults.nKatu : undefined,
          sliderEndHits:
            manualMode && beatmap.mode === "osu" && displayedSliderTailMisses > 0
              ? Math.max(0, sliderTailMax - displayedSliderTailMisses)
              : undefined,
        }),
        headers: {
          "Content-Type": "application/json",
        },
        method: "POST",
        signal: controller.signal,
      })
        .then(async (response) => {
          const data = await response.json();

          if (!response.ok) {
            throw new Error(typeof data?.error === "string" ? data.error : "Failed to calculate performance.");
          }

          setResult(data as BeatmapPpCalculatorResponse);
          setSettledCalculationKey(calculationInputKey);
        })
        .catch((fetchError) => {
          if (controller.signal.aborted) {
            return;
          }

          setError(fetchError instanceof Error ? fetchError.message : "Failed to calculate performance.");
          setResult(null);
          setSettledCalculationKey(calculationInputKey);
        })
        .finally(() => {
          if (!controller.signal.aborted) {
            setLoading(false);
          }
        });
    }, ppCalculationDebounceMs);

    return () => {
      window.clearTimeout(timeout);
      controller.abort();
    };
  }, [
    accuracy,
    beatmap.id,
    beatmap.mode,
    calculationInputKey,
    comboLockedToMax,
    debouncedModsPayload,
    debouncedModsPayloadKey,
    displayedCombo,
    displayedLargeTickMisses,
    displayedSliderTailMisses,
    hitResultMode,
    largeTickMax,
    manualHitResults.n50,
    manualHitResults.n100,
    manualHitResults.n300,
    manualHitResults.nGeki,
    manualHitResults.nKatu,
    misses,
    sliderTailMax,
  ]);

  const toggleMod = (acronym: string) => {
    if (acronym === "NM") {
      setSelectedMods([]);
      setComboLockedToMax(true);
      return;
    }

    setSelectedMods((currentMods) => {
      if (currentMods.includes(acronym)) {
        return currentMods.filter((mod) => mod !== acronym);
      }

      const conflicts = getConflicts(acronym);
      return availableMods.filter((mod) => mod !== "NM" && (mod === acronym || (currentMods.includes(mod) && !conflicts.has(mod))));
    });
    setComboLockedToMax(true);
  };

  const setModSetting = (acronym: string, schema: SettingSchema, value: SettingValue) => {
    setSettingsByMod((currentSettings) => {
      const modSettings = {
        ...(currentSettings[acronym] ?? {}),
      };

      if (value === schema.defaultValue) {
        delete modSettings[schema.key];
      } else {
        modSettings[schema.key] = value;
      }

      const nextSettings = {
        ...currentSettings,
      };

      if (Object.keys(modSettings).length === 0) {
        delete nextSettings[acronym];
      } else {
        nextSettings[acronym] = modSettings;
      }

      return nextSettings;
    });
    setComboLockedToMax(true);
  };

  const clearConfigurableModSettings = () => {
    const acronymsToClear = new Set(configurableMods.map(({ acronym }) => acronym));

    setSettingsByMod((currentSettings) => {
      let changed = false;
      const nextSettings = {
        ...currentSettings,
      };

      acronymsToClear.forEach((acronym) => {
        if (nextSettings[acronym] != null) {
          delete nextSettings[acronym];
          changed = true;
        }
      });

      return changed ? nextSettings : currentSettings;
    });
    setComboLockedToMax(true);
  };

  const setManualHitResult = (key: ManualHitResultKey, value: number) => {
    const sourceResults = getManualHitResultsFromState(
      hitResultMode === "accuracy" ? result?.state ?? null : null,
      manualHitResults,
    );

    setHitResultMode("manual");
    setManualHitResults(
      balanceManualHitResults(
        {
          ...sourceResults,
          [key]: Math.round(value),
        },
        {
          balancedKeys: balancedHitResultKeys,
          editedKey: key,
          hitResultMax,
          misses,
        },
      ),
    );
  };

  const configurableMods = selectedMods
    .map((acronym) => ({
      acronym,
      schemas: getSettingSchemas(acronym, beatmap, settingsByMod[acronym]),
    }))
    .filter((entry) => entry.schemas.length > 0);
  const hasConfigurableModSettings = configurableMods.some(
    ({ acronym }) => Object.keys(settingsByMod[acronym] ?? {}).length > 0,
  );
  const performanceRows = getPerformanceRows(result, selectedModSet.has("FL"));
  const displayedDifficultyRating =
    typeof result?.difficulty.stars === "number" && Number.isFinite(result.difficulty.stars)
      ? result.difficulty.stars
      : beatmap.difficulty_rating;
  const difficultyBadgeBeatmap = {
    ...beatmap,
    difficulty_rating: displayedDifficultyRating,
  };
  const comparisonRows = comparisonModSets.map((comparison) => {
    const calculated = result?.comparisonPp.find((entry) => entry.label === comparison.label);

    return {
      accuracyPp: calculated?.accuracyPp ?? [],
      label: comparison.label,
      mods: calculated?.mods.length ? calculated.mods : comparison.mods,
      stars: calculated?.stars,
    };
  });
  const ppPending = loading || modsDebouncing || settledCalculationKey !== calculationInputKey;

  return (
    <section
      aria-busy={loading || modsDebouncing}
      className="relative mx-0.5 overflow-hidden rounded-xl bg-osu-b4 text-[0.95rem] shadow-[0_12px_28px_rgba(0,0,0,0.28)] lg:mx-2.5"
    >
      <div className="bg-osu-b5/90 px-3 py-3 lg:px-4">
        <div className="grid gap-3 lg:grid-cols-[auto_minmax(0,1fr)] lg:items-center">
          <div className="flex items-start">
            <BeatmapDifficultyPill
              beatmap={difficultyBadgeBeatmap}
              className="h-6 min-w-14 px-2.5 text-sm"
            />
          </div>

          <div className="grid grid-cols-2 gap-3 md:grid-cols-4">
            <DifficultyStat
              baseValue={beatmap.cs}
              label="Circle Size"
              value={result?.attributes.cs}
            />
            <DifficultyStat
              baseValue={beatmap.ar}
              label="Approach Rate"
              value={result?.attributes.ar}
            />
            <DifficultyStat
              baseValue={beatmap.accuracy}
              label="Accuracy"
              value={result?.attributes.od}
            />
            <DifficultyStat
              baseValue={beatmap.drain}
              label="HP Drain"
              value={result?.attributes.hp}
            />
          </div>
        </div>
      </div>

      <div className="flex flex-col items-center gap-2 bg-osu-b6/75 px-3 py-2.5 lg:px-4 sm:flex-row sm:justify-between">
        <div className="group/mods flex max-w-full flex-wrap items-center justify-center">
          {availableMods.map((mod) => {
            const active = mod === "NM" ? selectedMods.length === 0 : selectedModSet.has(mod);
            const definition = getModDefinition(mod);
            const payloadMod = modsPayload.find((entry) => entry.acronym === mod);

            return (
              <button
                aria-label={definition.name}
                aria-pressed={active}
                className={cn(
                  "m-[2px] cursor-pointer rounded-sm transition-opacity focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-osu-h1/80",
                  selectedMods.length > 0
                    ? active
                      ? "opacity-100"
                      : "opacity-38 hover:opacity-100"
                    : "opacity-100 group-hover/mods:opacity-55 hover:!opacity-100",
                )}
                key={mod}
                onClick={() => toggleMod(mod)}
                title={definition.name}
                type="button"
              >
                <ModBadge mod={payloadMod ?? { acronym: mod }} />
              </button>
            );
          })}
        </div>

        <Popover>
          <PopoverTrigger asChild>
            <button
              className={customizeButtonClassName}
              disabled={configurableMods.length === 0}
              type="button"
            >
              <Settings2 className="size-3" />
              Customize
            </button>
          </PopoverTrigger>
          {configurableMods.length > 0 ? (
            <PopoverContent align="end" className="w-[min(28rem,calc(100vw-2rem))]" side="bottom">
              <PopoverHeader className="flex-row items-center justify-between gap-3">
                <PopoverTitle>Customize</PopoverTitle>
                <button
                  className="rounded-full px-2 py-1 text-[11px] font-semibold uppercase leading-none text-white/48 transition-colors hover:bg-white/10 hover:text-white disabled:cursor-default disabled:text-white/22 disabled:hover:bg-transparent"
                  disabled={!hasConfigurableModSettings}
                  onClick={clearConfigurableModSettings}
                  type="button"
                >
                  Clear
                </button>
              </PopoverHeader>
              <div className="flex flex-col gap-3 p-3">
                {configurableMods.map(({ acronym, schemas }) => {
                  const definition = getModDefinition(acronym);

                  return (
                    <div
                      className="min-w-0 rounded-md bg-osu-b5/82 p-2.5 shadow-[inset_0_0_0_1px_rgba(255,255,255,0.045)]"
                      key={acronym}
                    >
                      <div className="mb-2 flex min-w-0 items-center gap-2">
                        <ModBadge mod={modsPayload.find((mod) => mod.acronym === acronym) ?? { acronym }} />
                        <div className="truncate text-xs font-semibold text-white">{definition.name}</div>
                      </div>
                      <FieldGroup className="gap-2.5">
                        {schemas.map((schema) => (
                          <ModSettingControl
                            acronym={acronym}
                            key={`${acronym}-${schema.key}`}
                            onChange={setModSetting}
                            schema={schema}
                            settings={settingsByMod[acronym]}
                          />
                        ))}
                      </FieldGroup>
                    </div>
                  );
                })}
              </div>
            </PopoverContent>
          ) : null}
        </Popover>
      </div>

      {onToggleCollapsed == null ? null : (
        <div className="flex justify-center border-t border-white/5 px-3">
          <button
            aria-expanded={!collapsed}
            className="inline-flex items-center gap-1 rounded-full px-3 py-1 text-xs text-white/60 transition-colors hover:bg-white/10 hover:text-white"
            onClick={onToggleCollapsed}
            type="button"
          >
            {collapsed ? "show performance" : "hide performance"}
            {collapsed ? <ChevronDown className="size-4" /> : <ChevronUp className="size-4" />}
          </button>
        </div>
      )}

      <div
        className={cn(
          "grid gap-3 p-3 xl:grid-cols-[minmax(18rem,0.95fr)_minmax(18rem,0.8fr)_minmax(21rem,1.05fr)]",
          collapsed && "hidden",
        )}
      >
        <div className="flex min-w-0 flex-col gap-3">
          <div className="rounded-lg bg-osu-b5/62 p-3 shadow-[inset_0_0_0_1px_rgba(255,255,255,0.045)]">
            <div className="flex items-center gap-1.5 text-xs font-semibold uppercase text-white/50">
              <Settings2 className="size-3.5" />
              <span>Settings</span>
            </div>
            <div className="mt-2 grid gap-2 sm:grid-cols-2 xl:grid-cols-1 2xl:grid-cols-2">
              <CompactNumberInput
                inputMode="numeric"
                label="Max Combo"
                max={Math.max(0, maxCombo)}
                min={0}
                onChange={(value) => {
                  setComboLockedToMax(false);
                  setCombo(Math.round(clamp(value, 0, Math.max(0, maxCombo))));
                }}
                value={Math.round(displayedCombo)}
              />
              <CompactNumberInput
                label="Accuracy"
                max={100}
                min={0}
                onChange={(value) => {
                  setHitResultMode("accuracy");
                  setAccuracy(value);
                }}
                precision={2}
                step={0.01}
                value={accuracy}
              />
              {hitResultInputs.map((input) => (
                <CompactNumberInput
                  inputMode="numeric"
                  key={input.key}
                  label={input.label}
                  max={Math.max(100000, hitResultMax, displayedManualHitResults[input.key])}
                  min={0}
                  onChange={(value) => setManualHitResult(input.key, value)}
                  value={displayedManualHitResults[input.key]}
                />
              ))}
              <CompactNumberInput
                inputMode="numeric"
                label="Misses"
                max={hitResultMax}
                min={0}
                onChange={(value) => {
                  const roundedMisses = Math.round(value);
                  const sourceResults = getManualHitResultsFromState(
                    hitResultMode === "accuracy" ? result?.state ?? null : null,
                    manualHitResults,
                  );

                  setHitResultMode("manual");
                  setMisses(roundedMisses);
                  setManualHitResults(
                    balanceManualHitResults(sourceResults, {
                      balancedKeys: balancedHitResultKeys,
                      hitResultMax,
                      misses: roundedMisses,
                    }),
                  );
                }}
                value={misses}
              />
              {beatmap.mode === "osu" ? (
                <>
                  <CompactNumberInput
                    inputMode="numeric"
                    label="Large Tick Misses"
                    max={Math.max(0, largeTickMax)}
                    min={0}
                    onChange={(value) => {
                      setHitResultMode("manual");
                      setLargeTickMisses(Math.round(clamp(value, 0, Math.max(0, largeTickMax))));
                    }}
                    value={displayedLargeTickMisses}
                  />
                  <CompactNumberInput
                    inputMode="numeric"
                    label="Slider Tail Misses"
                    max={Math.max(0, sliderTailMax)}
                    min={0}
                    onChange={(value) => {
                      setHitResultMode("manual");
                      setSliderTailMisses(Math.round(clamp(value, 0, Math.max(0, sliderTailMax))));
                    }}
                    value={displayedSliderTailMisses}
                  />
                </>
              ) : null}
            </div>
          </div>

          <div className="rounded-lg bg-osu-b5/62 p-3 shadow-[inset_0_0_0_1px_rgba(255,255,255,0.045)]">
            <div className="flex items-center gap-1.5 text-xs font-semibold uppercase text-white/50">
              <Activity className="size-3.5" />
              <span>Performance Breakdown</span>
            </div>
            {performanceRows.length > 0 ? (
              <div className="mt-2 flex flex-col gap-2.5">
                {performanceRows.map(([label, value, available]) => (
                  <BreakdownRow available={available} key={label} label={label} value={value} />
                ))}
              </div>
            ) : null}
          </div>
        </div>

        <div className="min-w-0 text-center">
          <div className="rounded-lg bg-osu-b5/62 p-3 shadow-[inset_0_0_0_1px_rgba(255,255,255,0.045)]">
            <div className="flex items-center justify-center gap-1.5 text-xs font-semibold uppercase text-white/50">
              <Gauge className="size-3.5" />
              <span>Simulated Performance</span>
            </div>
            <div className="mt-3 flex min-h-32 flex-col items-center justify-center">
              <div className="flex min-h-14 items-center justify-center text-5xl font-semibold leading-none tracking-normal text-white">
                {ppPending ? (
                  <Spinner className="size-10 text-white/75" />
                ) : (
                  <span className="flex items-baseline justify-center">
                    {formatCompactPp(result?.pp)}
                    <span className="text-xl text-white/55">pp</span>
                  </span>
                )}
              </div>
              <div className="mt-3 flex items-center justify-center gap-3 text-sm font-semibold tabular-nums text-white/68">
                <span>{leaderboardRank}</span>
                <Separator className="h-5 bg-white/15" orientation="vertical" />
                <span>{result?.calculatedAccuracy == null ? "-" : `${formatNumber(result.calculatedAccuracy, 2)}%`}</span>
              </div>
              {error != null ? (
                <div className="mt-3 text-xs font-semibold text-osu-red-2">{error}</div>
              ) : null}
            </div>
          </div>

          <div className="mt-2 grid grid-cols-3 gap-2">
            {referenceAccuracyColumns.map((accuracyColumn) => (
              <PpValueCard
                key={accuracyColumn}
                label={`${accuracyColumn}%`}
                value={getAccuracyPp(result, accuracyColumn)}
              />
            ))}
          </div>
          <div className="mt-2 grid grid-cols-2 gap-2">
            <PpValueCard label="FC" value={result?.fcPp} />
            <PpValueCard label="Miss Loss" tone="loss" value={result?.missLossPp} />
          </div>
        </div>

        <div className="min-w-0 overflow-x-auto rounded-lg bg-osu-b5/62 p-3 shadow-[inset_0_0_0_1px_rgba(255,255,255,0.045)]">
          <div className="grid min-w-[22rem] grid-cols-[minmax(5.5rem,1fr)_4.5rem_repeat(3,minmax(3.25rem,0.7fr))] items-center gap-1.5 text-xs">
            <div className="font-semibold uppercase text-white/50">Mods</div>
            <div className="text-right font-semibold uppercase text-white/50">SR</div>
            {referenceAccuracyColumns.map((accuracyColumn) => (
              <div className="text-right font-semibold uppercase text-white/50" key={accuracyColumn}>
                {accuracyColumn}%
              </div>
            ))}
            {comparisonRows.map((row) => {
              const rowBeatmap =
                typeof row.stars === "number" && Number.isFinite(row.stars)
                  ? {
                      ...beatmap,
                      difficulty_rating: row.stars,
                    }
                  : null;

              return (
                <div className="contents" key={row.label}>
                  <div className="flex min-w-0 flex-wrap items-center gap-1 py-1">
                    {row.mods.map((mod) => (
                      <ModBadge key={`${row.label}-${mod.acronym}`} mod={mod} />
                    ))}
                  </div>
                  <div className="flex justify-end py-1">
                    {rowBeatmap == null ? (
                      <span className="text-sm font-semibold text-white/45">-</span>
                    ) : (
                      <BeatmapDifficultyPill
                        beatmap={rowBeatmap}
                        className="h-5 min-w-14 px-2 text-xs"
                      />
                    )}
                  </div>
                  {referenceAccuracyColumns.map((accuracyColumn) => (
                    <div className="py-1 text-right text-sm font-semibold tabular-nums text-white" key={`${row.label}-${accuracyColumn}`}>
                      {formatCompactPp(getComparisonAccuracyPp(row, accuracyColumn))}
                    </div>
                  ))}
                </div>
              );
            })}
          </div>
        </div>
      </div>
    </section>
  );
}

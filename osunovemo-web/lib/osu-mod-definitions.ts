export type OsuModType =
  | "Automation"
  | "Conversion"
  | "DifficultyIncrease"
  | "DifficultyReduction"
  | "Fun"
  | "System";

export type OsuModDefinition = {
  filename: string;
  name: string;
  type: OsuModType;
};

const modDefinitions: Record<string, OsuModDefinition> = {
  EZ: {
    name: "Easy",
    type: "DifficultyReduction",
    filename: "mod-easy.svg",
  },
  NF: {
    name: "No Fail",
    type: "DifficultyReduction",
    filename: "mod-no-fail.svg",
  },
  HT: {
    name: "Half Time",
    type: "DifficultyReduction",
    filename: "mod-half-time.svg",
  },
  DC: {
    name: "Daycore",
    type: "DifficultyReduction",
    filename: "mod-daycore.svg",
  },
  HR: {
    name: "Hard Rock",
    type: "DifficultyIncrease",
    filename: "mod-hard-rock.svg",
  },
  SD: {
    name: "Sudden Death",
    type: "DifficultyIncrease",
    filename: "mod-sudden-death.svg",
  },
  PF: {
    name: "Perfect",
    type: "DifficultyIncrease",
    filename: "mod-perfect.svg",
  },
  DT: {
    name: "Double Time",
    type: "DifficultyIncrease",
    filename: "mod-double-time.svg",
  },
  NC: {
    name: "Nightcore",
    type: "DifficultyIncrease",
    filename: "mod-nightcore.svg",
  },
  HD: {
    name: "Hidden",
    type: "DifficultyIncrease",
    filename: "mod-hidden.svg",
  },
  TC: {
    name: "Traceable",
    type: "DifficultyIncrease",
    filename: "mod-traceable.svg",
  },
  FL: {
    name: "Flashlight",
    type: "DifficultyIncrease",
    filename: "mod-flashlight.svg",
  },
  BL: {
    name: "Blinds",
    type: "DifficultyIncrease",
    filename: "mod-blinds.svg",
  },
  ST: {
    name: "Strict Tracking",
    type: "DifficultyIncrease",
    filename: "mod-strict-tracking.svg",
  },
  AC: {
    name: "Accuracy Challenge",
    type: "DifficultyIncrease",
    filename: "mod-accuracy-challenge.svg",
  },
  TP: {
    name: "Target Practice",
    type: "Conversion",
    filename: "mod-target-practice.svg",
  },
  DA: {
    name: "Difficulty Adjust",
    type: "Conversion",
    filename: "mod-difficulty-adjust.svg",
  },
  CL: {
    name: "Classic",
    type: "Conversion",
    filename: "mod-classic.svg",
  },
  RD: {
    name: "Random",
    type: "Conversion",
    filename: "mod-random.svg",
  },
  MR: {
    name: "Mirror",
    type: "Conversion",
    filename: "mod-mirror.svg",
  },
  AL: {
    name: "Alternate",
    type: "Conversion",
    filename: "mod-alternate.svg",
  },
  SG: {
    name: "Single Tap",
    type: "Conversion",
    filename: "mod-single-tap.svg",
  },
  AT: {
    name: "Autoplay",
    type: "Automation",
    filename: "mod-autoplay.svg",
  },
  CN: {
    name: "Cinema",
    type: "Automation",
    filename: "mod-cinema.svg",
  },
  RX: {
    name: "Relax",
    type: "Automation",
    filename: "mod-relax.svg",
  },
  AP: {
    name: "Autopilot",
    type: "Automation",
    filename: "mod-autopilot.svg",
  },
  SO: {
    name: "Spun Out",
    type: "Automation",
    filename: "mod-spun-out.svg",
  },
  TR: {
    name: "Transform",
    type: "Fun",
    filename: "mod-transform.svg",
  },
  WG: {
    name: "Wiggle",
    type: "Fun",
    filename: "mod-wiggle.svg",
  },
  SI: {
    name: "Spin In",
    type: "Fun",
    filename: "mod-spin-in.svg",
  },
  GR: {
    name: "Grow",
    type: "Fun",
    filename: "mod-grow.svg",
  },
  DF: {
    name: "Deflate",
    type: "Fun",
    filename: "mod-deflate.svg",
  },
  WU: {
    name: "Wind Up",
    type: "Fun",
    filename: "mod-wind-up.svg",
  },
  WD: {
    name: "Wind Down",
    type: "Fun",
    filename: "mod-wind-down.svg",
  },
  BR: {
    name: "Barrel Roll",
    type: "Fun",
    filename: "mod-barrel-roll.svg",
  },
  AD: {
    name: "Approach Different",
    type: "Fun",
    filename: "mod-approach-different.svg",
  },
  MU: {
    name: "Muted",
    type: "Fun",
    filename: "mod-muted.svg",
  },
  NS: {
    name: "No Scope",
    type: "Fun",
    filename: "mod-no-scope.svg",
  },
  MG: {
    name: "Magnetised",
    type: "Fun",
    filename: "mod-magnetised.svg",
  },
  RP: {
    name: "Repel",
    type: "Fun",
    filename: "mod-repel.svg",
  },
  AS: {
    name: "Adaptive Speed",
    type: "Fun",
    filename: "mod-adaptive-speed.svg",
  },
  FR: {
    name: "Freeze Frame",
    type: "Fun",
    filename: "mod-freeze-frame.svg",
  },
  BU: {
    name: "Bubbles",
    type: "Fun",
    filename: "mod-bubbles.svg",
  },
  SY: {
    name: "Synesthesia",
    type: "Fun",
    filename: "mod-synesthesia.svg",
  },
  DP: {
    name: "Depth",
    type: "Fun",
    filename: "mod-depth.svg",
  },
  BM: {
    name: "Bloom",
    type: "Fun",
    filename: "mod-bloom.svg",
  },
  TD: {
    name: "Touch Device",
    type: "System",
    filename: "mod-touch-device.svg",
  },
  SV2: {
    name: "Score V2",
    type: "System",
    filename: "mod-score-v2.svg",
  },
  SR: {
    name: "Simplified Rhythm",
    type: "DifficultyReduction",
    filename: "mod-simplified-rhythm.svg",
  },
  SW: {
    name: "Swap",
    type: "Conversion",
    filename: "mod-swap.svg",
  },
  CS: {
    name: "Constant Speed",
    type: "Conversion",
    filename: "mod-constant-speed.svg",
  },
  FF: {
    name: "Floating Fruits",
    type: "Fun",
    filename: "mod-floating-fruits.svg",
  },
  MF: {
    name: "Moving Fast",
    type: "Fun",
    filename: "mod-moving-fast.svg",
  },
  NR: {
    name: "No Release",
    type: "DifficultyReduction",
    filename: "mod-no-release.svg",
  },
  FI: {
    name: "Fade In",
    type: "DifficultyIncrease",
    filename: "mod-fade-in.svg",
  },
  CO: {
    name: "Cover",
    type: "DifficultyIncrease",
    filename: "mod-cover.svg",
  },
  DS: {
    name: "Dual Stages",
    type: "Conversion",
    filename: "mod-dual-stages.svg",
  },
  IN: {
    name: "Invert",
    type: "Conversion",
    filename: "mod-invert.svg",
  },
  HO: {
    name: "Hold Off",
    type: "Conversion",
    filename: "mod-hold-off.svg",
  },
  "1K": {
    name: "One Key",
    type: "Conversion",
    filename: "mod-one-key.svg",
  },
  "2K": {
    name: "Two Keys",
    type: "Conversion",
    filename: "mod-two-keys.svg",
  },
  "3K": {
    name: "Three Keys",
    type: "Conversion",
    filename: "mod-three-keys.svg",
  },
  "4K": {
    name: "Four Keys",
    type: "Conversion",
    filename: "mod-four-keys.svg",
  },
  "5K": {
    name: "Five Keys",
    type: "Conversion",
    filename: "mod-five-keys.svg",
  },
  "6K": {
    name: "Six Keys",
    type: "Conversion",
    filename: "mod-six-keys.svg",
  },
  "7K": {
    name: "Seven Keys",
    type: "Conversion",
    filename: "mod-seven-keys.svg",
  },
  "8K": {
    name: "Eight Keys",
    type: "Conversion",
    filename: "mod-eight-keys.svg",
  },
  "9K": {
    name: "Nine Keys",
    type: "Conversion",
    filename: "mod-nine-keys.svg",
  },
  "10K": {
    name: "Ten Keys",
    type: "Conversion",
    filename: "mod-ten-keys.svg",
  },
};

export default modDefinitions;

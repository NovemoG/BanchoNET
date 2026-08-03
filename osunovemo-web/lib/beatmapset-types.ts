import type { Ruleset } from "@/lib/rankings";

export type BeatmapsetShowUser = {
  avatar_url: string;
  country?: {
    code: string;
    name: string;
  } | null;
  country_code: string;
  id: number;
  profile_colour: string | null;
  team?: {
    flag_url?: string | null;
    id: number;
    name: string;
  } | null;
  username: string | null;
};

export type BeatmapLeaderboardScoreMod = {
  acronym: string;
  settings?: Record<string, boolean | number | string>;
};

export type BeatmapLeaderboardScore = {
  accuracy: number;
  beatmap_id: number;
  classic_total_score?: number | null;
  ended_at: string;
  id: number;
  is_perfect_combo: boolean;
  legacy_total_score?: number | null;
  max_combo: number;
  mods: BeatmapLeaderboardScoreMod[];
  passed: boolean;
  pp: number | null;
  rank: string;
  statistics: Partial<Record<string, number>>;
  total_score: number;
  user: BeatmapsetShowUser;
  user_id: number;
};

export type BeatmapLeaderboardResponse = {
  score_count: number;
  scores: BeatmapLeaderboardScore[];
};

export type BeatmapFailtimes = {
  exit: number[];
  fail: number[];
};

export type BeatmapDifficultyGraphSegment = {
  color: string;
  height: number;
  span: number;
};

export type BeatmapDifficultyGraphResponse = {
  rawSegmentCount: number;
  segments: BeatmapDifficultyGraphSegment[];
};

export type BeatmapsetShowBeatmap = {
  accuracy: number;
  ar: number;
  beatmapset_id: number;
  bpm: number;
  checksum: string;
  convert: boolean;
  count_circles: number;
  count_sliders: number;
  count_spinners: number;
  cs: number;
  current_user_playcount: number;
  current_user_tag_ids: number[];
  deleted_at: string | null;
  difficulty_rating: number;
  drain: number;
  failtimes: BeatmapFailtimes;
  hit_length: number;
  id: number;
  is_scoreable: boolean;
  last_updated: string;
  max_combo: number | null;
  mode: Ruleset;
  mode_int: number;
  owners: Array<{
    id: number;
    username: string | null;
  }>;
  passcount: number;
  playcount: number;
  ranked: number;
  status: string;
  top_tag_ids: number[];
  total_length: number;
  url: string;
  user_id: number;
  version: string;
};

export type BeatmapsetShowData = {
  anime_cover: boolean;
  artist: string;
  artist_unicode: string;
  availability?: {
    download_disabled?: boolean;
    more_information?: string | null;
  } | null;
  beatmaps: BeatmapsetShowBeatmap[];
  bpm: number | null;
  can_be_hyped: boolean;
  covers: {
    card: string;
    card2_x: string;
    cover: string;
    cover2_x: string;
    list: string;
    list2_x: string;
    slimcover: string;
    slimcover2_x: string;
  };
  creator: string;
  current_nominations: Array<{
    beatmapset_id: number;
    reset: boolean;
    rulesets: Ruleset[];
    user_id: number;
  }>;
  deleted_at: string | null;
  description: {
    description?: string | null;
  };
  discussion_enabled: boolean;
  discussion_locked: boolean;
  favourite_count: number;
  genre: {
    id: number;
    name: string;
  };
  genre_id: number;
  has_favourited: boolean;
  hype?: {
    current: number;
    required: number;
  } | null;
  id: number;
  is_scoreable: boolean;
  language: {
    id: number;
    name: string;
  };
  language_id: number;
  last_updated: string | null;
  legacy_thread_url?: string | null;
  nominations_summary?: {
    current: number;
    required_meta?: {
      main_ruleset: number;
      non_main_ruleset: number;
    };
  } | null;
  nsfw: boolean;
  offset: number;
  pack_tags: string[];
  play_count: number;
  preview_url: string;
  ranked: number;
  ranked_date: string | null;
  rating: number;
  ratings: number[];
  recent_favourites?: BeatmapsetShowUser[];
  related_tags?: Array<{
    id: number;
    name: string;
  }>;
  related_users?: BeatmapsetShowUser[];
  source: string;
  spotlight: boolean;
  status: string;
  storyboard: boolean;
  submitted_date: string | null;
  tags: string;
  title: string;
  title_unicode: string;
  track_id: number | null;
  user: BeatmapsetShowUser;
  user_id: number;
  version_count: number;
  video: boolean;
};

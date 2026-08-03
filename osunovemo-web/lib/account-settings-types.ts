export type AccountSettingsDownloadType = "all" | "direct" | "no_video";

export type AccountSettingsProfile = {
  discord: string;
  interests: string;
  location: string;
  occupation: string;
  signature: string;
  twitter: string;
  website: string;
};

export type AccountSettingsNotifications = {
  autoSubscribeTopics: boolean;
  beatmapsetDiscussionQualifiedProblemModes: string[];
  beatmapsetDisqualifyModes: string[];
  commentReply: boolean;
  delivery: Record<string, Record<"mail" | "push", boolean>>;
  newsPostSeries: string[];
};

export type AccountSettingsOptions = {
  beatmapsetDownload: AccountSettingsDownloadType;
  beatmapsetShowAnimeCover: boolean;
  beatmapsetShowNsfw: boolean;
  beatmapsetTitleShowOriginal: boolean;
};

export type AccountSettingsPrivacy = {
  hidePresence: boolean;
  pmFriendsOnly: boolean;
};

export type AccountSettingsCover = {
  /** Only set when the user uploaded their own image. */
  customUrl: string | null;
  /** Preset id, null when a custom upload is the effective cover. */
  id: string | null;
  url: string | null;
};

export type AccountSettingsData = {
  authenticatorEnabled: boolean;
  avatarUrl: string | null;
  countryCode: string | null;
  countryName: string | null;
  cover: AccountSettingsCover;
  coverUrl: string | null;
  isSupporter: boolean;
  notifications: AccountSettingsNotifications;
  options: AccountSettingsOptions;
  playstyles: string[];
  privacy: AccountSettingsPrivacy;
  profile: AccountSettingsProfile;
  sessionVerificationMethod: string | null;
  sessionVerified: boolean;
  userId: number;
  username: string;
};

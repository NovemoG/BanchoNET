// Intentionally not "server-only": the settings page re-maps the payload every endpoint returns,
// so this pure mapper has to be callable from the client component too. It reads nothing but its
// argument.

import type {AuthUser} from "@/lib/auth/types";
import type {
  AccountSettingsData,
  AccountSettingsDownloadType,
} from "@/lib/account-settings-types";
import {resolveAssetUrl} from "@/lib/osu-api-common";

type UnknownRecord = Record<string, unknown>;

const defaultDeliveryRows = [
  "beatmap_owner_change",
  "beatmapset:modding",
  "channel_message",
  "channel_mention",
  "channel_team",
  "comment_new",
  "forum_topic_reply",
  "mapping",
  "news_post",
] as const;

function asRecord(value: unknown): UnknownRecord | null {
  return value != null && typeof value === "object" && !Array.isArray(value)
    ? (value as UnknownRecord)
    : null;
}

function asString(value: unknown) {
  return typeof value === "string" ? value : null;
}

function asNumber(value: unknown) {
  return typeof value === "number" && Number.isFinite(value) ? value : null;
}

function asBoolean(value: unknown) {
  return typeof value === "boolean" ? value : null;
}

function asStringArray(value: unknown) {
  return Array.isArray(value)
    ? value.filter((item): item is string => typeof item === "string")
    : [];
}

function readString(source: UnknownRecord, keys: string[], fallback = "") {
  for (const key of keys) {
    const value = asString(source[key]);

    if (value != null) {
      return value;
    }
  }

  return fallback;
}

function readNullableString(source: UnknownRecord, keys: string[]) {
  const value = readString(source, keys, "");

  return value.length > 0 ? value : null;
}

function readBoolean(source: UnknownRecord, keys: string[], fallback = false) {
  for (const key of keys) {
    const value = asBoolean(source[key]);

    if (value != null) {
      return value;
    }
  }

  return fallback;
}

function readStringArray(source: UnknownRecord, keys: string[]) {
  for (const key of keys) {
    const value = asStringArray(source[key]);

    if (value.length > 0) {
      return value;
    }
  }

  return [];
}

function readRecord(source: UnknownRecord, keys: string[]) {
  for (const key of keys) {
    const value = asRecord(source[key]);

    if (value != null) {
      return value;
    }
  }

  return null;
}

function normalizeAssetUrl(value: unknown) {
  const stringValue = asString(value);

  if (stringValue == null || stringValue.length === 0) {
    return null;
  }

  try {
    return resolveAssetUrl(stringValue);
  } catch {
    return stringValue;
  }
}

function readProfileCustomization(user: UnknownRecord) {
  return readRecord(user, [
    "user_profile_customization",
    "profile_customization",
    "profileCustomization",
  ]);
}

function readSignature(user: UnknownRecord) {
  // user_sig is the forum signature; page is the user page. They are different things, so the
  // signature editor must read user_sig and only fall back to page for older payloads.
  const page = asRecord(user.page);

  return readString(user, ["user_sig"], readString(page ?? {}, ["raw"], ""));
}

function readDownloadType(value: unknown): AccountSettingsDownloadType {
  return value === "direct" || value === "no_video" ? value : "all";
}

function buildDefaultDelivery() {
  return Object.fromEntries(
    defaultDeliveryRows.map((row) => [
      row,
      {
        mail: true,
        push: row.startsWith("channel_"),
      },
    ]),
  ) as AccountSettingsData["notifications"]["delivery"];
}

function readNotificationOptionDetails(notificationOptions: unknown, name: string) {
  // The API sends an array of {id, name, details}, matching osu-web. The keyed-object form is
  // kept as a fallback so a stale deploy does not blank the whole page.
  if (Array.isArray(notificationOptions)) {
    const option = notificationOptions
      .map((entry) => asRecord(entry))
      .find((entry) => entry != null && entry.name === name);

    return asRecord(option?.details);
  }

  const optionsRecord = asRecord(notificationOptions);

  if (optionsRecord == null) {
    return null;
  }

  const option = asRecord(optionsRecord[name]);

  return asRecord(option?.details);
}

function readNotificationBool(
  notificationOptions: unknown,
  name: string,
  key: string,
  fallback: boolean,
) {
  const details = readNotificationOptionDetails(notificationOptions, name);

  return details == null ? fallback : readBoolean(details, [key], fallback);
}

function readNotificationArray(notificationOptions: unknown, name: string, key: string) {
  const details = readNotificationOptionDetails(notificationOptions, name);

  return details == null ? [] : readStringArray(details, [key]);
}

export function buildAccountSettingsData(user: AuthUser): AccountSettingsData {
  const userRecord = user as UnknownRecord;
  const country = asRecord(userRecord.country);
  const cover = asRecord(userRecord.cover);
  const customization = readProfileCustomization(userRecord);
  const notificationOptions =
    userRecord.user_notification_options ??
    userRecord.notification_options ??
    userRecord.notificationOptions;
  const sessionVerificationMethod = readNullableString(userRecord, ["session_verification_method"]);
  const delivery = buildDefaultDelivery();

  for (const row of Object.keys(delivery)) {
    delivery[row] = {
      mail: readNotificationBool(notificationOptions, row, "mail", delivery[row].mail),
      push: readNotificationBool(notificationOptions, row, "push", delivery[row].push),
    };
  }

  return {
    authenticatorEnabled:
      sessionVerificationMethod === "totp" ||
      sessionVerificationMethod === "app" ||
      sessionVerificationMethod === "authenticator",
    avatarUrl: normalizeAssetUrl(userRecord.avatar_url),
    countryCode: readNullableString(userRecord, ["country_code"]),
    countryName: readNullableString(country ?? {}, ["name"]),
    cover: {
      customUrl: normalizeAssetUrl(cover?.custom_url),
      id: readNullableString(cover ?? {}, ["id"]),
      url: normalizeAssetUrl(cover?.url),
    },
    coverUrl: normalizeAssetUrl(cover?.url ?? cover?.custom_url),
    isSupporter: readBoolean(userRecord, ["is_supporter", "osu_subscriber"], false),
    notifications: {
      autoSubscribeTopics: readBoolean(userRecord, ["user_notify"], false),
      beatmapsetDiscussionQualifiedProblemModes: readNotificationArray(
        notificationOptions,
        "beatmapset_discussion_qualified_problem",
        "modes",
      ),
      beatmapsetDisqualifyModes: readNotificationArray(
        notificationOptions,
        "beatmapset_disqualify",
        "modes",
      ),
      commentReply: readNotificationBool(notificationOptions, "comment_new", "comment_reply", true),
      delivery,
      newsPostSeries: readNotificationArray(notificationOptions, "news_post", "series"),
    },
    options: {
      beatmapsetDownload: readDownloadType(customization?.beatmapset_download),
      beatmapsetShowAnimeCover: readBoolean(customization ?? {}, ["beatmapset_show_anime_cover"], true),
      beatmapsetShowNsfw: readBoolean(customization ?? {}, ["beatmapset_show_nsfw"], false),
      beatmapsetTitleShowOriginal: readBoolean(customization ?? {}, ["beatmapset_title_show_original"], false),
    },
    playstyles: readStringArray(userRecord, ["playstyle", "playstyles", "osu_playstyle"]),
    privacy: {
      hidePresence: readBoolean(userRecord, ["hide_presence"], false),
      pmFriendsOnly: readBoolean(userRecord, ["pm_friends_only"], false),
    },
    profile: {
      discord: readString(userRecord, ["discord", "user_discord"]),
      interests: readString(userRecord, ["interests", "user_interests"]),
      location: readString(userRecord, ["location", "user_from"]),
      occupation: readString(userRecord, ["occupation", "user_occ"]),
      signature: readSignature(userRecord),
      twitter: readString(userRecord, ["twitter", "user_twitter"]),
      website: readString(userRecord, ["website", "user_website"]),
    },
    sessionVerificationMethod,
    sessionVerified: readBoolean(userRecord, ["session_verified"], false),
    userId: asNumber(userRecord.id) ?? user.id,
    username: user.username,
  };
}

import type { AuthUser } from "@/lib/auth/types";
import { parseApiErrorBody } from "@/lib/osu-api-errors";
import type { AccountSettingsDownloadType } from "@/lib/account-settings-types";

export type AccountSession = {
  id: string;
  current: boolean;
  created_at: string;
  last_used_at: string | null;
  expires_at: string;
  ip: string | null;
  user_agent: string | null;
  client: { id: number; name: string | null } | null;
};

export class AccountSettingsError extends Error {
  readonly status: number;
  readonly fields: Record<string, string[]>;

  constructor(message: string, status: number, fields: Record<string, string[]>) {
    super(message);
    this.name = "AccountSettingsError";
    this.status = status;
    this.fields = fields;
  }
}

async function request(path: string, init: RequestInit) {
  const response = await fetch(`/api/account/${path}`, { ...init, cache: "no-store" });

  if (response.status === 204) {
    return null;
  }

  const body = await response.text();

  if (!response.ok) {
    const parsed = parseApiErrorBody(body, response.statusText);

    throw new AccountSettingsError(parsed.message, response.status, parsed.fields);
  }

  return body.length > 0 ? (JSON.parse(body) as AuthUser) : null;
}

function form(fields: Record<string, string | string[] | undefined>) {
  const params = new URLSearchParams();

  for (const [key, value] of Object.entries(fields)) {
    if (value === undefined) {
      continue;
    }

    if (Array.isArray(value)) {
      // An explicit empty entry is how "clear this list" is expressed, matching osu-web.
      if (value.length === 0) {
        params.append(key, "");
        continue;
      }

      for (const item of value) {
        params.append(key, item);
      }

      continue;
    }

    params.append(key, value);
  }

  return params;
}

function send(path: string, params: URLSearchParams, method: "PUT" | "POST" = "PUT") {
  return request(path, {
    method,
    headers: { "Content-Type": "application/x-www-form-urlencoded" },
    body: params,
  });
}

export type AccountProfileInput = {
  discord?: string;
  hidePresence?: boolean;
  interests?: string;
  location?: string;
  occupation?: string;
  playstyles?: string[];
  pmFriendsOnly?: boolean;
  signature?: string;
  twitter?: string;
  userNotify?: boolean;
  website?: string;
};

function bool(value: boolean | undefined) {
  return value === undefined ? undefined : value ? "1" : "0";
}

/**
 * Sends only the keys it was given. The server treats an absent key as "leave alone" and an empty
 * value as "clear", so every section can post just its own fields to this one endpoint.
 */
export function updateAccount(input: AccountProfileInput) {
  return send(
    "account",
    form({
      "user[user_from]": input.location,
      "user[user_interests]": input.interests,
      "user[user_occ]": input.occupation,
      "user[user_twitter]": input.twitter,
      "user[user_discord]": input.discord,
      "user[user_website]": input.website,
      "user[user_sig]": input.signature,
      "user[user_notify]": bool(input.userNotify),
      "user[pm_friends_only]": bool(input.pmFriendsOnly),
      "user[hide_presence]": bool(input.hidePresence),
      "user[osu_playstyle][]": input.playstyles,
    }),
  );
}

export function updateOptions(input: {
  beatmapsetDownload?: AccountSettingsDownloadType;
  beatmapsetShowAnimeCover?: boolean;
  beatmapsetShowNsfw?: boolean;
  beatmapsetTitleShowOriginal?: boolean;
}) {
  return send(
    "account/options",
    form({
      "user_profile_customization[beatmapset_download]": input.beatmapsetDownload,
      "user_profile_customization[beatmapset_show_nsfw]": bool(input.beatmapsetShowNsfw),
      "user_profile_customization[beatmapset_show_anime_cover]": bool(input.beatmapsetShowAnimeCover),
      "user_profile_customization[beatmapset_title_show_original]": bool(input.beatmapsetTitleShowOriginal),
    }),
  );
}

export type NotificationOptionInput = {
  name: string;
  details: Record<string, boolean | string[]>;
};

/** Serialises osu-web's indexed shape: user_notification_options[0][details][modes][]=osu */
export function updateNotificationOptions(rows: NotificationOptionInput[]) {
  const params = new URLSearchParams();

  rows.forEach((row, index) => {
    params.append(`user_notification_options[${index}][name]`, row.name);

    for (const [key, value] of Object.entries(row.details)) {
      const base = `user_notification_options[${index}][details][${key}]`;

      if (Array.isArray(value)) {
        if (value.length === 0) {
          params.append(`${base}[]`, "");
          continue;
        }

        for (const item of value) {
          params.append(`${base}[]`, item);
        }

        continue;
      }

      params.append(base, value ? "1" : "0");
    }
  });

  return send("account/notification-options", params);
}

export function updatePassword(input: {
  currentPassword: string;
  password: string;
  passwordConfirmation: string;
}) {
  return send(
    "account/password",
    form({
      "user[current_password]": input.currentPassword,
      "user[password]": input.password,
      "user[password_confirmation]": input.passwordConfirmation,
    }),
  );
}

export function updateEmail(input: {
  currentPassword: string;
  email: string;
  emailConfirmation: string;
}) {
  return send(
    "account/email",
    form({
      "user[current_password]": input.currentPassword,
      "user[user_email]": input.email,
      "user[user_email_confirmation]": input.emailConfirmation,
    }),
  );
}

export function uploadAvatar(file: File) {
  const data = new FormData();
  data.append("avatar_file", file);

  return request("account/avatar", { method: "POST", body: data });
}

export function updateCover(input: { coverId?: number; file?: File }) {
  if (input.file != null) {
    const data = new FormData();
    data.append("cover_file", input.file);

    return request("account/cover", { method: "POST", body: data });
  }

  return send("account/cover", form({ cover_id: String(input.coverId) }), "POST");
}

export async function listSessions(): Promise<AccountSession[]> {
  const response = await fetch("/api/account/account/sessions", { cache: "no-store" });
  const body = await response.text();

  if (!response.ok) {
    const parsed = parseApiErrorBody(body, response.statusText);

    throw new AccountSettingsError(parsed.message, response.status, parsed.fields);
  }

  return (JSON.parse(body) as { sessions: AccountSession[] }).sessions;
}

export async function revokeSession(id: string) {
  const response = await fetch(`/api/account/account/sessions/${id}`, {
    method: "DELETE",
    cache: "no-store",
  });

  if (!response.ok) {
    const parsed = parseApiErrorBody(await response.text(), response.statusText);

    throw new AccountSettingsError(parsed.message, response.status, parsed.fields);
  }
}

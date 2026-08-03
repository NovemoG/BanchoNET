"use client";

import {
  type FormEvent,
  type KeyboardEvent,
  useCallback,
  useEffect,
  useMemo,
  useRef,
  useState,
} from "react";
import Link from "next/link";
import {
  ChevronRight,
  Hash,
  Inbox,
  Megaphone,
  MessageCircle,
  Minus,
  Search,
  Users,
  X,
} from "lucide-react";
import { usePathname } from "next/navigation";
import useSWR from "swr";
import { useSession } from "@/components/auth/session-provider";
import {
  ChatComposerShell,
  ChatConversationFrame,
  getChannelMessageLengthLimit,
  getMessageChannelId,
  getMessageId,
  getMessageNumericId,
  parseOutgoingMessage,
  type ParsedOutgoingMessage,
} from "@/components/chat/chat-conversation";
import type { ChatBeatmapCardBeatmap } from "@/components/chat/beatmap-card-message";
import type { ChatProfileCardUser } from "@/components/chat/profile-card-message";
import { useRealtime } from "@/components/realtime/realtime-provider";
import type {
  ChatChannel,
  ChatChannelType,
  ChatMessage,
  ChatMessagesNewEventData,
  ChatUpdatesResponse,
  ChatUser,
} from "@/lib/chat/types";
import { parseBeatmapsetPageHash } from "@/lib/beatmapset-page-hash";
import type { BeatmapsetShowBeatmap, BeatmapsetShowData } from "@/lib/beatmapset-types";
import type { ProfileUser } from "@/lib/profile";
import { cn } from "@/lib/utils";

type NowPlayingContext = ChatBeatmapCardBeatmap & {
  label: string;
};

type ProfileCommandUser = ChatProfileCardUser;

type ProfileCommandMode = "fruits" | "mania" | "osu" | "taiko";

type ProfileCommandModeOption = {
  description: string;
  label: string;
  value: ProfileCommandMode;
};

type ProfileCommandParameterDescriptor = {
  description: string;
  label: string;
  name: ProfileCommandParameterName;
  optional: boolean;
};

type ProfileCommandParameterName = "mode" | "user";

type ActiveCommand =
  | {
      activeParameter: null;
      kind: "np";
      parameters: Record<string, never>;
      value: NowPlayingContext;
    }
  | {
      activeParameter: ProfileCommandParameterName | null;
      error?: string;
      kind: "profile";
      parameters: {
        mode?: ProfileCommandMode;
        user?: ProfileCommandUser;
      };
      previewValue?: ProfileCommandUser;
      status: "error" | "loading" | "ready";
      value: ProfileCommandUser;
    };

type OverlayCommand = {
  description: string;
  kind: ActiveCommand["kind"];
  label: string;
  name: string;
};

const profileCommandModes: ProfileCommandModeOption[] = [
  {
    description: "standard",
    label: "osu",
    value: "osu",
  },
  {
    description: "drums",
    label: "taiko",
    value: "taiko",
  },
  {
    description: "catch",
    label: "fruits",
    value: "fruits",
  },
  {
    description: "keys",
    label: "mania",
    value: "mania",
  },
];

const maxOpenOverlayChats = 3;
const channelTypeOrder: ChatChannelType[] = ["PM", "PUBLIC", "ANNOUNCE", "TEAM", "GROUP"];
const channelTypeLabels: Record<string, string> = {
  ANNOUNCE: "announcements",
  GROUP: "groups",
  PM: "private messages",
  PUBLIC: "public channels",
  TEAM: "team",
};

const nowPlayingCommand: OverlayCommand = {
  description: "Share current beatmap",
  kind: "np",
  label: "Now playing",
  name: "/np",
};

const profileCommand: OverlayCommand = {
  description: "Share user profile stats",
  kind: "profile",
  label: "Profile",
  name: "/profile",
};

const profileCommandParameters: ProfileCommandParameterDescriptor[] = [
  {
    description: "id or username",
    label: "user",
    name: "user",
    optional: true,
  },
  {
    description: "osu, taiko, fruits or mania",
    label: "mode",
    name: "mode",
    optional: true,
  },
];
const commandSettingsMenuClassName = "absolute right-0 bottom-full left-0 z-[160] mb-2 overflow-hidden rounded-md bg-osu-b6 text-sm text-osu-c1 shadow-[0_12px_32px_rgba(0,0,0,0.35)]";

function getChannelId(channel: ChatChannel) {
  const id = channel.channel_id ?? channel.channelId;
  return typeof id === "number" ? id : null;
}

function getMessageUsers(messages: ChatMessage[]) {
  return messages.map((message) => message.sender).filter((user): user is ChatUser => user != null);
}

function mergeUsersById(current: Record<number, ChatUser>, users: ChatUser[]) {
  if (users.length === 0) {
    return current;
  }

  const next = { ...current };

  for (const user of users) {
    next[user.id] = {
      ...next[user.id],
      ...user,
    };
  }

  return next;
}

function getChannelName(channel: ChatChannel) {
  return channel.name ?? `channel ${channel.channel_id ?? channel.channelId ?? ""}`.trim();
}

function getChannelDescription(channel: ChatChannel) {
  return channel.description?.trim() ?? "";
}

function getChannelUserIds(channel: ChatChannel) {
  const ids = new Set<number>();

  if (Array.isArray(channel.users)) {
    for (const id of channel.users) {
      if (typeof id === "number") {
        ids.add(id);
      }
    }
  }

  for (const key of ["user_id", "userId", "target_id", "targetId"]) {
    const value = channel[key];

    if (typeof value === "number") {
      ids.add(value);
    }
  }

  return [...ids];
}

function channelMatchesSearch(
  channel: ChatChannel,
  query: string,
  usersById: Record<number, ChatUser>,
) {
  const normalizedQuery = query.trim().toLowerCase();

  if (normalizedQuery.length === 0) {
    return true;
  }

  const channelId = getChannelId(channel);
  const userIds = getChannelUserIds(channel);
  const values = [
    getChannelName(channel),
    getChannelDescription(channel),
    channel.type,
    channel.uuid,
    channelId == null ? null : String(channelId),
    ...userIds.map((id) => String(id)),
    ...userIds.map((id) => usersById[id]?.username ?? null),
  ];

  return values.some((value) => (
    typeof value === "string" && value.toLowerCase().includes(normalizedQuery)
  ));
}

function groupChannels(channels: ChatChannel[]) {
  return channels.reduce<Record<string, ChatChannel[]>>((groups, channel) => {
    const type = channel.type ?? "PUBLIC";
    groups[type] ??= [];
    groups[type].push(channel);

    return groups;
  }, {});
}

function getChannelIcon(channel: ChatChannel) {
  if (channel.type === "PM" && channel.icon != null) {
    return (
      <span
        className="h-[30px] w-[30px] shrink-0 rounded-full bg-cover bg-center"
        style={{ backgroundImage: `url(${JSON.stringify(channel.icon)})` }}
      />
    );
  }

  if (channel.type === "ANNOUNCE") {
    return <Megaphone className="h-4 w-4" />;
  }

  if (channel.type === "PM") {
    return <Inbox className="h-4 w-4" />;
  }

  if (channel.type === "TEAM" || channel.type === "GROUP") {
    return <Users className="h-4 w-4" />;
  }

  return <Hash className="h-4 w-4" />;
}

function getChannelActivityId(channel: ChatChannel, messagesByChannel: Record<number, ChatMessage[]>) {
  const channelId = getChannelId(channel);
  const messages = channelId == null ? [] : messagesByChannel[channelId] ?? [];
  const latestMessage = messages.at(-1);

  return Math.max(
    channel.last_message_id ?? channel.lastMessageId ?? 0,
    latestMessage == null ? 0 : getMessageNumericId(latestMessage),
  );
}

function mergeChannels(...channelGroups: ChatChannel[][]) {
  const channels = new Map<number, ChatChannel>();

  for (const group of channelGroups) {
    for (const channel of group) {
      const id = getChannelId(channel);

      if (id == null) {
        continue;
      }

      channels.set(id, {
        ...channels.get(id),
        ...channel,
      });
    }
  }

  return [...channels.values()];
}

function sortChannelsByActivity(channels: ChatChannel[], messagesByChannel: Record<number, ChatMessage[]>) {
  return [...channels].sort((left, right) => {
    const activityDelta = getChannelActivityId(right, messagesByChannel) - getChannelActivityId(left, messagesByChannel);

    if (activityDelta !== 0) {
      return activityDelta;
    }

    const leftOrder = channelTypeOrder.indexOf(left.type ?? "");
    const rightOrder = channelTypeOrder.indexOf(right.type ?? "");
    const orderDelta =
      (leftOrder === -1 ? channelTypeOrder.length : leftOrder) -
      (rightOrder === -1 ? channelTypeOrder.length : rightOrder);

    if (orderDelta !== 0) {
      return orderDelta;
    }

    return getChannelName(left).localeCompare(getChannelName(right));
  });
}

function appendMessages(currentMessages: ChatMessage[], incomingMessages: ChatMessage[]) {
  const messagesById = new Map(currentMessages.map((message) => [getMessageId(message), message]));

  for (const message of incomingMessages) {
    messagesById.set(getMessageId(message), {
      ...messagesById.get(getMessageId(message)),
      ...message,
    });
  }

  return [...messagesById.values()].sort((left, right) => getMessageNumericId(left) - getMessageNumericId(right));
}

function getMessageSenderId(message: ChatMessage) {
  const senderId = message.sender?.id ?? message.sender_id ?? message.senderId;

  return typeof senderId === "number" ? senderId : null;
}

function getRelevantMessageUsers(messages: ChatMessage[], users: ChatUser[]) {
  const senderIds = new Set<number>();

  for (const message of messages) {
    const senderId = getMessageSenderId(message);

    if (senderId != null) {
      senderIds.add(senderId);
    }
  }

  return [
    ...users.filter((user) => senderIds.has(user.id)),
    ...getMessageUsers(messages),
  ];
}

function pruneChannelRecord<T>(record: Record<number, T>, retainedChannelIds: ReadonlySet<number>) {
  let changed = false;
  const next: Record<number, T> = {};

  for (const [rawChannelId, value] of Object.entries(record)) {
    const channelId = Number(rawChannelId);

    if (retainedChannelIds.has(channelId)) {
      next[channelId] = value;
      continue;
    }

    changed = true;
  }

  return changed ? next : record;
}

function getRetainedUserIds(
  messagesByChannel: Record<number, ChatMessage[]>,
  retainedChannelIds: ReadonlySet<number>,
  channels: ChatChannel[],
) {
  const retainedUserIds = new Set<number>();

  for (const [rawChannelId, messages] of Object.entries(messagesByChannel)) {
    const channelId = Number(rawChannelId);

    if (!retainedChannelIds.has(channelId)) {
      continue;
    }

    for (const message of messages) {
      const senderId = getMessageSenderId(message);

      if (senderId != null) {
        retainedUserIds.add(senderId);
      }
    }
  }

  for (const channel of channels) {
    const channelId = getChannelId(channel);

    if (channelId == null || !retainedChannelIds.has(channelId)) {
      continue;
    }

    for (const userId of getChannelUserIds(channel)) {
      retainedUserIds.add(userId);
    }
  }

  return retainedUserIds;
}

function pruneUserRecord(record: Record<number, ChatUser>, retainedUserIds: ReadonlySet<number>) {
  let changed = false;
  const next: Record<number, ChatUser> = {};

  for (const [rawUserId, user] of Object.entries(record)) {
    const userId = Number(rawUserId);

    if (retainedUserIds.has(userId)) {
      next[userId] = user;
      continue;
    }

    changed = true;
  }

  return changed ? next : record;
}

function normalizeChatMessagesResponse(response: ChatMessage[] | { messages?: ChatMessage[]; users?: ChatUser[] }) {
  if (Array.isArray(response)) {
    return {
      messages: response,
      users: getMessageUsers(response),
    };
  }

  return {
    messages: response.messages ?? [],
    users: response.users ?? [],
  };
}

function chatUserFromSessionUser(user: { avatar_url?: string | null; id: number; username: string } | undefined) {
  if (user == null) {
    return null;
  }

  return {
    avatar_url: user.avatar_url ?? undefined,
    id: user.id,
    username: user.username,
  } satisfies ChatUser;
}

async function chatRequest<T>(path: string, init: RequestInit = {}) {
  const response = await fetch(`/api/chat/${path}`, {
    cache: "no-store",
    ...init,
    headers: {
      "Content-Type": "application/json",
      ...(init.headers ?? {}),
    },
  });

  if (!response.ok) {
    const body = await response.text();
    throw new Error(body.length > 0 ? body : `Chat request failed with ${response.status}.`);
  }

  if (response.status === 204) {
    return null as T;
  }

  return (await response.json()) as T;
}

function createUuid() {
  return globalThis.crypto?.randomUUID?.() ?? `${Date.now()}-${Math.random().toString(16).slice(2)}`;
}

function getBeatmapsetIdFromPathname(pathname: string | null) {
  const match = pathname?.match(/^\/beatmapsets\/(\d+)/);
  const id = match == null ? Number.NaN : Number.parseInt(match[1], 10);

  return Number.isFinite(id) ? id : null;
}

function getDefaultBeatmap(beatmapset: BeatmapsetShowData, beatmapId: number | null) {
  if (beatmapId != null) {
    const exact = beatmapset.beatmaps.find((beatmap) => beatmap.id === beatmapId);

    if (exact != null) {
      return exact;
    }
  }

  return beatmapset.beatmaps.find((beatmap) => !beatmap.convert) ?? beatmapset.beatmaps[0] ?? null;
}

function buildNowPlayingContext(
  beatmapset: BeatmapsetShowData,
  beatmap: BeatmapsetShowBeatmap,
): NowPlayingContext {
  const artist = beatmapset.artist_unicode || beatmapset.artist;
  const title = beatmapset.title_unicode || beatmapset.title;
  const beatmapsetUrl = `/beatmapsets/${beatmapset.id}#${beatmap.mode}/${beatmap.id}`;

  return {
    artist,
    beatmapId: beatmap.id,
    beatmapsetId: beatmapset.id,
    beatmapsetUrl,
    bpm: beatmap.bpm || beatmapset.bpm,
    coverUrl: beatmapset.covers.slimcover || beatmapset.covers.cover || beatmapset.covers.card,
    creator: beatmapset.user.username ?? beatmapset.creator,
    creatorId: beatmapset.user_id,
    difficultyRating: beatmap.difficulty_rating,
    favouriteCount: beatmapset.favourite_count,
    label: `${artist} - ${title} [${beatmap.version}]`,
    mode: beatmap.mode,
    playCount: beatmapset.play_count,
    source: beatmapset.source,
    status: beatmapset.status,
    title,
    totalLength: beatmap.total_length,
    url: `https://osu.novemo.dev/b/${beatmap.id}`,
    version: beatmap.version,
    video: beatmapset.video,
    storyboard: beatmapset.storyboard,
  };
}

function readChatUserBoolean(user: ChatUser, snakeName: string, camelName: string) {
  const snakeValue = user[snakeName];
  const camelValue = user[camelName];

  if (typeof snakeValue === "boolean") {
    return snakeValue;
  }

  return typeof camelValue === "boolean" ? camelValue : null;
}

function getProfileCommandMode(value?: string | null): ProfileCommandMode {
  return value === "taiko" || value === "fruits" || value === "mania" ? value : "osu";
}

function profileCommandUserFromChatUser(user: ChatUser): ProfileCommandUser {
  const playmode = typeof user.playmode === "string" ? getProfileCommandMode(user.playmode) : null;

  return {
    avatarUrl: user.avatar_url ?? user.avatarUrl ?? null,
    countryCode: user.country_code ?? user.countryCode ?? null,
    countryName: null,
    coverUrl: null,
    id: user.id,
    isOnline: readChatUserBoolean(user, "is_online", "isOnline"),
    isSupporter: readChatUserBoolean(user, "is_supporter", "isSupporter"),
    lastVisit: typeof user.last_visit === "string" ? user.last_visit : typeof user.lastVisit === "string" ? user.lastVisit : null,
    playmode,
    statistics: null,
    supportLevel: typeof user.support_level === "number" ? user.support_level : typeof user.supportLevel === "number" ? user.supportLevel : null,
    username: user.username,
  };
}

function profileCommandUserFromApiUser(user: ProfileUser, mode?: ProfileCommandMode | null): ProfileCommandUser {
  return {
    avatarUrl: user.avatar_url,
    countryCode: user.country_code,
    countryName: user.country?.name ?? null,
    coverUrl: user.cover.url ?? user.cover.custom_url ?? user.avatar_url,
    groups: user.groups.map((group) => ({
      colour: group.colour,
      id: group.id,
      name: group.name,
      shortName: group.short_name,
    })),
    id: user.id,
    isOnline: user.is_online,
    isSupporter: user.is_supporter,
    lastVisit: user.last_visit,
    playmode: mode ?? getProfileCommandMode(user.playmode),
    statistics: {
      accuracy: user.statistics.accuracy,
      globalRank: user.statistics.global_rank,
      playCount: user.statistics.play_count,
      pp: user.statistics.pp,
    },
    supportLevel: user.support_level ?? null,
    username: user.username,
  };
}

async function fetchProfileCommandUser(value: string, mode?: ProfileCommandMode | null, signal?: AbortSignal) {
  const searchParams = mode == null ? "" : `?mode=${encodeURIComponent(mode)}`;
  const response = await fetch(`/api/users/${encodeURIComponent(value)}${searchParams}`, {
    cache: "no-store",
    signal,
  });

  if (!response.ok) {
    throw new Error(response.status === 404 ? "user not found" : "could not load user");
  }

  return profileCommandUserFromApiUser((await response.json()) as ProfileUser, mode);
}

function resolveProfileCommandLookupValue(
  value: string,
  currentUser: ChatUser | null,
  usersById: Record<number, ChatUser>,
) {
  const trimmed = value.trim();

  if (/^\d+$/.test(trimmed)) {
    return trimmed;
  }

  const normalized = trimmed.replace(/^@/, "").toLowerCase();

  if (currentUser?.username.toLowerCase() === normalized) {
    return String(currentUser.id);
  }

  const matchingUser = Object.values(usersById).find((user) => user.username.toLowerCase() === normalized);

  return matchingUser == null ? trimmed : String(matchingUser.id);
}

function formatProfileInteger(value: number) {
  return new Intl.NumberFormat().format(Math.round(value));
}

const profilePercentageFormatter = new Intl.NumberFormat("en-US", {
  maximumFractionDigits: 2,
  minimumFractionDigits: 2,
  style: "percent",
});

function formatProfileAccuracy(value: number) {
  return profilePercentageFormatter.format(value > 1 ? value / 100 : value);
}

function getProfileDetails(value: ProfileCommandUser) {
  const details: string[] = [getProfileCommandMode(value.playmode)];
  const statistics = value.statistics;

  if (statistics != null) {
    if (statistics.globalRank != null) {
      details.push(`#${formatProfileInteger(statistics.globalRank)}`);
    }

    if (statistics.pp > 0) {
      details.push(`${formatProfileInteger(statistics.pp)}pp`);
    }

    if (statistics.accuracy > 0) {
      details.push(formatProfileAccuracy(statistics.accuracy));
    }
  }

  return details.join(" | ");
}

function getProfileCommandMessage(value: ProfileCommandUser) {
  const parts = [
    `profile: [https://osu.novemo.dev/u/${value.id} ${value.username}]`,
  ];
  const statistics = value.statistics;

  parts.push(getProfileCommandMode(value.playmode));

  if (statistics != null) {
    if (statistics.globalRank != null) {
      parts.push(`#${formatProfileInteger(statistics.globalRank)}`);
    }

    if (statistics.pp > 0) {
      parts.push(`${formatProfileInteger(statistics.pp)}pp`);
    }

    if (statistics.accuracy > 0) {
      parts.push(`${formatProfileAccuracy(statistics.accuracy)} acc`);
    }

    if (statistics.playCount > 0) {
      parts.push(`${formatProfileInteger(statistics.playCount)} plays`);
    }
  }

  return parts.join(" | ");
}

function getAvailableCommands(nowPlaying: NowPlayingContext | null, currentUser: ChatUser | null) {
  const commands: OverlayCommand[] = [];

  if (nowPlaying != null) {
    commands.push(nowPlayingCommand);
  }

  if (currentUser != null) {
    commands.push(profileCommand);
  }

  return commands;
}

function commandToMessage(command: ActiveCommand): ParsedOutgoingMessage {
  if (command.kind === "profile") {
    return {
      is_action: false,
      message: getProfileCommandMessage(command.value),
    };
  }

  return {
    is_action: true,
    message: `is viewing [${command.value.url} ${command.value.label}]`,
  };
}

function CommandMenu({
  activeIndex,
  commands,
  onSelect,
}: {
  activeIndex: number;
  commands: OverlayCommand[];
  onSelect: (command: OverlayCommand) => void;
}) {
  if (commands.length === 0) {
    return null;
  }

  return (
    <div className={commandSettingsMenuClassName}>
      {commands.map((command, index) => (
        <button
          key={command.name}
          className={cn(
            "flex w-full items-center justify-between gap-3 px-3 py-2 text-left transition-colors",
            index === activeIndex ? "bg-osu-b4 text-white" : "text-osu-c1 hover:bg-osu-b5",
          )}
          onMouseDown={(event) => {
            event.preventDefault();
            onSelect(command);
          }}
          type="button"
        >
          <span className="min-w-0">
            <span className="block font-semibold">{command.name}</span>
            <span className="block truncate text-xs text-osu-f1">{command.description}</span>
          </span>
          <span className="rounded-full bg-osu-h2 px-2 py-0.5 text-xs font-semibold text-white">{command.label}</span>
        </button>
      ))}
    </div>
  );
}

function ParameterMenu({
  activeIndex,
  command,
  query,
  onConfirmMode,
  onConfirmValue,
  onStartParameter,
}: {
  activeIndex: number;
  command: ActiveCommand;
  query: string;
  onConfirmMode: (mode: ProfileCommandMode) => void;
  onConfirmValue: () => void;
  onStartParameter: (parameter: ProfileCommandParameterName) => void;
}) {
  if (command.kind !== "profile") {
    return null;
  }

  if (command.activeParameter == null) {
    const selectedParameterIndex = Math.min(activeIndex, profileCommandParameters.length - 1);

    return (
      <div className={commandSettingsMenuClassName}>
        {profileCommandParameters.map((parameter, index) => {
          const selectedValue =
            parameter.name === "mode"
              ? command.parameters.mode ?? getProfileCommandMode(command.value.playmode)
              : command.parameters.user?.username ?? null;

          return (
          <button
            key={parameter.name}
            className={cn(
              "flex w-full items-center justify-between gap-3 px-3 py-2 text-left transition-colors",
              index === selectedParameterIndex ? "bg-osu-b4 text-white" : "text-osu-c1 hover:bg-osu-b4 hover:text-white",
            )}
            onMouseDown={(event) => {
              event.preventDefault();
              onStartParameter(parameter.name);
            }}
            type="button"
          >
            <span className="min-w-0">
              <span className="block font-semibold">{parameter.label}</span>
              <span className="block truncate text-xs text-osu-f1">
                {selectedValue == null ? parameter.description : `${parameter.description} | ${selectedValue}`}
              </span>
            </span>
            <span className="rounded-full bg-osu-b4 px-2 py-0.5 text-xs font-semibold text-white">
              {parameter.optional ? "optional" : "required"}
            </span>
          </button>
          );
        })}
      </div>
    );
  }

  if (command.activeParameter === "mode") {
    const normalizedQuery = query.trim().toLowerCase();
    const options = profileCommandModes.filter((option) => option.value.startsWith(normalizedQuery));
    const selectedOptionIndex = options.length === 0 ? 0 : Math.min(activeIndex, options.length - 1);
    const selectedMode = command.parameters.mode ?? getProfileCommandMode(command.value.playmode);

    return (
      <div className={commandSettingsMenuClassName}>
        {options.length === 0 ? (
          <div className="px-3 py-2 text-osu-f1">no matching mode</div>
        ) : options.map((option, index) => (
          <button
            key={option.value}
            className={cn(
              "flex w-full items-center justify-between gap-3 px-3 py-2 text-left transition-colors",
              index === selectedOptionIndex ? "bg-osu-b4 text-white" : "text-osu-c1 hover:bg-osu-b4 hover:text-white",
            )}
            onMouseDown={(event) => {
              event.preventDefault();
              onConfirmMode(option.value);
            }}
            type="button"
          >
            <span className="min-w-0">
              <span className="block font-semibold">{option.label}</span>
              <span className="block truncate text-xs text-osu-f1">{option.description}</span>
            </span>
            {option.value === selectedMode ? (
              <span className="rounded-full bg-osu-h2 px-2 py-0.5 text-xs font-semibold text-white">selected</span>
            ) : null}
          </button>
        ))}
      </div>
    );
  }

  const previewValue = command.previewValue ?? command.value;
  const loading = command.status === "loading";
  const error = command.status === "error";
  const label = query.trim().length > 0 ? "match" : "default";

  return (
    <div className={commandSettingsMenuClassName}>
      <button
        className={cn(
          "flex w-full items-center justify-between gap-3 bg-osu-b4 px-3 py-2 text-left text-white",
          (loading || error) && "cursor-default",
        )}
        disabled={loading || error}
        onMouseDown={(event) => {
          event.preventDefault();
          onConfirmValue();
        }}
        type="button"
      >
        <span className="min-w-0">
          <span className="block text-xs font-semibold uppercase text-osu-f1">{command.activeParameter}</span>
          <span className="block truncate">
            {loading ? "loading..." : previewValue.username}
          </span>
          <span className={cn("block truncate text-xs", error ? "text-osu-red-2" : "text-osu-f1")}>
            {error ? command.error : getProfileDetails(previewValue)}
          </span>
        </span>
        <span className={cn(
          "rounded-full px-2 py-0.5 text-xs font-semibold",
          error ? "bg-osu-red-3 text-white" : "bg-osu-h2 text-white",
        )}>
          {error ? "error" : label}
        </span>
      </button>
    </div>
  );
}

function ChatOverlayComposer({
  currentUser,
  disabled,
  maxLength,
  nowPlaying,
  onSubmit,
  sending,
  usersById,
}: {
  currentUser: ChatUser | null;
  disabled: boolean;
  maxLength?: number;
  nowPlaying: NowPlayingContext | null;
  onSubmit: (message: ParsedOutgoingMessage) => Promise<void>;
  sending: boolean;
  usersById: Record<number, ChatUser>;
}) {
  const [messageText, setMessageText] = useState("");
  const [activeCommand, setActiveCommand] = useState<ActiveCommand | null>(null);
  const [activeIndex, setActiveIndex] = useState(0);
  const [submitting, setSubmitting] = useState(false);
  const inputRef = useRef<HTMLTextAreaElement | null>(null);
  const availableCommands = useMemo(() => getAvailableCommands(nowPlaying, currentUser), [currentUser, nowPlaying]);
  const commandQuery = activeCommand == null && messageText.startsWith("/") && !messageText.includes(" ")
    ? messageText.toLowerCase()
    : null;
  const matchingCommands = useMemo(
    () => (
      commandQuery == null
        ? []
        : availableCommands.filter((command) => command.name.toLowerCase().startsWith(commandQuery))
    ),
    [availableCommands, commandQuery],
  );
  const showCommandMenu = commandQuery != null && matchingCommands.length > 0;
  const selectedCommandIndex = matchingCommands.length === 0 ? 0 : Math.min(activeIndex, matchingCommands.length - 1);
  const parsedMessage = activeCommand == null ? parseOutgoingMessage(messageText) : null;
  const activeCommandKind = activeCommand?.kind ?? null;
  const profileCommandActiveParameter = activeCommand?.kind === "profile" ? activeCommand.activeParameter : null;
  const profileModeQuery = activeCommand?.kind === "profile" && activeCommand.activeParameter === "mode"
    ? messageText.trim().toLowerCase()
    : null;
  const matchingProfileModes = useMemo(
    () => (
      profileModeQuery == null
        ? []
        : profileCommandModes.filter((option) => option.value.startsWith(profileModeQuery))
    ),
    [profileModeQuery],
  );
  const selectedProfileParameterIndex = Math.min(activeIndex, profileCommandParameters.length - 1);
  const selectedProfileModeIndex =
    matchingProfileModes.length === 0 ? 0 : Math.min(activeIndex, matchingProfileModes.length - 1);
  const shouldLoadProfileCommandDefault =
    activeCommand?.kind === "profile" && activeCommand.activeParameter == null && activeCommand.status === "loading";
  const profileCommandValueId = activeCommand?.kind === "profile" ? activeCommand.value.id : null;
  const profileCommandParameterUserId = activeCommand?.kind === "profile" ? activeCommand.parameters.user?.id ?? null : null;
  const profileCommandSelectedMode = activeCommand?.kind === "profile"
    ? activeCommand.parameters.mode ?? getProfileCommandMode(activeCommand.value.playmode)
    : null;
  const currentUserId = currentUser?.id ?? null;
  const commandReady = activeCommand == null
    ? false
    : activeCommand.kind === "profile"
      ? activeCommand.activeParameter == null && activeCommand.status === "ready"
      : activeCommand.activeParameter == null;
  const sendDisabled = disabled || sending || submitting || (activeCommand == null ? parsedMessage == null : !commandReady);

  const selectCommand = useCallback((command: OverlayCommand) => {
    if (command.kind === "np") {
      if (nowPlaying == null) {
        return;
      }

      setActiveCommand({
        activeParameter: null,
        kind: "np",
        parameters: {},
        value: nowPlaying,
      });
      setActiveIndex(0);
      setMessageText("");
      window.queueMicrotask(() => inputRef.current?.focus());
      return;
    }

    if (command.kind === "profile" && currentUser != null) {
      setActiveCommand({
        activeParameter: null,
        kind: "profile",
        parameters: {},
        status: "loading",
        value: profileCommandUserFromChatUser(currentUser),
      });
      setActiveIndex(0);
      setMessageText("");
      window.queueMicrotask(() => inputRef.current?.focus());
    }
  }, [currentUser, nowPlaying]);

  const startCommandParameter = useCallback((parameter: ProfileCommandParameterName) => {
    setActiveCommand((current) => (
      current?.kind === "profile"
        ? {
            ...current,
            activeParameter: parameter,
            error: undefined,
            previewValue: parameter === "user" ? current.value : current.previewValue,
            status: parameter === "user" && current.status !== "ready" ? "loading" : current.status,
          }
        : current
    ));
    setActiveIndex(0);
    setMessageText("");
    window.queueMicrotask(() => inputRef.current?.focus());
  }, []);

  const cancelCommandParameter = useCallback(() => {
    setActiveCommand((current) => (
      current?.kind === "profile" && current.activeParameter != null
        ? {
            ...current,
            activeParameter: null,
            error: undefined,
            previewValue: undefined,
            status: "ready",
          }
        : current
    ));
    setActiveIndex(0);
    setMessageText("");
    window.queueMicrotask(() => inputRef.current?.focus());
  }, []);

  const confirmActiveParameter = useCallback((override?: {
    mode?: ProfileCommandMode;
  }) => {
    if (activeCommand?.kind !== "profile" || activeCommand.activeParameter == null) {
      return;
    }

    if (
      activeCommand.activeParameter === "mode" &&
      override?.mode == null &&
      messageText.trim().length > 0 &&
      !profileCommandModes.some((option) => option.value.startsWith(messageText.trim().toLowerCase()))
    ) {
      return;
    }

    setActiveCommand((current) => {
      if (current?.kind !== "profile" || current.activeParameter == null) {
        return current;
      }

      if (current.activeParameter === "user") {
        if (current.status !== "ready") {
          return current;
        }

        const user = current.previewValue ?? current.value;

        return {
          ...current,
          activeParameter: null,
          error: undefined,
          parameters: {
            ...current.parameters,
            user,
          },
          previewValue: undefined,
          status: "ready",
          value: user,
        };
      }

      if (current.activeParameter === "mode") {
        const query = messageText.trim().toLowerCase();
        const matchingMode = profileCommandModes.find((option) => option.value.startsWith(query))?.value;

        if (override?.mode == null && query.length > 0 && matchingMode == null) {
          return current;
        }

        const mode = override?.mode
          ?? matchingMode
          ?? current.parameters.mode
          ?? getProfileCommandMode(current.value.playmode);

        return {
          ...current,
          activeParameter: null,
          error: undefined,
          parameters: {
            ...current.parameters,
            mode,
          },
          previewValue: undefined,
          status: "loading",
        };
      }

      return current;
    });
    setActiveIndex(0);
    setMessageText("");
    window.queueMicrotask(() => inputRef.current?.focus());
  }, [activeCommand, messageText]);

  useEffect(() => {
    if (activeCommand?.kind === "np" && nowPlaying != null && activeCommand.value.beatmapId !== nowPlaying.beatmapId) {
      window.queueMicrotask(() => {
        setActiveCommand(null);
        setMessageText("");
      });
    }
  }, [activeCommand, nowPlaying]);

  useEffect(() => {
    if (activeCommandKind !== "profile") {
      return;
    }

    const fallbackUserId = currentUserId ?? profileCommandParameterUserId ?? profileCommandValueId;

    if (fallbackUserId == null) {
      return;
    }

    const query = messageText.trim();
    const lookupSource = profileCommandActiveParameter === "user"
      ? (query.length > 0 ? query : String(fallbackUserId))
      : shouldLoadProfileCommandDefault
        ? String(fallbackUserId)
        : null;

    if (lookupSource == null) {
      return;
    }

    const activeParameter = profileCommandActiveParameter;
    const lookupValue = resolveProfileCommandLookupValue(lookupSource, currentUser, usersById);
    const controller = new AbortController();
    const timeoutId = window.setTimeout(() => {
      setActiveCommand((current) => (
        current?.kind === "profile" && current.activeParameter === activeParameter
          ? current.status === "loading"
            ? current
            : {
                ...current,
                error: undefined,
                status: "loading",
              }
          : current
      ));

      fetchProfileCommandUser(lookupValue, profileCommandSelectedMode, controller.signal)
        .then((user) => {
          setActiveCommand((current) => (
            current?.kind === "profile" && current.activeParameter === activeParameter
              ? activeParameter === "user"
                ? {
                    ...current,
                    error: undefined,
                    previewValue: user,
                    status: "ready",
                  }
                : {
                    ...current,
                    error: undefined,
                    previewValue: undefined,
                    status: "ready",
                    value: user,
                  }
              : current
          ));
        })
        .catch((error) => {
          if (controller.signal.aborted) {
            return;
          }

          setActiveCommand((current) => (
            current?.kind === "profile" && current.activeParameter === activeParameter
              ? {
                  ...current,
                  error: error instanceof Error ? error.message : "could not load user",
                  status: "error",
                }
              : current
          ));
        });
    }, query.length === 0 ? 0 : 300);

    return () => {
      controller.abort();
      window.clearTimeout(timeoutId);
    };
  }, [
    activeCommandKind,
    currentUser,
    currentUserId,
    messageText,
    profileCommandActiveParameter,
    profileCommandParameterUserId,
    profileCommandSelectedMode,
    profileCommandValueId,
    shouldLoadProfileCommandDefault,
    usersById,
  ]);

  const handleKeyDown = (event: KeyboardEvent<HTMLTextAreaElement>) => {
    if (activeCommand != null) {
      if (activeCommand.kind === "np" && activeCommand.activeParameter == null) {
        if (event.key === "ArrowDown" || event.key === "ArrowUp") {
          event.preventDefault();
          setActiveIndex(0);
          return;
        }
      }

      if (activeCommand.kind === "profile" && activeCommand.activeParameter == null) {
        if (event.key === "ArrowDown") {
          event.preventDefault();
          setActiveIndex((current) => (current + 1) % profileCommandParameters.length);
          return;
        }

        if (event.key === "ArrowUp") {
          event.preventDefault();
          setActiveIndex((current) => (current - 1 + profileCommandParameters.length) % profileCommandParameters.length);
          return;
        }
      }

      if (activeCommand.kind === "profile" && activeCommand.activeParameter === "mode" && matchingProfileModes.length > 0) {
        if (event.key === "ArrowDown") {
          event.preventDefault();
          setActiveIndex((current) => (current + 1) % matchingProfileModes.length);
          return;
        }

        if (event.key === "ArrowUp") {
          event.preventDefault();
          setActiveIndex((current) => (current - 1 + matchingProfileModes.length) % matchingProfileModes.length);
          return;
        }
      }

      if (event.key === "Tab") {
        event.preventDefault();
        if (activeCommand.kind === "np") {
          return;
        } else if (activeCommand.kind === "profile") {
          if (activeCommand.activeParameter == null) {
            startCommandParameter(profileCommandParameters[selectedProfileParameterIndex]?.name ?? "user");
          } else if (activeCommand.activeParameter === "mode") {
            confirmActiveParameter({ mode: matchingProfileModes[selectedProfileModeIndex]?.value });
          } else {
            confirmActiveParameter();
          }
        }
        return;
      }

      if (event.key === "Backspace" && messageText.length === 0) {
        event.preventDefault();
        if (activeCommand.activeParameter != null) {
          cancelCommandParameter();
        } else {
          setActiveCommand(null);
        }
        return;
      }
    }

    if (showCommandMenu) {
      if (event.key === "ArrowDown") {
        event.preventDefault();
        setActiveIndex((current) => (current + 1) % matchingCommands.length);
        return;
      }

      if (event.key === "ArrowUp") {
        event.preventDefault();
        setActiveIndex((current) => (current - 1 + matchingCommands.length) % matchingCommands.length);
        return;
      }

      if (event.key === "Tab") {
        event.preventDefault();
        selectCommand(matchingCommands[selectedCommandIndex] ?? matchingCommands[0]);
        return;
      }
    }

    if (event.key !== "Enter" || event.shiftKey) {
      return;
    }

    event.preventDefault();
    event.currentTarget.form?.requestSubmit();
  };

  const handleSubmit = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();

    const outgoingMessage = activeCommand == null
      ? parsedMessage
      : commandReady
        ? commandToMessage(activeCommand)
        : null;

    if (outgoingMessage == null || sendDisabled) {
      return;
    }

    setSubmitting(true);

    try {
      await onSubmit(outgoingMessage);
      setMessageText("");
      setActiveCommand(null);
    } finally {
      setSubmitting(false);
    }
  };
  const activeCommandName = activeCommand?.kind === "profile" ? "Profile" : "Now playing";
  const activeCommandValueLabel = activeCommand == null
    ? null
    : activeCommand.kind === "profile"
      ? activeCommand.activeParameter == null && activeCommand.status === "ready"
        ? activeCommand.parameters.user?.username ?? null
        : null
      : null;
  const activeCommandModeLabel =
    activeCommand?.kind === "profile" && activeCommand.activeParameter == null && activeCommand.status === "ready"
      ? activeCommand.parameters.mode ?? null
      : null;
  const commandParameterActive = activeCommand != null && activeCommand.activeParameter != null;
  const commandParameterPlaceholder =
    activeCommand?.kind === "profile" && activeCommand.activeParameter === "mode"
        ? "osu, taiko, fruits or mania"
        : "id or username";

  return (
    <ChatComposerShell
      buttonVariant="icon"
      disabled={disabled || sending || submitting}
      leadingContent={(
        <>
          {activeCommand == null ? null : (
            <span className="shrink-0 rounded-full bg-osu-h2 px-2 py-0.5 text-xs font-semibold text-white">
              {activeCommandName}
            </span>
          )}
          {activeCommandValueLabel != null ? (
            <span className="min-w-0 max-w-[120px] shrink-0 truncate rounded-full bg-osu-b4 px-2 py-0.5 text-xs font-semibold text-white">
              {activeCommandValueLabel}
            </span>
          ) : null}
          {activeCommandModeLabel != null ? (
            <span className="min-w-0 shrink-0 truncate rounded-full bg-osu-b4 px-2 py-0.5 text-xs font-semibold text-white">
              {activeCommandModeLabel}
            </span>
          ) : null}
        </>
      )}
      maxLength={maxLength}
      menuContent={(
        <>
          <CommandMenu
            activeIndex={selectedCommandIndex}
            commands={matchingCommands}
            onSelect={selectCommand}
          />
          {activeCommand == null ? null : (
            <ParameterMenu
              activeIndex={activeIndex}
              command={activeCommand}
              onConfirmMode={(mode) => confirmActiveParameter({ mode })}
              onConfirmValue={confirmActiveParameter}
              onStartParameter={startCommandParameter}
              query={messageText}
            />
          )}
        </>
      )}
      onChange={setMessageText}
      onKeyDown={handleKeyDown}
      onSubmit={handleSubmit}
      placeholder={activeCommand == null ? "type your message" : commandParameterActive ? commandParameterPlaceholder : ""}
      readOnly={activeCommand != null && !commandParameterActive}
      resizeSignal={activeCommand}
      sendDisabled={sendDisabled}
      sending={sending || submitting}
      textareaRef={inputRef}
      value={messageText}
    />
  );
}

function ChatOverlayChannelPicker({
  channels,
  error,
  loading,
  onClose,
  onOpenChannel,
  openChannelIds,
  usersById,
}: {
  channels: ChatChannel[];
  error: string | null;
  loading: boolean;
  onClose: () => void;
  onOpenChannel: (channelId: number) => void;
  openChannelIds: number[];
  usersById: Record<number, ChatUser>;
}) {
  const [channelSearch, setChannelSearch] = useState("");
  const filteredChannels = useMemo(
    () => channels.filter((channel) => channelMatchesSearch(channel, channelSearch, usersById)),
    [channelSearch, channels, usersById],
  );
  const channelsByGroup = useMemo(() => groupChannels(filteredChannels), [filteredChannels]);
  const openChannelIdSet = useMemo(() => new Set(openChannelIds), [openChannelIds]);

  return (
    <section className="flex h-[520px] w-[300px] max-w-[calc(100vw-2rem)] shrink-0 flex-col overflow-hidden rounded-md bg-osu-b5 text-osu-c1 shadow-2xl shadow-black/40">
      <header className="flex h-11 shrink-0 items-center gap-2 bg-osu-d5 px-3">
        <MessageCircle className="h-4 w-4 text-osu-l2" />
        <Link className="min-w-0 flex-1 text-sm font-semibold text-white transition-colors hover:text-osu-l1" href="/community/chat">
          chat
        </Link>
        <button
          className="flex h-8 w-8 items-center justify-center rounded-full text-osu-c1 transition-colors hover:bg-white/10"
          onClick={onClose}
          type="button"
        >
          <Minus className="h-4 w-4" />
          <span className="sr-only">hide chat list</span>
        </button>
      </header>

      <div className="sticky top-0 z-20 bg-osu-b5 px-2.5 py-2">
        <label className="flex h-9 items-center gap-2 rounded-md bg-osu-b6 px-3 text-osu-f1 shadow-[inset_0_0_0_1px_rgba(255,255,255,0.06)] focus-within:shadow-[inset_0_0_0_1px_hsl(var(--hsl-h1))]">
          <span className="sr-only">search chats</span>
          <Search className="h-4 w-4 shrink-0" />
          <input
            className="min-w-0 flex-1 bg-transparent text-sm text-white caret-osu-h1 outline-none placeholder:text-osu-f1"
            disabled={loading}
            onChange={(event) => setChannelSearch(event.target.value)}
            placeholder="search"
            type="search"
            value={channelSearch}
          />
        </label>
      </div>

      <div className="min-h-0 flex-1 overflow-y-auto pb-2.5">
        {error == null ? null : (
          <div className="mx-3 mb-2 rounded-md bg-osu-red-3/20 px-3 py-2 text-xs text-osu-red-2">{error}</div>
        )}

        {loading ? (
          <div className="px-4 py-8 text-center text-sm text-osu-f1">loading...</div>
        ) : channels.length === 0 ? (
          <div className="px-4 py-8 text-center text-sm text-osu-f1">no channels</div>
        ) : filteredChannels.length === 0 ? (
          <div className="px-4 py-8 text-center text-sm text-osu-f1">no matching channels</div>
        ) : (
          channelTypeOrder.map((type) => {
            const groupedChannels = channelsByGroup[type] ?? [];

            if (groupedChannels.length === 0) {
              return null;
            }

            return (
              <div key={type} className="mb-1">
                <div className="sticky top-0 z-10 flex gap-[5px] bg-osu-b5 px-[30px] py-2.5 text-sm text-osu-c1">
                  <span className="uppercase">{channelTypeLabels[type] ?? type.toLowerCase()}</span>
                </div>

                {groupedChannels.map((channel) => {
                  const id = getChannelId(channel);
                  const active = id != null && openChannelIdSet.has(id);

                  return (
                    <div
                      key={`${channel.type}-${id}`}
                      className={cn(
                        "group/channel relative ml-2.5 flex h-10 w-[calc(100%-10px)] min-w-0 items-center pl-2.5 text-white transition-colors",
                        active
                          ? "bg-[linear-gradient(to_right,hsl(var(--hsl-b5))_10px,hsl(var(--hsl-b4))_33%,hsl(var(--hsl-b4))_100%)]"
                          : "hover:bg-[linear-gradient(to_right,hsl(var(--hsl-b5))_10px,hsl(var(--hsl-b3))_33%,hsl(var(--hsl-b3))_100%)]",
                      )}
                    >
                      <button
                        aria-pressed={active}
                        className="ml-2.5 flex h-full min-w-0 flex-1 items-center bg-transparent text-left"
                        disabled={id == null}
                        onClick={() => {
                          if (id != null) {
                            onOpenChannel(id);
                          }
                        }}
                        type="button"
                      >
                        <span className="mr-2.5 flex h-[30px] w-[30px] shrink-0 items-center justify-center text-white/80 [&>span]:h-[30px] [&>span]:w-[30px]">
                          {getChannelIcon(channel)}
                        </span>
                        <span className="min-w-0 flex-1 truncate text-[15px] text-white">
                          {getChannelName(channel)}
                        </span>
                      </button>
                      <ChevronRight
                        aria-hidden
                        className={cn(
                          "mr-2.5 h-4 w-4 shrink-0 text-white transition-opacity",
                          active ? "opacity-100" : "opacity-0",
                        )}
                      />
                    </div>
                  );
                })}
              </div>
            );
          })
        )}
      </div>
    </section>
  );
}

function ChatOverlayWindow({
  channel,
  currentUser,
  error,
  loading,
  messages,
  nowPlaying,
  onClose,
  onSend,
  sending,
  usersById,
}: {
  channel: ChatChannel;
  currentUser: ChatUser | null;
  error: string | null;
  loading: boolean;
  messages: ChatMessage[];
  nowPlaying: NowPlayingContext | null;
  onClose: (channelId: number) => void;
  onSend: (channelId: number, message: ParsedOutgoingMessage) => Promise<void>;
  sending: boolean;
  usersById: Record<number, ChatUser>;
}) {
  const channelId = getChannelId(channel);
  const currentUserId = currentUser?.id ?? null;

  if (channelId == null) {
    return null;
  }

  return (
    <section className="flex h-[520px] w-[340px] max-w-[calc(100vw-2rem)] shrink-0 flex-col overflow-hidden rounded-md bg-osu-b5 text-osu-c1 shadow-2xl shadow-black/40">
      <header className="flex h-11 shrink-0 items-center gap-2 bg-osu-d5 px-3">
        <span className="flex h-[30px] w-[30px] shrink-0 items-center justify-center text-white/80 [&>span]:h-[30px] [&>span]:w-[30px]">
          {getChannelIcon(channel)}
        </span>
        <div className="min-w-0 flex-1 truncate text-sm font-semibold text-white">{getChannelName(channel)}</div>
        <button
          className="flex h-8 w-8 items-center justify-center rounded-full text-osu-c1 transition-colors hover:bg-white/10"
          onClick={() => onClose(channelId)}
          type="button"
        >
          <X className="h-4 w-4" />
          <span className="sr-only">close chat</span>
        </button>
      </header>

      <div className="min-h-0 flex-1">
        <ChatConversationFrame
          canMessage
          channel={channel}
          composer={(
            <ChatOverlayComposer
              currentUser={currentUser}
              disabled={channelId == null}
              maxLength={getChannelMessageLengthLimit(channel)}
              nowPlaying={nowPlaying}
              onSubmit={(message) => onSend(channelId, message)}
              sending={sending}
              usersById={usersById}
            />
          )}
          currentUser={currentUser}
          currentUserId={currentUserId}
          error={error}
          loading={loading}
          messages={messages}
          usersById={usersById}
          variant="overlay"
        />
      </div>
    </section>
  );
}

export function ChatOverlay() {
  const pathname = usePathname();
  const { data: session, status } = useSession();
  const { lastEvent } = useRealtime();
  const [channelPickerOpen, setChannelPickerOpen] = useState(true);
  const [openChannelIds, setOpenChannelIds] = useState<number[]>([]);
  const [channels, setChannels] = useState<ChatChannel[]>([]);
  const [messagesByChannel, setMessagesByChannel] = useState<Record<number, ChatMessage[]>>({});
  const [usersById, setUsersById] = useState<Record<number, ChatUser>>({});
  const [loading, setLoading] = useState(false);
  const [loadingMessagesByChannel, setLoadingMessagesByChannel] = useState<Record<number, boolean>>({});
  const [sendingByChannel, setSendingByChannel] = useState<Record<number, boolean>>({});
  const [error, setError] = useState<string | null>(null);
  const [hash, setHash] = useState("");
  const loadedChannelIdsRef = useRef(new Set<number>());
  const lastHandledEventRef = useRef<typeof lastEvent>(null);
  const openChannelIdsRef = useRef<number[]>([]);
  const beatmapsetId = getBeatmapsetIdFromPathname(pathname);
  const beatmapsetHref = beatmapsetId == null ? null : `/api/beatmapsets/${beatmapsetId}`;
  const { data: nowPlayingBeatmapset = null } = useSWR<BeatmapsetShowData>(beatmapsetHref);
  const currentUser = useMemo(() => chatUserFromSessionUser(session?.user), [session?.user]);
  const sortedChannels = useMemo(() => sortChannelsByActivity(channels, messagesByChannel), [channels, messagesByChannel]);
  const openChannels = useMemo(
    () => openChannelIds
      .map((channelId) => sortedChannels.find((channel) => getChannelId(channel) === channelId) ?? null)
      .filter((channel): channel is ChatChannel => channel != null),
    [openChannelIds, sortedChannels],
  );
  const nowPlaying = useMemo(() => {
    if (beatmapsetId == null || nowPlayingBeatmapset == null) {
      return null;
    }

    const { beatmapId } = parseBeatmapsetPageHash(hash);
    const beatmap = getDefaultBeatmap(nowPlayingBeatmapset, beatmapId);

    return beatmap == null ? null : buildNowPlayingContext(nowPlayingBeatmapset, beatmap);
  }, [beatmapsetId, hash, nowPlayingBeatmapset]);

  useEffect(() => {
    openChannelIdsRef.current = openChannelIds;
  }, [openChannelIds]);

  const pruneOverlayRecords = useCallback((nextOpenChannelIds: number[]) => {
    openChannelIdsRef.current = nextOpenChannelIds;
    const retainedChannelIds = new Set(nextOpenChannelIds);

    for (const channelId of loadedChannelIdsRef.current) {
      if (!retainedChannelIds.has(channelId)) {
        loadedChannelIdsRef.current.delete(channelId);
      }
    }

    setMessagesByChannel((current) => pruneChannelRecord(current, retainedChannelIds));
    setLoadingMessagesByChannel((current) => pruneChannelRecord(current, retainedChannelIds));
    setSendingByChannel((current) => pruneChannelRecord(current, retainedChannelIds));
    const retainedUserIds = getRetainedUserIds(messagesByChannel, retainedChannelIds, openChannels);
    setUsersById((current) => pruneUserRecord(current, retainedUserIds));
  }, [messagesByChannel, openChannels]);

  useEffect(() => {
    const updateHash = () => setHash(window.location.hash);
    updateHash();

    const interval = window.setInterval(updateHash, 500);
    window.addEventListener("hashchange", updateHash);
    window.addEventListener("popstate", updateHash);

    return () => {
      window.clearInterval(interval);
      window.removeEventListener("hashchange", updateHash);
      window.removeEventListener("popstate", updateHash);
    };
  }, [pathname]);

  const loadChannels = useCallback(async () => {
    setLoading(true);
    setError(null);

    try {
      const [updates, publicChannels] = await Promise.all([
        chatRequest<ChatUpdatesResponse>("updates?since=0&includes[]=presence"),
        chatRequest<ChatChannel[]>("channels"),
      ]);
      const mergedChannels = sortChannelsByActivity(mergeChannels(publicChannels, updates.presence ?? []), {});

      setChannels(mergedChannels);
      setOpenChannelIds((current) => (
        current.filter((channelId) => mergedChannels.some((channel) => getChannelId(channel) === channelId))
      ));
    } catch (loadError) {
      setError(loadError instanceof Error ? loadError.message : "Could not load chat.");
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    if (status !== "authenticated") {
      return;
    }

    const timeout = window.setTimeout(() => {
      void loadChannels();
    }, 0);

    return () => {
      window.clearTimeout(timeout);
    };
  }, [loadChannels, status]);

  const loadMessagesForChannel = useCallback(async (channelId: number) => {
    if (status !== "authenticated" || loadedChannelIdsRef.current.has(channelId)) {
      return;
    }

    loadedChannelIdsRef.current.add(channelId);
    setLoadingMessagesByChannel((current) => ({
      ...current,
      [channelId]: true,
    }));
    setError(null);

    try {
      const response = await chatRequest<ChatMessage[] | { messages?: ChatMessage[]; users?: ChatUser[] }>(`channels/${channelId}/messages?return_object=1`);
      const { messages, users } = normalizeChatMessagesResponse(response);

      if (!openChannelIdsRef.current.includes(channelId)) {
        return;
      }

      setUsersById((current) => mergeUsersById(current, getRelevantMessageUsers(messages, users)));
      setMessagesByChannel((current) => ({
        ...current,
        [channelId]: appendMessages(current[channelId] ?? [], messages),
      }));
    } catch (loadError) {
      loadedChannelIdsRef.current.delete(channelId);
      setError(loadError instanceof Error ? loadError.message : "Could not load messages.");
    } finally {
      setLoadingMessagesByChannel((current) => {
        const next = { ...current };

        if (openChannelIdsRef.current.includes(channelId)) {
          next[channelId] = false;
        } else {
          delete next[channelId];
        }

        return next;
      });
    }
  }, [status]);

  useEffect(() => {
    if (status !== "authenticated") {
      return;
    }

    const timeout = window.setTimeout(() => {
      for (const channelId of openChannelIds) {
        void loadMessagesForChannel(channelId);
      }
    }, 0);

    return () => {
      window.clearTimeout(timeout);
    };
  }, [loadMessagesForChannel, openChannelIds, status]);

  useEffect(() => {
    if (lastEvent == null || lastEvent === lastHandledEventRef.current) {
      return;
    }

    lastHandledEventRef.current = lastEvent;

    let active = true;
    const eventToHandle = lastEvent;

    window.queueMicrotask(() => {
      if (!active) {
        return;
      }

      if (eventToHandle.event === "chat.channel.join" && typeof eventToHandle.data === "object" && eventToHandle.data != null) {
        setChannels((current) => mergeChannels(current, [eventToHandle.data as ChatChannel]));
        return;
      }

      if (eventToHandle.event !== "chat.message.new" || typeof eventToHandle.data !== "object" || eventToHandle.data == null) {
        return;
      }

      const data = eventToHandle.data as ChatMessagesNewEventData;
      const openChannelIds = new Set(openChannelIdsRef.current);
      const messages = (data.messages ?? []).filter((message) => {
        const channelId = getMessageChannelId(message);

        return channelId != null && openChannelIds.has(channelId);
      });

      if (messages.length === 0) {
        return;
      }

      setUsersById((current) => mergeUsersById(current, getRelevantMessageUsers(messages, data.users ?? [])));
      setMessagesByChannel((current) => {
        const next = { ...current };

        for (const message of messages) {
          const channelId = getMessageChannelId(message);

          if (channelId != null) {
            next[channelId] = appendMessages(next[channelId] ?? [], [message]);
          }
        }

        return next;
      });
    });

    return () => {
      active = false;
    };
  }, [lastEvent]);

  const handleOpenChannel = useCallback((channelId: number) => {
    setChannelPickerOpen(true);

    if (openChannelIds.includes(channelId)) {
      setError(null);
      return;
    }

    setError(null);
    const nextOpenChannelIds = openChannelIds.length < maxOpenOverlayChats
      ? [...openChannelIds, channelId]
      : [...openChannelIds.slice(1), channelId];

    setOpenChannelIds(nextOpenChannelIds);
    pruneOverlayRecords(nextOpenChannelIds);
  }, [openChannelIds, pruneOverlayRecords]);

  const handleCloseChannel = useCallback((channelId: number) => {
    const nextOpenChannelIds = openChannelIds.filter((id) => id !== channelId);

    setOpenChannelIds(nextOpenChannelIds);
    pruneOverlayRecords(nextOpenChannelIds);
  }, [openChannelIds, pruneOverlayRecords]);

  const handleSend = async (channelId: number, message: ParsedOutgoingMessage) => {
    setSendingByChannel((current) => ({
      ...current,
      [channelId]: true,
    }));
    setError(null);

    try {
      const sentMessage = await chatRequest<ChatMessage>(`channels/${channelId}/messages`, {
        body: JSON.stringify({
          ...message,
          uuid: createUuid(),
        }),
        method: "POST",
      });
      const messageWithSender = {
        ...sentMessage,
        sender: sentMessage.sender ?? currentUser ?? undefined,
      };

      if (!openChannelIdsRef.current.includes(channelId)) {
        return;
      }

      setUsersById((current) => mergeUsersById(current, getMessageUsers([messageWithSender])));
      setMessagesByChannel((current) => ({
        ...current,
        [channelId]: appendMessages(current[channelId] ?? [], [messageWithSender]),
      }));
    } catch (sendError) {
      setError(sendError instanceof Error ? sendError.message : "Could not send message.");
      throw sendError;
    } finally {
      setSendingByChannel((current) => {
        const next = { ...current };

        if (openChannelIdsRef.current.includes(channelId)) {
          next[channelId] = false;
        } else {
          delete next[channelId];
        }

        return next;
      });
    }
  };

  if (status !== "authenticated") {
    return null;
  }

  return (
    <div className="fixed right-4 bottom-4 z-[120] flex max-w-[calc(100vw-2rem)] items-end justify-end gap-3 overflow-x-auto print:hidden">
      {/* Minimising the main window hides the open conversations with it, rather than leaving a
          row of bubbles floating with nothing to anchor them. They are only hidden, not closed,
          so reopening restores exactly what was there — the count on the button reflects that. */}
      {(channelPickerOpen ? openChannels : []).map((channel) => {
        const channelId = getChannelId(channel);

        if (channelId == null) {
          return null;
        }

        return (
          <ChatOverlayWindow
            key={channelId}
            channel={channel}
            currentUser={currentUser}
            error={error}
            loading={loadingMessagesByChannel[channelId] === true}
            messages={messagesByChannel[channelId] ?? []}
            nowPlaying={nowPlaying}
            onClose={handleCloseChannel}
            onSend={handleSend}
            sending={sendingByChannel[channelId] === true}
            usersById={usersById}
          />
        );
      })}

      {channelPickerOpen ? (
        <ChatOverlayChannelPicker
          channels={sortedChannels}
          error={openChannels.length === 0 ? error : null}
          loading={loading}
          onClose={() => setChannelPickerOpen(false)}
          onOpenChannel={handleOpenChannel}
          openChannelIds={openChannelIds}
          usersById={usersById}
        />
      ) : (
        <button
          className="flex h-11 shrink-0 items-center gap-2 rounded-full bg-osu-h2 px-4 text-sm font-semibold text-white shadow-[0_12px_32px_rgba(0,0,0,0.35)] transition-colors hover:bg-osu-h1"
          onClick={() => setChannelPickerOpen(true)}
          type="button"
        >
          <MessageCircle className="h-4 w-4" />
          chat
          {openChannelIds.length > 0 ? (
            <span className="rounded-full bg-white/20 px-1.5 py-0.5 text-xs">{openChannelIds.length}</span>
          ) : null}
        </button>
      )}
    </div>
  );
}

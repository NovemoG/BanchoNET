"use client";

import {
  type FormEvent,
  type KeyboardEvent,
  type MutableRefObject,
  type ReactNode,
  useCallback,
  useEffect,
  useMemo,
  useRef,
  useState,
} from "react";
import Link from "next/link";
import { Loader2, Send } from "lucide-react";
import useSWR from "swr";
import {
  ChatBeatmapCard,
  parseChatBeatmapCardPayload,
  type ChatBeatmapCardBeatmap,
  type ChatBeatmapCardPayload,
} from "@/components/chat/beatmap-card-message";
import { renderChatTextWithLinks, type ChatMentionUser } from "@/components/chat/message-text";
import {
  ChatProfileCard,
  parseChatProfileCardPayload,
  type ChatProfileCardPayload,
  type ChatProfileCardUser,
} from "@/components/chat/profile-card-message";
import type { BeatmapsetShowBeatmap, BeatmapsetShowData } from "@/lib/beatmapset-types";
import type { ChatChannel, ChatMessage, ChatUser } from "@/lib/chat/types";
import type { ProfileUser } from "@/lib/profile";
import type { Ruleset } from "@/lib/rankings";
import { cn } from "@/lib/utils";

export type ParsedOutgoingMessage = {
  is_action: boolean;
  message: string;
};

type ChatChannelAttributes = {
  can_message?: boolean;
  can_message_error?: string | null;
  canMessage?: boolean;
  canMessageError?: string | null;
  last_read_id?: number | null;
  lastReadId?: number | null;
};

type ConversationEntry =
  | {
      timestamp: string;
      type: "day" | "unread";
    }
  | {
      messages: ChatMessage[];
      type: "group";
    };

type ChatMessageCardPayload =
  | {
      payload: ChatBeatmapCardPayload;
      type: "beatmap";
    }
  | {
      payload: ChatProfileCardPayload;
      type: "profile";
    };

type ChatMessageCardReference =
  | {
      beatmapId: number;
      type: "beatmap-reference";
    }
  | {
      mode: Ruleset | null;
      type: "profile-reference";
      userId: number;
    };

type ChatMessageCardContent = ChatMessageCardPayload | ChatMessageCardReference;

type BeatmapLookupResponse = {
  beatmap: BeatmapsetShowBeatmap;
  beatmapset: BeatmapsetShowData;
};

export const chatPageActionButtonClassName =
  "inline-flex min-w-20 items-center justify-center gap-2 self-end rounded-full border-0 bg-osu-h2 px-2.5 py-2.5 text-sm font-semibold text-osu-c1 transition-colors hover:bg-osu-h1 disabled:cursor-default disabled:bg-osu-b3 disabled:text-osu-f1 disabled:hover:bg-osu-b3 md:min-w-[130px] md:px-5";

export const chatOverlayActionButtonClassName =
  "inline-flex h-10 w-10 shrink-0 items-center justify-center self-end rounded-full border-0 bg-osu-h2 p-0 text-sm font-semibold text-osu-c1 transition-colors hover:bg-osu-h1 disabled:cursor-default disabled:bg-osu-b3 disabled:text-osu-f1 disabled:hover:bg-osu-b3";

export function parseOutgoingMessage(value: string): ParsedOutgoingMessage | null {
  const trimmed = value.trim();

  if (trimmed.length === 0) {
    return null;
  }

  if (trimmed.startsWith("/me ")) {
    const action = trimmed.slice(4).trim();

    return action.length > 0
      ? {
          is_action: true,
          message: action,
        }
      : null;
  }

  return {
    is_action: false,
    message: trimmed,
  };
}

export function getMessageChannelId(message: ChatMessage) {
  const id = message.channel_id ?? message.channelId;

  return typeof id === "number" ? id : null;
}

export function getMessageNumericId(message: ChatMessage) {
  const id = message.message_id ?? message.messageId;

  return typeof id === "number" ? id : 0;
}

export function getMessageSenderId(message: ChatMessage) {
  const id = message.sender_id ?? message.senderId;

  return typeof id === "number" ? id : null;
}

export function getMessageId(message: ChatMessage) {
  return `${getMessageChannelId(message) ?? "unknown"}:${getMessageNumericId(message)}:${message.uuid ?? ""}`;
}

export function getChannelMessageLengthLimit(channel: ChatChannel | null) {
  return channel?.message_length_limit ?? channel?.messageLengthLimit ?? 450;
}

function getMessageSender(
  message: ChatMessage,
  usersById: Record<number, ChatUser>,
  currentUser: ChatUser | null,
) {
  const senderId = getMessageSenderId(message);

  if (message.sender != null) {
    return message.sender;
  }

  if (senderId == null) {
    return null;
  }

  return usersById[senderId] ?? (currentUser?.id === senderId ? currentUser : null);
}

function getMessageAction(message: ChatMessage) {
  return message.is_action ?? message.isAction ?? false;
}

function getMessageType(message: ChatMessage) {
  if (getMessageAction(message)) {
    return "action";
  }

  return message.type ?? "plain";
}

function getMessageProfileCardPayload(message: ChatMessage) {
  return getMessageType(message) === "action" ? null : parseChatProfileCardPayload(message.content);
}

function getMessageBeatmapCardPayload(message: ChatMessage) {
  return getMessageType(message) === "action" ? null : parseChatBeatmapCardPayload(message.content);
}

const profileCardReferencePattern = /^profile:\s*\[https:\/\/osu\.novemo\.dev\/(?:u|users)\/(\d+)\s+[^\]\n]+\](?:\s*\|\s*([a-z]+))?/i;
const beatmapCardReferencePattern = /\[https:\/\/osu\.novemo\.dev\/b\/(\d+)\s+[^\]\n]+\]/i;

function getProfileCardMode(value?: string | null): Ruleset | null {
  if (value == null) {
    return null;
  }

  const normalized = value.toLowerCase() === "ctb" ? "fruits" : value.toLowerCase();

  return normalized === "osu" || normalized === "taiko" || normalized === "fruits" || normalized === "mania"
    ? normalized
    : null;
}

function getMessageProfileCardReference(message: ChatMessage): ChatMessageCardReference | null {
  if (getMessageType(message) === "action") {
    return null;
  }

  const match = profileCardReferencePattern.exec(message.content.trim());
  if (match == null) {
    return null;
  }

  const userId = Number(match[1]);
  if (!Number.isFinite(userId)) {
    return null;
  }

  return {
    mode: getProfileCardMode(match[2]),
    type: "profile-reference",
    userId,
  };
}

function getMessageBeatmapCardReference(message: ChatMessage): ChatMessageCardReference | null {
  const content = message.content.trim();
  const type = getMessageType(message);

  if (type !== "action" && !content.startsWith("beatmap:")) {
    return null;
  }

  const match = beatmapCardReferencePattern.exec(content);
  if (match == null) {
    return null;
  }

  const beatmapId = Number(match[1]);
  if (!Number.isFinite(beatmapId)) {
    return null;
  }

  return {
    beatmapId,
    type: "beatmap-reference",
  };
}

function getMessageCardPayload(message: ChatMessage): ChatMessageCardContent | null {
  const profile = getMessageProfileCardPayload(message);

  if (profile != null) {
    return {
      payload: profile,
      type: "profile",
    };
  }

  const beatmap = getMessageBeatmapCardPayload(message);

  if (beatmap != null) {
    return {
        payload: beatmap,
        type: "beatmap",
      };
  }

  return getMessageProfileCardReference(message) ?? getMessageBeatmapCardReference(message);
}

function getMessageTimestamp(message: ChatMessage) {
  return message.timestamp ?? new Date().toISOString();
}

function getChannelCurrentUserAttributes(channel: ChatChannel | null) {
  return (channel?.current_user_attributes ?? channel?.currentUserAttributes ?? null) as ChatChannelAttributes | null;
}

function getChannelLastReadId(channel: ChatChannel | null) {
  const attributes = getChannelCurrentUserAttributes(channel);
  const id =
    attributes?.last_read_id ??
    attributes?.lastReadId ??
    channel?.last_read_id ??
    channel?.lastReadId;

  return typeof id === "number" ? id : null;
}

function getAvatarUrl(value?: { avatar_url?: string; avatarUrl?: string } | null) {
  return value?.avatar_url ?? value?.avatarUrl ?? null;
}

function getUserProfileHref(user: ChatUser | null, fallbackUserId: number | null) {
  const userId = user?.id ?? fallbackUserId;

  return userId == null ? null : `/users/${encodeURIComponent(String(userId))}`;
}

function getChannelName(channel: ChatChannel) {
  return channel.name ?? `channel ${channel.channel_id ?? channel.channelId ?? ""}`.trim();
}

function getChannelDescription(channel: ChatChannel) {
  return channel.description?.trim() ?? "";
}

function formatMessageTime(timestamp: string) {
  const date = new Date(timestamp);

  if (Number.isNaN(date.valueOf())) {
    return "";
  }

  return date.toLocaleTimeString([], {
    hour: "2-digit",
    minute: "2-digit",
  });
}

function formatDayDivider(timestamp: string) {
  const date = new Date(timestamp);

  if (Number.isNaN(date.valueOf())) {
    return timestamp;
  }

  return date.toLocaleDateString([], {
    day: "numeric",
    month: "long",
    year: "numeric",
  });
}

function getDayKey(timestamp: string) {
  const date = new Date(timestamp);

  if (Number.isNaN(date.valueOf())) {
    return timestamp;
  }

  return date.toLocaleDateString();
}

function getMinuteKey(timestamp: string) {
  return formatMessageTime(timestamp);
}

function buildConversationStack(
  messages: ChatMessage[],
  channel: ChatChannel | null,
  currentUserId: number | null,
) {
  const entries: ConversationEntry[] = [];
  let currentGroup: ChatMessage[] = [];
  let unreadMarkerShown = false;
  let currentDay: string | null = null;
  const lastReadId = getChannelLastReadId(channel) ?? -1;

  const flushGroup = () => {
    if (currentGroup.length === 0) {
      return;
    }

    entries.push({
      messages: currentGroup,
      type: "group",
    });
    currentGroup = [];
  };

  messages.forEach((message) => {
    const messageId = getMessageNumericId(message);
    const senderId = getMessageSenderId(message);
    const timestamp = getMessageTimestamp(message);
    const dayKey = getDayKey(timestamp);

    if (!unreadMarkerShown && messageId > lastReadId && senderId !== currentUserId) {
      unreadMarkerShown = true;
      flushGroup();
      entries.push({
        timestamp,
        type: "unread",
      });
    }

    if (entries.length === 0 || dayKey !== currentDay) {
      flushGroup();
      entries.push({
        timestamp,
        type: "day",
      });
      currentDay = dayKey;
    }

    const cardPayload = getMessageCardPayload(message);

    if (cardPayload != null) {
      flushGroup();
      currentGroup.push(message);
      flushGroup();
      return;
    }

    const lastMessage = currentGroup.at(-1);

    if (lastMessage == null || getMessageSenderId(lastMessage) === senderId) {
      currentGroup.push(message);
      return;
    }

    flushGroup();
    currentGroup.push(message);
  });

  flushGroup();

  return entries;
}

function profileUserToCardUser(user: ProfileUser, mode: Ruleset | null): ChatProfileCardUser {
  const playmode = mode ?? getProfileCardMode(user.playmode) ?? "osu";

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
    playmode,
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

function beatmapLookupToCardBeatmap({
  beatmap,
  beatmapset,
}: BeatmapLookupResponse): ChatBeatmapCardBeatmap {
  const artist = beatmapset.artist_unicode || beatmapset.artist;
  const title = beatmapset.title_unicode || beatmapset.title;

  return {
    artist,
    beatmapId: beatmap.id,
    beatmapsetId: beatmapset.id,
    beatmapsetUrl: `/beatmapsets/${beatmapset.id}#${beatmap.mode}/${beatmap.id}`,
    bpm: beatmap.bpm || beatmapset.bpm,
    coverUrl: beatmapset.covers.slimcover || beatmapset.covers.cover || beatmapset.covers.card,
    creator: beatmapset.user.username ?? beatmapset.creator,
    creatorId: beatmapset.user_id,
    difficultyRating: beatmap.difficulty_rating,
    favouriteCount: beatmapset.favourite_count,
    mode: beatmap.mode,
    playCount: beatmapset.play_count,
    source: beatmapset.source,
    status: beatmapset.status,
    storyboard: beatmapset.storyboard,
    title,
    totalLength: beatmap.total_length,
    url: `https://osu.novemo.dev/b/${beatmap.id}`,
    version: beatmap.version,
    video: beatmapset.video,
  };
}

function getProfileCardHref(userId: number, mode: Ruleset | null) {
  const searchParams = new URLSearchParams();

  if (mode != null) {
    searchParams.set("mode", mode);
  }

  const query = searchParams.toString();
  const search = query.length === 0 ? "" : `?${query}`;

  return `/api/users/${encodeURIComponent(String(userId))}${search}`;
}

function getBeatmapCardHref(beatmapId: number) {
  return `/api/beatmaps/${encodeURIComponent(String(beatmapId))}`;
}

async function fetchProfileMessageCard(href: string, mode: Ruleset | null): Promise<ChatMessageCardPayload> {
  const response = await fetch(href, { cache: "no-store" });

  if (!response.ok) {
    throw new Error("could not load user");
  }

  return {
    payload: {
      format: "card",
      type: "profile",
      user: profileUserToCardUser((await response.json()) as ProfileUser, mode),
      version: 1,
    },
    type: "profile",
  };
}

async function fetchBeatmapMessageCard(href: string): Promise<ChatMessageCardPayload> {
  const response = await fetch(href, { cache: "no-store" });

  if (!response.ok) {
    throw new Error("could not load beatmap");
  }

  return {
    payload: {
      beatmap: beatmapLookupToCardBeatmap((await response.json()) as BeatmapLookupResponse),
      format: "card",
      type: "beatmap",
      version: 1,
    },
    type: "beatmap",
  };
}

function ChatResolvedMessageCard({
  card,
  fallbackBubble,
  message,
  usersByUsername,
  variant,
}: {
  card: ChatMessageCardReference;
  fallbackBubble: boolean;
  message: ChatMessage;
  usersByUsername: ReadonlyMap<string, ChatMentionUser>;
  variant: "overlay" | "page";
}) {
  const referenceType = card.type;
  const profileUserId = card.type === "profile-reference" ? card.userId : null;
  const profileMode = card.type === "profile-reference" ? card.mode : null;
  const beatmapId = card.type === "beatmap-reference" ? card.beatmapId : null;
  const resolvedCardHref = useMemo(() => {
    if (referenceType === "profile-reference" && profileUserId != null) {
      return getProfileCardHref(profileUserId, profileMode);
    }

    if (beatmapId != null) {
      return getBeatmapCardHref(beatmapId);
    }

    return null;
  }, [beatmapId, profileMode, profileUserId, referenceType]);
  const { data: resolvedCard } = useSWR<ChatMessageCardPayload>(
    resolvedCardHref,
    (href: string) => referenceType === "profile-reference"
      ? fetchProfileMessageCard(href, profileMode)
      : fetchBeatmapMessageCard(href),
  );

  if (resolvedCard != null) {
    return (
      <ChatMessageCard
        card={resolvedCard}
        fallbackBubble={fallbackBubble}
        message={message}
        usersByUsername={usersByUsername}
        variant={variant}
      />
    );
  }

  const fallback = <ChatOriginalMessageContent message={message} usersByUsername={usersByUsername} />;

  return fallbackBubble ? <ChatMessageBubble variant={variant}>{fallback}</ChatMessageBubble> : fallback;
}

function ChatMessageCard({
  card,
  fallbackBubble = false,
  message,
  usersByUsername,
  variant,
}: {
  card: ChatMessageCardContent;
  fallbackBubble?: boolean;
  message: ChatMessage;
  usersByUsername: ReadonlyMap<string, ChatMentionUser>;
  variant: "overlay" | "page";
}) {
  if (card.type === "profile-reference" || card.type === "beatmap-reference") {
    return (
      <ChatResolvedMessageCard
        card={card}
        fallbackBubble={fallbackBubble}
        message={message}
        usersByUsername={usersByUsername}
        variant={variant}
      />
    );
  }

  return card.type === "profile"
    ? <ChatProfileCard payload={card.payload} variant={variant} />
    : <ChatBeatmapCard payload={card.payload} variant={variant} />;
}

function ChatMessageBubble({
  children,
  variant,
}: {
  children: ReactNode;
  variant: "overlay" | "page";
}) {
  return (
    <div className={cn(
      "min-w-0 overflow-x-auto rounded-md bg-osu-b6",
      variant === "overlay" ? "max-w-49 px-2.5 py-2" : "max-w-[560px] px-2 py-1.5",
    )}>
      {children}
    </div>
  );
}

function resizeMessageInput(textarea: HTMLTextAreaElement) {
  textarea.style.height = "auto";

  const style = window.getComputedStyle(textarea);
  const lineHeight = Number.parseFloat(style.lineHeight) || 20;
  const paddingTop = Number.parseFloat(style.paddingTop) || 0;
  const paddingBottom = Number.parseFloat(style.paddingBottom) || 0;
  const maxHeight = Math.ceil(lineHeight * 3 + paddingTop + paddingBottom);
  const nextHeight = Math.min(textarea.scrollHeight, maxHeight);

  textarea.style.height = `${nextHeight}px`;
  textarea.style.overflowY = textarea.scrollHeight > maxHeight ? "auto" : "hidden";
}

function ChatAvatar({
  className,
  fallback,
  href,
  url,
}: {
  className?: string;
  fallback: string;
  href?: string | null;
  url?: string | null;
}) {
  const avatar = (
    <span
      className={cn(
        "flex shrink-0 items-center justify-center overflow-hidden rounded-full bg-osu-b6 bg-cover bg-center text-xs font-semibold text-osu-c1",
        className,
      )}
      style={url == null ? undefined : { backgroundImage: `url(${JSON.stringify(url)})` }}
    >
      {url == null ? fallback : null}
    </span>
  );

  return href == null ? avatar : (
    <Link aria-label={fallback} className="shrink-0" href={href}>
      {avatar}
    </Link>
  );
}

function ChatOriginalMessageContent({
  message,
  usersByUsername,
}: {
  message: ChatMessage;
  usersByUsername: ReadonlyMap<string, ChatMentionUser>;
}) {
  const type = getMessageType(message);

  return (
    <div
      className={cn(
        "min-w-0 flex-1 [overflow-wrap:anywhere] text-sm leading-[1.5] text-white",
        type === "action" && "text-osu-l1 italic",
      )}
    >
      {type === "action" ? <span>* </span> : null}
      {renderChatTextWithLinks(message.content, usersByUsername)}
      {type === "action" ? <span> *</span> : null}
    </div>
  );
}

function ChatMessageContent({
  message,
  variant,
  usersByUsername,
}: {
  message: ChatMessage;
  variant: "overlay" | "page";
  usersByUsername: ReadonlyMap<string, ChatMentionUser>;
}) {
  const cardPayload = getMessageCardPayload(message);

  if (cardPayload != null) {
    return (
      <ChatMessageCard
        card={cardPayload}
        fallbackBubble={false}
        message={message}
        usersByUsername={usersByUsername}
        variant={variant}
      />
    );
  }

  return <ChatOriginalMessageContent message={message} usersByUsername={usersByUsername} />;
}

function ChatMessageGroup({
  currentUser,
  messages,
  usersById,
  variant,
  usersByUsername,
}: {
  currentUser: ChatUser | null;
  messages: ChatMessage[];
  usersById: Record<number, ChatUser>;
  variant: "overlay" | "page";
  usersByUsername: ReadonlyMap<string, ChatMentionUser>;
}) {
  const sender = getMessageSender(messages[0], usersById, currentUser);
  const senderId = getMessageSenderId(messages[0]);
  const own = senderId != null && senderId === currentUser?.id;
  const avatarUrl = getAvatarUrl(sender);
  const profileHref = getUserProfileHref(sender, senderId);
  const username = sender?.username ?? `user ${senderId ?? "?"}`;
  const usernameClassName = variant === "overlay" ? "max-w-11" : "max-w-[60px]";
  const cardPayload = messages.length === 1 ? getMessageCardPayload(messages[0]) : null;

  if (cardPayload != null) {
    const timestamp = formatMessageTime(getMessageTimestamp(messages[0]));

    return (
      <div
        className={cn(
          "my-2.5 flex min-w-0 text-white",
          variant === "overlay" && "mx-2 gap-2",
          variant === "page" && "sm:mr-[120px]",
          own && "flex-row-reverse",
          own && variant === "page" && "sm:mr-0 sm:ml-[120px]",
        )}
      >
        <div className={cn(
          "flex shrink-0 flex-col items-center text-xs text-white",
          variant === "overlay" ? "w-12" : "w-20",
        )}>
          <ChatAvatar
            className={cn(
              "mb-1.5",
              variant === "overlay" ? "h-[30px] w-[30px]" : "h-[35px] w-[35px]",
            )}
            fallback={username.slice(0, 1)}
            href={profileHref}
            url={avatarUrl}
          />
          {profileHref == null ? (
            <div className={cn("truncate", usernameClassName)}>{username}</div>
          ) : (
            <Link className={cn("truncate transition-colors hover:text-osu-l1", usernameClassName)} href={profileHref}>
              {username}
            </Link>
          )}
        </div>

        <div className="min-w-0">
          <ChatMessageCard
            card={cardPayload}
            fallbackBubble
            message={messages[0]}
            usersByUsername={usersByUsername}
            variant={variant}
          />
          <div className={cn("mt-1 text-xs text-osu-f1", own && "text-right")}>{timestamp}</div>
        </div>
      </div>
    );
  }

  return (
    <div
      className={cn(
        "my-2.5 flex min-w-0 text-white",
        variant === "overlay" && "mx-2 gap-2",
        variant === "page" && "sm:mr-[120px]",
        own && "flex-row-reverse",
        own && variant === "page" && "sm:mr-0 sm:ml-[120px]",
      )}
    >
      <div className={cn(
        "flex shrink-0 flex-col items-center text-xs text-white",
        variant === "overlay" ? "w-12" : "w-20",
      )}>
        <ChatAvatar
          className={cn(
            "mb-1.5",
            variant === "overlay" ? "h-[30px] w-[30px]" : "h-[35px] w-[35px]",
          )}
          fallback={username.slice(0, 1)}
          href={profileHref}
          url={avatarUrl}
        />
        {profileHref == null ? (
          <div className={cn("truncate", usernameClassName)}>{username}</div>
        ) : (
          <Link className={cn("truncate transition-colors hover:text-osu-l1", usernameClassName)} href={profileHref}>
            {username}
          </Link>
        )}
      </div>

      <ChatMessageBubble variant={variant}>
        {messages.map((message, index) => {
          const timestamp = formatMessageTime(getMessageTimestamp(message));
          const nextMessage = messages[index + 1];
          const showTimestamp =
            nextMessage == null ||
            timestamp !== getMinuteKey(getMessageTimestamp(nextMessage));

          return (
            <div key={getMessageId(message)} className={cn(variant === "overlay" && index > 0 && "mt-2")}>
              <div className={cn("flex justify-between gap-3", variant === "page" && "m-1")}>
                <ChatMessageContent message={message} usersByUsername={usersByUsername} variant={variant} />
              </div>
              {showTimestamp ? (
                <div className={cn("text-xs text-osu-f1", variant === "overlay" ? "mt-1" : "m-1")}>{timestamp}</div>
              ) : null}
            </div>
          );
        })}
      </ChatMessageBubble>
    </div>
  );
}

function ChatDivider({
  timestamp,
  type,
}: {
  timestamp: string;
  type: "day" | "unread";
}) {
  if (type === "unread") {
    return (
      <div className="relative mx-[clamp(24px,18vw,100px)] my-1.5 h-4 text-center text-[10px] leading-4 text-osu-h1 before:absolute before:top-1/2 before:left-0 before:h-px before:w-full before:bg-osu-h1 before:content-['']">
        <span className="relative bg-osu-b4 px-2">unread messages</span>
      </div>
    );
  }

  return (
    <div className="pt-5 text-center text-[10px] font-bold uppercase text-osu-l1">
      {formatDayDivider(timestamp)}
    </div>
  );
}

function ChatConversationIntro({
  channel,
}: {
  channel: ChatChannel;
}) {
  const channelIcon = channel.icon ?? null;
  const channelName = getChannelName(channel);
  const isPm = channel.type === "PM";

  return (
    <>
      <div className="my-2.5 flex w-full justify-center">
        <ChatAvatar
          className="h-16 w-16"
          fallback={channelName.slice(0, 1)}
          url={channelIcon}
        />
      </div>
      <div className="mx-5 [overflow-wrap:anywhere] text-center text-[10px] font-bold uppercase text-white">
        {isPm ? `talking with ${channelName}` : `talking in ${channelName}`}
      </div>
      {getChannelDescription(channel).length > 0 ? (
        <div className="mx-5 [overflow-wrap:anywhere] text-center text-[10px] font-bold uppercase text-white">
          {getChannelDescription(channel)}
        </div>
      ) : null}
    </>
  );
}

export function ChatNoChannel({
  children,
  title,
}: {
  children?: ReactNode;
  title: string;
}) {
  return (
    <div className="flex h-full min-h-[250px] w-full flex-col items-center justify-center px-5 py-8 text-center text-osu-l1">
      <div className="text-[26px] font-light text-white [text-shadow:0_1px_3px_rgba(0,0,0,0.65)]">{title}</div>
      {children == null ? null : <div className="mt-2.5 text-sm">{children}</div>}
    </div>
  );
}

export function ChatComposerShell({
  buttonLabel,
  buttonVariant = "page",
  disabled,
  leadingContent,
  maxLength,
  menuContent,
  name,
  onChange,
  onKeyDown,
  onSubmit,
  placeholder,
  readOnly = false,
  resizeSignal,
  sendDisabled,
  sending,
  textareaRef,
  value,
}: {
  buttonLabel?: ReactNode;
  buttonVariant?: "icon" | "page";
  disabled: boolean;
  leadingContent?: ReactNode;
  maxLength?: number;
  menuContent?: ReactNode;
  name?: string;
  onChange: (value: string) => void;
  onKeyDown?: (event: KeyboardEvent<HTMLTextAreaElement>) => void;
  onSubmit: (event: FormEvent<HTMLFormElement>) => void;
  placeholder: string;
  readOnly?: boolean;
  resizeSignal?: unknown;
  sendDisabled: boolean;
  sending: boolean;
  textareaRef?: MutableRefObject<HTMLTextAreaElement | null>;
  value: string;
}) {
  const internalRef = useRef<HTMLTextAreaElement | null>(null);
  const buttonClassName = buttonVariant === "icon" ? chatOverlayActionButtonClassName : chatPageActionButtonClassName;

  const setTextareaRef = useCallback((node: HTMLTextAreaElement | null) => {
    internalRef.current = node;

    if (textareaRef != null) {
      textareaRef.current = node;
    }
  }, [textareaRef]);

  useEffect(() => {
    if (internalRef.current == null) {
      return;
    }

    resizeMessageInput(internalRef.current);
  }, [resizeSignal, value]);

  return (
    <form className="relative flex w-full items-end gap-2" onSubmit={onSubmit}>
      {menuContent}

      <label className="flex min-h-10 min-w-0 flex-1 items-center gap-1.5 rounded-md border border-osu-b4 bg-osu-b6 px-2 text-base text-osu-f1 transition-colors focus-within:border-osu-h1">
        <span className="sr-only">message</span>
        {leadingContent}
        <textarea
          ref={setTextareaRef}
          autoComplete="off"
          className="min-h-10 min-w-0 flex-1 resize-none bg-transparent px-1 py-2 text-base leading-6 text-white caret-osu-h1 outline-none placeholder:text-osu-f1 disabled:opacity-80"
          disabled={disabled}
          maxLength={maxLength}
          name={name}
          onChange={(event) => onChange(event.target.value)}
          onKeyDown={onKeyDown}
          placeholder={placeholder}
          readOnly={readOnly}
          rows={1}
          value={value}
        />
      </label>

      <button className={buttonClassName} disabled={sendDisabled} type="submit">
        {sending ? <Loader2 className="h-4 w-4 animate-spin" /> : <Send className="h-4 w-4" />}
        {buttonVariant === "icon" ? <span className="sr-only">send</span> : <span>{buttonLabel ?? "send"}</span>}
      </button>
    </form>
  );
}

export function ChatConversationFrame({
  canMessage,
  canMessageError,
  channel,
  composer,
  currentUser,
  currentUserId,
  error,
  loading,
  messages,
  usersById,
  variant = "page",
}: {
  canMessage: boolean;
  canMessageError?: string | null;
  channel: ChatChannel;
  composer: ReactNode;
  currentUser: ChatUser | null;
  currentUserId: number | null;
  error: string | null;
  loading: boolean;
  messages: ChatMessage[];
  usersById: Record<number, ChatUser>;
  variant?: "overlay" | "page";
}) {
  const messagesEndRef = useRef<HTMLDivElement | null>(null);
  const messagesContentRef = useRef<HTMLDivElement | null>(null);
  const scrollAreaRef = useRef<HTMLDivElement | null>(null);
  const [scrollbarInset, setScrollbarInset] = useState(0);
  const conversationStack = useMemo(
    () => buildConversationStack(messages, channel, currentUserId),
    [channel, currentUserId, messages],
  );
  const usersByUsername = useMemo(() => {
    const next = new Map<string, ChatMentionUser>();

    for (const user of Object.values(usersById)) {
      next.set(user.username.toLowerCase(), user);
    }

    if (currentUser != null) {
      next.set(currentUser.username.toLowerCase(), currentUser);
    }

    for (const message of messages) {
      if (message.sender != null) {
        next.set(message.sender.username.toLowerCase(), message.sender);
      }
    }

    return next;
  }, [currentUser, messages, usersById]);

  useEffect(() => {
    messagesEndRef.current?.scrollIntoView({ block: "end" });
  }, [messages.length]);

  useEffect(() => {
    const scrollArea = scrollAreaRef.current;

    if (scrollArea == null) {
      return;
    }

    let animationFrame = 0;

    const updateScrollbarInset = () => {
      animationFrame = 0;

      const hasScrollbar = scrollArea.scrollHeight > scrollArea.clientHeight + 1;
      const nextInset = hasScrollbar ? Math.max(0, scrollArea.offsetWidth - scrollArea.clientWidth) : 0;

      setScrollbarInset((current) => (current === nextInset ? current : nextInset));
    };

    const scheduleUpdate = () => {
      if (animationFrame !== 0) {
        window.cancelAnimationFrame(animationFrame);
      }

      animationFrame = window.requestAnimationFrame(updateScrollbarInset);
    };

    const resizeObserver = new ResizeObserver(scheduleUpdate);
    resizeObserver.observe(scrollArea);

    if (messagesContentRef.current != null) {
      resizeObserver.observe(messagesContentRef.current);
    }

    window.addEventListener("resize", scheduleUpdate);
    scheduleUpdate();

    return () => {
      if (animationFrame !== 0) {
        window.cancelAnimationFrame(animationFrame);
      }

      resizeObserver.disconnect();
      window.removeEventListener("resize", scheduleUpdate);
    };
  }, [error, loading, messages.length]);

  return (
    <div className="relative h-full min-h-0 bg-osu-b4">
      <div
        ref={scrollAreaRef}
        className={cn(
          "h-full overflow-y-auto",
          variant === "overlay" ? "py-3 pb-24" : "px-5 py-2.5 pb-28 max-sm:px-0",
        )}
      >
        <div ref={messagesContentRef}>
          <ChatConversationIntro channel={channel} />

          {error == null ? null : (
            <div className={cn(
              "rounded-md bg-osu-red-3/20 px-3 py-2 text-xs text-osu-red-2",
              variant === "overlay" ? "mb-2" : "mx-auto my-3 max-w-md text-center",
            )}>
              {error}
            </div>
          )}

          {loading && messages.length === 0 ? (
            <div className="pt-5 text-center text-osu-l1">
              <Loader2 className="mx-auto h-5 w-5 animate-spin" />
            </div>
          ) : null}

          {conversationStack.map((entry) => {
            if (entry.type === "group") {
              return (
                <ChatMessageGroup
                  key={`group-${getMessageId(entry.messages[0])}`}
                  currentUser={currentUser}
                  messages={entry.messages}
                  usersById={usersById}
                  variant={variant}
                  usersByUsername={usersByUsername}
                />
              );
            }

            return (
              <ChatDivider
                key={`${entry.type}-${entry.timestamp}`}
                timestamp={entry.timestamp}
                type={entry.type}
              />
            );
          })}

          {!canMessage ? (
            <div className="mt-2.5 px-2 py-1 text-center text-[10px] text-osu-l1">
              {channel.type === "PM" ? "you cannot send messages to this user" : "you cannot send messages to this channel"}
              {canMessageError == null ? null : ` ${canMessageError}`}
            </div>
          ) : null}

          <div ref={messagesEndRef} />
        </div>
      </div>

      <div
        className={cn(
          "absolute right-0 bottom-0 left-0 z-[150] bg-linear-to-b from-osu-b4/0 via-osu-b4/95 to-osu-b4/95 text-right text-white",
          variant === "overlay" ? "px-2 pt-2.5 pb-3" : "pt-2.5 pr-[30px] pb-[15px] pl-[50px] max-sm:pl-5 max-sm:pr-0",
        )}
        style={scrollbarInset === 0 ? undefined : { right: scrollbarInset }}
      >
        {composer}
      </div>
    </div>
  );
}

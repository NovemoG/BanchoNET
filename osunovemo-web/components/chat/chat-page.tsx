"use client";

import {
  useCallback,
  useEffect,
  useMemo,
  useRef,
  useState,
} from "react";
import Image from "next/image";
import { usePathname } from "next/navigation";
import {
  ChevronRight,
  Hash,
  Inbox,
  Loader2,
  Megaphone,
  MessageCircle,
  Search,
  Users,
} from "lucide-react";
import { useSession } from "@/components/auth/session-provider";
import { Header } from "@/components/header";
import {
  ChatConversationFrame,
  ChatNoChannel,
  chatPageActionButtonClassName,
  type ParsedOutgoingMessage,
} from "@/components/chat/chat-conversation";
import { ChatCommandComposer } from "@/components/chat/chat-command-composer";
import { PageWrapper } from "@/components/page-wrapper";
import { useRealtime } from "@/components/realtime/realtime-provider";
import { Skeleton } from "@/components/ui/skeleton";
import type {
  ChatChannel,
  ChatChannelDetailsResponse,
  ChatChannelType,
  ChatMessage,
  ChatMessagesNewEventData,
  ChatUpdatesResponse,
  ChatUser,
} from "@/lib/chat/types";
import { cn } from "@/lib/utils";

const channelTypeOrder: ChatChannelType[] = ["PM", "PUBLIC", "ANNOUNCE", "TEAM", "GROUP"];
const channelTypeLabels: Record<string, string> = {
  ANNOUNCE: "announcements",
  GROUP: "groups",
  PM: "private messages",
  PUBLIC: "public channels",
  TEAM: "team",
};

type ChatChannelAttributes = {
  can_message?: boolean;
  can_message_error?: string | null;
  canMessage?: boolean;
  canMessageError?: string | null;
  last_read_id?: number | null;
  lastReadId?: number | null;
};

type ChatPageProps = {
  initialChannelId?: number | null;
  redirectToFirstChannel?: boolean;
};

type ChatMessagesResponse =
  | ChatMessage[]
  | {
      messages?: ChatMessage[];
      users?: ChatUser[];
    };

function ChatPageHeader({ connectionStatus }: { connectionStatus: string }) {
  return (
    <Header
      background={
        <div className="absolute inset-0 bg-osu-d5">
          <Image
            alt=""
            className="object-cover opacity-70 hue-rotate-[115deg]"
            fill
            priority
            sizes="100vw"
            src="/headers/chat.jpg"
          />
          <div className="absolute inset-0 bg-[linear-gradient(to_right,hsl(var(--hsl-d5)),transparent_18%,transparent_82%,hsl(var(--hsl-d5)))]" />
        </div>
      }
      icon={
        <div className="flex w-10 flex-none items-center justify-center self-stretch">
          <MessageCircle className="h-5 w-5 text-white" />
        </div>
      }
      mobileSubtitle={connectionStatus}
      title="chat"
      topClassName="bg-osu-d5 text-osu-c1"
      topRight={
        <span
          className={cn(
            "h-2 w-2 rounded-full",
            connectionStatus === "connected" ? "bg-osu-green-2" : "bg-osu-orange-3",
          )}
          title={connectionStatus}
        />
      }
    />
  );
}

function getChannelId(channel: ChatChannel) {
  const id = channel.channel_id ?? channel.channelId;

  return typeof id === "number" ? id : null;
}

function getChannelIdFromPathname(pathname: string | null) {
  const match = pathname?.match(/^\/community\/chat\/([^/]+)$/);
  const channelId = match == null ? Number.NaN : Number.parseInt(match[1], 10);

  return Number.isFinite(channelId) && channelId > 0 ? channelId : null;
}

function updateChatChannelPath(channelId: number, mode: "push" | "replace") {
  const nextPath = `/community/chat/${channelId}`;

  if (window.location.pathname === nextPath) {
    return;
  }

  if (mode === "replace") {
    window.history.replaceState(null, "", nextPath);
  } else {
    window.history.pushState(null, "", nextPath);
  }
}

function getChannelLastMessageId(channel: ChatChannel) {
  const id = channel.last_message_id ?? channel.lastMessageId;

  return typeof id === "number" ? id : 0;
}

function getMessageChannelId(message: ChatMessage) {
  const id = message.channel_id ?? message.channelId;

  return typeof id === "number" ? id : null;
}

function getMessageNumericId(message: ChatMessage) {
  const id = message.message_id ?? message.messageId;

  return typeof id === "number" ? id : 0;
}

function getChannelCurrentUserAttributes(channel: ChatChannel | null) {
  return (channel?.current_user_attributes ?? channel?.currentUserAttributes ?? null) as ChatChannelAttributes | null;
}

function getChannelMessageLengthLimit(channel: ChatChannel | null) {
  return channel?.message_length_limit ?? channel?.messageLengthLimit ?? 450;
}

function getMessageId(message: ChatMessage) {
  return `${getMessageChannelId(message) ?? "unknown"}:${getMessageNumericId(message)}:${message.uuid ?? ""}`;
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

function getChannelName(channel: ChatChannel) {
  return channel.name ?? `channel ${channel.channel_id ?? ""}`.trim();
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

  return [...channels.values()].sort((left, right) => {
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

function getChannelActivityId(channel: ChatChannel, messagesByChannel: Record<number, ChatMessage[]>) {
  const channelId = getChannelId(channel);
  const loadedMessages = channelId == null ? [] : messagesByChannel[channelId] ?? [];
  const loadedLatestId = loadedMessages.reduce(
    (latest, message) => Math.max(latest, getMessageNumericId(message)),
    0,
  );

  return Math.max(getChannelLastMessageId(channel), loadedLatestId);
}

function sortChannelsByActivity(
  channels: ChatChannel[],
  messagesByChannel: Record<number, ChatMessage[]>,
) {
  return [...channels].sort((left, right) => {
    const activityDelta =
      getChannelActivityId(right, messagesByChannel) -
      getChannelActivityId(left, messagesByChannel);

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

function normalizeChatMessagesResponse(response: ChatMessagesResponse) {
  if (Array.isArray(response)) {
    return {
      messages: response,
      users: [],
    };
  }

  return {
    messages: response.messages ?? [],
    users: response.users ?? [],
  };
}

function chatUserFromSessionUser(user: { avatar_url?: string | null; id: number; username: string } | null | undefined) {
  if (user == null) {
    return null;
  }

  return {
    avatar_url: user.avatar_url ?? undefined,
    id: user.id,
    username: user.username,
  } satisfies ChatUser;
}

function getMessageUsers(messages: ChatMessage[]) {
  return messages.flatMap((message) => (message.sender == null ? [] : [message.sender]));
}

function mergeUsersById(current: Record<number, ChatUser>, users: ChatUser[]) {
  let next = current;

  for (const user of users) {
    if (typeof user.id !== "number") {
      continue;
    }

    if (next === current) {
      next = { ...current };
    }

    next[user.id] = {
      ...next[user.id],
      ...user,
    };
  }

  return next;
}

function groupChannels(channels: ChatChannel[]) {
  return channels.reduce<Record<string, ChatChannel[]>>((groups, channel) => {
    const type = channel.type ?? "PUBLIC";
    groups[type] ??= [];
    groups[type].push(channel);

    return groups;
  }, {});
}

function ChannelSkeleton() {
  return (
    <div className="ml-2.5 flex h-10 items-center gap-2.5 pl-5 pr-2.5 max-md:ml-0 max-md:pl-1">
      <Skeleton className="h-[30px] w-[30px] rounded-full" />
      <div className="min-w-0 flex-1">
        <Skeleton className="h-3 w-28" />
      </div>
    </div>
  );
}

export function ChatPage({
  initialChannelId = null,
  redirectToFirstChannel = false,
}: ChatPageProps) {
  const pathname = usePathname();
  const { data: session, status } = useSession();
  const { connectionStatus, lastEvent } = useRealtime();
  const [joinedChannels, setJoinedChannels] = useState<ChatChannel[]>([]);
  const [availableChannels, setAvailableChannels] = useState<ChatChannel[]>([]);
  const [selectedChannelId, setSelectedChannelId] = useState<number | null>(initialChannelId);
  const [messagesByChannel, setMessagesByChannel] = useState<Record<number, ChatMessage[]>>({});
  const [usersById, setUsersById] = useState<Record<number, ChatUser>>({});
  const [channelSearch, setChannelSearch] = useState("");
  const [loadingChannels, setLoadingChannels] = useState(true);
  const [loadingMessages, setLoadingMessages] = useState(false);
  const [joining, setJoining] = useState(false);
  const [sending, setSending] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const lastHandledEventRef = useRef<typeof lastEvent>(null);
  const pathnameChannelId = useMemo(() => getChannelIdFromPathname(pathname), [pathname]);

  const joinedChannelIds = useMemo(
    () => new Set(joinedChannels.map(getChannelId).filter((id): id is number => id != null)),
    [joinedChannels],
  );
  const channels = useMemo(
    () => sortChannelsByActivity(
      mergeChannels(availableChannels, joinedChannels),
      messagesByChannel,
    ),
    [availableChannels, joinedChannels, messagesByChannel],
  );
  const filteredChannels = useMemo(
    () => channels.filter((channel) => channelMatchesSearch(channel, channelSearch, usersById)),
    [channelSearch, channels, usersById],
  );
  const channelsByGroup = useMemo(() => groupChannels(filteredChannels), [filteredChannels]);
  const selectedChannel = useMemo(
    () => channels.find((channel) => getChannelId(channel) === selectedChannelId) ?? null,
    [channels, selectedChannelId],
  );
  const selectedMessages = useMemo(
    () => (selectedChannelId == null ? [] : messagesByChannel[selectedChannelId] ?? []),
    [messagesByChannel, selectedChannelId],
  );
  const selectedChannelJoined = selectedChannelId != null && joinedChannelIds.has(selectedChannelId);
  const selectedChannelAttributes = getChannelCurrentUserAttributes(selectedChannel);
  const selectedChannelCanMessage =
    selectedChannelAttributes?.can_message ??
    selectedChannelAttributes?.canMessage ??
    selectedChannelJoined;
  const selectedChannelCanMessageError =
    selectedChannelAttributes?.can_message_error ??
    selectedChannelAttributes?.canMessageError ??
    null;
  const chatReady = connectionStatus === "connected";
  const currentUserId = session?.user.id ?? null;
  const currentUser = useMemo(() => chatUserFromSessionUser(session?.user), [session?.user]);

  const loadChannels = useCallback(async () => {
    setLoadingChannels(true);
    setError(null);

    try {
      const [updates, publicChannels, selectedChannelDetails] = await Promise.all([
        chatRequest<ChatUpdatesResponse>("updates?since=0&includes[]=presence"),
        chatRequest<ChatChannel[]>("channels"),
        initialChannelId == null
          ? Promise.resolve(null)
          : chatRequest<ChatChannelDetailsResponse>(`channels/${initialChannelId}`).catch(() => null),
      ]);
      const presence = updates.presence ?? [];
      const selectedChannel = selectedChannelDetails?.channel ?? null;
      const selectedUsers = selectedChannelDetails?.users ?? [];
      const mergedChannels = mergeChannels(
        publicChannels,
        presence,
        selectedChannel == null ? [] : [selectedChannel],
      );
      const sortedChannels = sortChannelsByActivity(mergedChannels, {});
      const firstChannel = sortedChannels[0] ?? null;
      const firstChannelId = firstChannel == null ? null : getChannelId(firstChannel);

      setJoinedChannels(presence);
      setAvailableChannels(selectedChannel == null ? publicChannels : mergeChannels(publicChannels, [selectedChannel]));
      setUsersById((current) => mergeUsersById(current, selectedUsers));
      setSelectedChannelId((current) => {
        if (initialChannelId != null) {
          return initialChannelId;
        }

        if (current != null && mergedChannels.some((channel) => getChannelId(channel) === current)) {
          return current;
        }

        return firstChannelId;
      });

      if (initialChannelId == null && redirectToFirstChannel && firstChannelId != null) {
        updateChatChannelPath(firstChannelId, "replace");
      }
    } catch (loadError) {
      setError(loadError instanceof Error ? loadError.message : "Could not load chat.");
    } finally {
      setLoadingChannels(false);
    }
  }, [initialChannelId, redirectToFirstChannel]);

  useEffect(() => {
    const timeout = window.setTimeout(() => {
      setSelectedChannelId(pathnameChannelId ?? initialChannelId);
    }, 0);

    return () => {
      window.clearTimeout(timeout);
    };
  }, [initialChannelId, pathnameChannelId]);

  const handleSelectChannel = useCallback((channelId: number) => {
    setSelectedChannelId(channelId);
    updateChatChannelPath(channelId, "push");
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

  useEffect(() => {
    if (selectedChannelId == null || status !== "authenticated") {
      return;
    }

    let active = true;
    const timeout = window.setTimeout(() => {
      setLoadingMessages(true);
      setError(null);

      chatRequest<ChatMessagesResponse>(`channels/${selectedChannelId}/messages?return_object=1`)
        .then((response) => {
          if (!active) {
            return;
          }

          const { messages, users } = normalizeChatMessagesResponse(response);

          setUsersById((current) => mergeUsersById(current, [...users, ...getMessageUsers(messages)]));
          setMessagesByChannel((current) => ({
            ...current,
            [selectedChannelId]: appendMessages(current[selectedChannelId] ?? [], messages),
          }));
        })
        .catch((loadError) => {
          if (active) {
            setError(loadError instanceof Error ? loadError.message : "Could not load messages.");
          }
        })
        .finally(() => {
          if (active) {
            setLoadingMessages(false);
          }
        });
    }, 0);

    return () => {
      active = false;
      window.clearTimeout(timeout);
    };
  }, [selectedChannelId, status]);

  useEffect(() => {
    if (lastEvent == null || lastEvent === lastHandledEventRef.current) {
      return;
    }

    const eventToHandle = lastEvent;
    lastHandledEventRef.current = eventToHandle;
    let active = true;

    window.queueMicrotask(() => {
      if (!active) {
        return;
      }

      if (eventToHandle.event === "chat.channel.join" && typeof eventToHandle.data === "object" && eventToHandle.data != null) {
        const channel = eventToHandle.data as ChatChannel;
        setJoinedChannels((current) => mergeChannels(current, [channel]));
        return;
      }

      if (eventToHandle.event === "chat.channel.part" && typeof eventToHandle.data === "object" && eventToHandle.data != null) {
        const channelId = getChannelId(eventToHandle.data as ChatChannel);

        if (channelId != null) {
          setJoinedChannels((current) => current.filter((channel) => getChannelId(channel) !== channelId));
        }

        return;
      }

      if (eventToHandle.event !== "chat.message.new" || typeof eventToHandle.data !== "object" || eventToHandle.data == null) {
        return;
      }

      const data = eventToHandle.data as ChatMessagesNewEventData;

      if (!Array.isArray(data.messages) || data.messages.length === 0) {
        return;
      }

      setUsersById((current) => mergeUsersById(current, [...(data.users ?? []), ...getMessageUsers(data.messages ?? [])]));
      setMessagesByChannel((current) => {
        const next = { ...current };

        for (const message of data.messages ?? []) {
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

  const handleJoinSelectedChannel = async () => {
    if (selectedChannelId == null || currentUserId == null) {
      return;
    }

    setJoining(true);
    setError(null);

    try {
      const channel = await chatRequest<ChatChannel>(`channels/${selectedChannelId}/users/${currentUserId}`, {
        method: "PUT",
      });

      setJoinedChannels((current) => mergeChannels(current, [channel ?? selectedChannel].filter(Boolean) as ChatChannel[]));
    } catch (joinError) {
      setError(joinError instanceof Error ? joinError.message : "Could not join channel.");
    } finally {
      setJoining(false);
    }
  };

  const handleSubmit = async (parsedMessage: ParsedOutgoingMessage) => {
    if (selectedChannelId == null || !selectedChannelCanMessage || sending) {
      return;
    }

    setSending(true);
    setError(null);

    try {
      const message = await chatRequest<ChatMessage>(`channels/${selectedChannelId}/messages`, {
        body: JSON.stringify({
          ...parsedMessage,
          uuid: createUuid(),
        }),
        method: "POST",
      });
      const messageWithSender = {
        ...message,
        sender: message.sender ?? currentUser ?? undefined,
      };

      setUsersById((current) => mergeUsersById(current, getMessageUsers([messageWithSender])));
      setMessagesByChannel((current) => ({
        ...current,
        [selectedChannelId]: appendMessages(current[selectedChannelId] ?? [], [messageWithSender]),
      }));
    } catch (sendError) {
      setError(sendError instanceof Error ? sendError.message : "Could not send message.");
      throw sendError;
    } finally {
      setSending(false);
    }
  };

  if (status === "loading") {
    return (
      <section className="flex-1 bg-osu-b6 text-osu-c1">
        <ChatPageHeader connectionStatus={connectionStatus} />
        <PageWrapper className="my-4" modifiers="generic-compact">
          <Skeleton className="h-[520px] w-full" />
        </PageWrapper>
      </section>
    );
  }

  if (status !== "authenticated") {
    return (
      <section className="flex-1 bg-osu-b6 text-osu-c1">
        <ChatPageHeader connectionStatus={connectionStatus} />
        <PageWrapper className="my-4" modifiers="generic">
          <div className="flex min-h-[420px] items-center justify-center px-6 text-center text-osu-f1">
            sign in to use chat
          </div>
        </PageWrapper>
      </section>
    );
  }

  return (
    <section className="flex-1 bg-osu-b6 text-osu-c1">
      <ChatPageHeader connectionStatus={connectionStatus} />
      <PageWrapper
        className="mb-10 flex h-[calc(100svh-250px)] min-h-[520px] flex-col bg-osu-b5 shadow-[0_12px_32px_rgba(0,0,0,0.24)] md:h-[calc(100svh-310px)] lg:h-[calc(100svh-350px)]"
        modifiers="generic-compact"
      >
        <div className="grid min-h-0 flex-1 grid-rows-[230px_minmax(0,1fr)] overflow-hidden bg-osu-b5 md:grid-cols-[270px_minmax(0,1fr)] md:grid-rows-1">
          <aside className="min-h-0 bg-osu-b5">
            <div className="h-full overflow-y-auto pb-2.5">
              <div className="sticky top-0 z-20 bg-osu-b5 px-2.5 py-2">
                <label className="flex h-9 items-center gap-2 rounded-md bg-osu-b6 px-3 text-osu-f1 shadow-[inset_0_0_0_1px_rgba(255,255,255,0.06)] focus-within:shadow-[inset_0_0_0_1px_hsl(var(--hsl-h1))]">
                  <span className="sr-only">search chats</span>
                  <Search className="h-4 w-4 shrink-0" />
                  <input
                    className="min-w-0 flex-1 bg-transparent text-sm text-white caret-osu-h1 outline-none placeholder:text-osu-f1"
                    disabled={loadingChannels}
                    onChange={(event) => setChannelSearch(event.target.value)}
                    placeholder="search"
                    type="search"
                    value={channelSearch}
                  />
                </label>
              </div>

              {loadingChannels ? (
                <>
                  <ChannelSkeleton />
                  <ChannelSkeleton />
                  <ChannelSkeleton />
                </>
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
                    <div key={type} className="mb-1 md:block max-md:flex">
                      <div className="sticky top-[52px] z-10 flex gap-[5px] bg-osu-b5 px-[30px] py-2.5 text-sm text-osu-c1 max-md:static max-md:px-1.5 max-md:py-1.5">
                        <span className="uppercase max-md:hidden">{channelTypeLabels[type] ?? type.toLowerCase()}</span>
                      </div>

                      {groupedChannels.map((channel) => {
                        const id = getChannelId(channel);
                        const active = id === selectedChannelId;

                        return (
                          <div
                            key={`${channel.type}-${id}`}
                            className={cn(
                              "group/channel relative ml-2.5 flex h-10 w-[calc(100%-10px)] min-w-0 items-center pl-2.5 text-white transition-colors max-md:ml-0 max-md:w-auto max-md:pl-0",
                              active
                                ? "bg-[linear-gradient(to_right,hsl(var(--hsl-b5))_10px,hsl(var(--hsl-b4))_33%,hsl(var(--hsl-b4))_100%)] max-md:bg-osu-b4"
                                : "hover:bg-[linear-gradient(to_right,hsl(var(--hsl-b5))_10px,hsl(var(--hsl-b3))_33%,hsl(var(--hsl-b3))_100%)]",
                            )}
                          >
                            <button
                              aria-pressed={active}
                              className="ml-2.5 flex h-full min-w-0 flex-1 items-center bg-transparent text-left max-md:ml-1"
                              disabled={id == null}
                              onClick={() => {
                                if (id != null && id !== selectedChannelId) {
                                  handleSelectChannel(id);
                                }
                              }}
                              type="button"
                            >
                              <span className="mr-2.5 flex h-[30px] w-[30px] shrink-0 items-center justify-center text-white/80 [&>span]:h-[30px] [&>span]:w-[30px]">
                                {getChannelIcon(channel)}
                              </span>
                              <span className="min-w-0 flex-1 truncate text-[15px] text-white max-md:hidden">
                                {getChannelName(channel)}
                              </span>
                            </button>
                            <ChevronRight
                              aria-hidden
                              className={cn(
                                "mr-2.5 h-4 w-4 shrink-0 text-white transition-opacity max-md:hidden",
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
          </aside>

          <div className="relative min-h-0 bg-osu-b4">
            {selectedChannel == null ? (
              <ChatNoChannel title="no channel selected" />
            ) : !selectedChannelJoined ? (
              <ChatNoChannel title={getChannelName(selectedChannel)}>
                <div className="grid gap-2">
                  {getChannelDescription(selectedChannel).length > 0 ? (
                    <div>{getChannelDescription(selectedChannel)}</div>
                  ) : null}
                  <div>you haven&apos;t joined this channel yet</div>
                  <div className="mt-1 flex justify-center">
                    <button
                      className={chatPageActionButtonClassName}
                      disabled={joining}
                      onClick={handleJoinSelectedChannel}
                      type="button"
                    >
                      {joining ? <Loader2 className="h-4 w-4 animate-spin" /> : null}
                      <span>join</span>
                    </button>
                  </div>
                </div>
              </ChatNoChannel>
            ) : (
              <ChatConversationFrame
                canMessage={selectedChannelCanMessage}
                canMessageError={selectedChannelCanMessageError}
                channel={selectedChannel}
                composer={(
                  <ChatCommandComposer
                    buttonLabel={chatReady ? "send" : "disconnected"}
                    buttonVariant="page"
                    currentUser={currentUser}
                    disabled={!selectedChannelCanMessage || !chatReady || sending}
                    key={selectedChannelId}
                    maxLength={getChannelMessageLengthLimit(selectedChannel)}
                    name="textbox"
                    onSubmit={handleSubmit}
                    placeholder={selectedChannelCanMessage ? "type your message" : "sending disabled"}
                    sending={sending}
                    shouldSubmitOnEnter={(event) => (
                      event.key === "Enter" && !(event.shiftKey && selectedChannel.type === "ANNOUNCE")
                    )}
                    usersById={usersById}
                  />
                )}
                currentUser={currentUser}
                currentUserId={currentUserId}
                error={error}
                loading={loadingMessages}
                messages={selectedMessages}
                usersById={usersById}
              />
            )}
          </div>
        </div>
      </PageWrapper>
    </section>
  );
}

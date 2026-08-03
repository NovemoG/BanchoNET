"use client";

import {
  createContext,
  type ReactNode,
  useContext,
  useEffect,
  useMemo,
  useState,
} from "react";
import { useSession } from "@/components/auth/session-provider";
import type {
  ChatMessagePreview,
  NotificationBundle,
  NotificationCounters,
  NotificationPayload,
  NotificationStack,
  RealtimeConnectionStatus,
  RealtimeSocketEvent,
} from "@/lib/realtime/types";

type RealtimeContextValue = {
  chatMessagePreviews: ChatMessagePreview[];
  connectionStatus: RealtimeConnectionStatus;
  lastEvent: RealtimeSocketEvent | null;
  notificationBundle: NotificationBundle | null;
  notificationCounts: NotificationCounters;
  notificationsReady: boolean;
};

type SseMessage = {
  data: string;
  event: string;
};

const RealtimeContext = createContext<RealtimeContextValue | null>(null);
const initialNotificationCounts: NotificationCounters = {
  chat: 0,
  notifications: 0,
};
// The bridge is a long lived SSE connection, not a poll, so a retry only happens when it drops.
// A flat 2.5s retry against a persistent failure (an unauthorised token, notify being down) turned
// into a request every few seconds forever, so back off up to a minute instead. Reset on success.
const reconnectBaseDelayMs = 2_500;
const reconnectMaxDelayMs = 60_000;

function getReconnectDelayMs(attempt: number) {
  return Math.min(reconnectBaseDelayMs * 2 ** Math.max(0, attempt - 1), reconnectMaxDelayMs);
}

function getObjectType(value: unknown) {
  if (typeof value !== "object" || value == null) {
    return null;
  }

  const objectType = "object_type" in value
    ? (value as { object_type?: unknown }).object_type
    : (value as { objectType?: unknown }).objectType;

  return typeof objectType === "string" ? objectType : null;
}

function getObjectId(value: unknown) {
  if (typeof value !== "object" || value == null) {
    return null;
  }

  const objectId = "object_id" in value
    ? (value as { object_id?: unknown }).object_id
    : (value as { objectId?: unknown }).objectId;

  return typeof objectId === "number" && Number.isFinite(objectId) ? objectId : null;
}

function getNotificationCategory(value: unknown) {
  if (typeof value !== "object" || value == null) {
    return null;
  }

  const category = "category" in value
    ? (value as { category?: unknown }).category
    : (value as { name?: unknown }).name;

  return typeof category === "string" && category.length > 0 ? category : null;
}

function getNotificationId(value: unknown) {
  if (typeof value !== "object" || value == null || !("id" in value)) {
    return null;
  }

  const id = (value as { id?: unknown }).id;

  return typeof id === "number" && Number.isFinite(id) ? id : null;
}

function getTotal(value: unknown) {
  if (typeof value !== "object" || value == null || !("total" in value)) {
    return 0;
  }

  const total = (value as { total?: unknown }).total;

  return typeof total === "number" && Number.isFinite(total) ? total : 0;
}

function getReadCount(value: unknown) {
  if (typeof value !== "object" || value == null) {
    return 0;
  }

  const readCount = "read_count" in value
    ? (value as { read_count?: unknown }).read_count
    : (value as { readCount?: unknown }).readCount;

  return typeof readCount === "number" && Number.isFinite(readCount) ? readCount : 0;
}

function getNotifications(value: unknown) {
  if (typeof value !== "object" || value == null || !("notifications" in value)) {
    return [];
  }

  const notifications = (value as { notifications?: unknown }).notifications;

  return Array.isArray(notifications) ? notifications : [];
}

function getNotificationIdentityKey(value: unknown) {
  const id = getNotificationId(value);

  if (id != null) {
    return `id:${id}`;
  }

  return [
    getObjectType(value) ?? "unknown",
    getObjectId(value) ?? "unknown",
    getNotificationCategory(value) ?? "default",
  ].join(":");
}

function matchesNotificationIdentity(notification: NotificationPayload, identity: unknown) {
  const identityId = getNotificationId(identity);

  if (identityId != null) {
    return getNotificationId(notification) === identityId;
  }

  const objectType = getObjectType(identity);
  const objectId = getObjectId(identity);
  const category = getNotificationCategory(identity);

  return (objectType == null || getObjectType(notification) === objectType) &&
    (objectId == null || getObjectId(notification) === objectId) &&
    (category == null || getNotificationCategory(notification) === category);
}

function stackMatchesIdentity(stack: NotificationStack, identity: unknown) {
  const objectType = getObjectType(identity);
  const objectId = getObjectId(identity);
  const category = getNotificationCategory(identity);

  return (objectType == null || getObjectType(stack) === objectType) &&
    (objectId == null || getObjectId(stack) === objectId) &&
    (category == null || getNotificationCategory(stack) === category);
}

function stackMatchesNotification(stack: NotificationStack, notification: NotificationPayload) {
  return getObjectType(stack) === getObjectType(notification) &&
    getObjectId(stack) === getObjectId(notification) &&
    getNotificationCategory(stack) === getNotificationCategory(notification);
}

function upsertBundleNotification(
  bundle: NotificationBundle | null,
  notification: NotificationPayload,
) {
  if (!isUnreadNotification(notification)) {
    return bundle;
  }

  const notifications = bundle?.notifications ?? [];
  const notificationKey = getNotificationIdentityKey(notification);
  const hasNotification = notifications.some((item) => getNotificationIdentityKey(item) === notificationKey);
  const objectId = getObjectId(notification);
  const objectType = getObjectType(notification);
  const category = getNotificationCategory(notification) ?? notification.name ?? "default";
  const next: NotificationBundle = {
    ...(bundle ?? {}),
    notifications: hasNotification
      ? notifications.map((item) => getNotificationIdentityKey(item) === notificationKey ? { ...item, ...notification } : item)
      : [notification, ...notifications],
  };

  if (objectId != null && objectType != null) {
    const stacks = bundle?.stacks ?? [];
    const stack = stacks.find((item) => stackMatchesNotification(item, notification));

    next.stacks = stack == null
      ? [
          {
            category,
            cursor: null,
            object_id: objectId,
            object_type: objectType,
            total: 1,
          },
          ...stacks,
        ]
      : stacks.map((item) => stackMatchesNotification(item, notification)
        ? {
            ...item,
            total: hasNotification ? getTotal(item) : getTotal(item) + 1,
          }
        : item);

    const types = bundle?.types ?? [];
    const type = types.find((item) => item.name === objectType);

    next.types = type == null
      ? [{ cursor: null, name: objectType, total: 1 }, ...types]
      : types.map((item) => item.name === objectType
        ? { ...item, total: hasNotification ? getTotal(item) : getTotal(item) + 1 }
        : item);
  }

  if (typeof next.unread_count === "number" && !hasNotification) {
    next.unread_count += 1;
  }

  return next;
}

function removeBundleNotifications(bundle: NotificationBundle | null, data: unknown) {
  if (bundle == null) {
    return bundle;
  }

  const identities = getNotifications(data);

  if (identities.length === 0) {
    return bundle;
  }

  let notifications = bundle.notifications ?? [];
  let stacks = bundle.stacks ?? [];
  let types = bundle.types ?? [];
  let removedCount = 0;

  for (const identity of identities) {
    const hasNotificationId = getNotificationId(identity) != null;
    const matchingNotifications = notifications.filter((item) => matchesNotificationIdentity(item, identity));
    const decrement = Math.max(1, matchingNotifications.length);

    notifications = notifications.filter((item) => !matchesNotificationIdentity(item, identity));
    removedCount += matchingNotifications.length;

    stacks = stacks.flatMap((stack) => {
      if (!stackMatchesIdentity(stack, identity)) {
        return [stack];
      }

      if (!hasNotificationId) {
        removedCount += getTotal(stack);
        return [];
      }

      const total = Math.max(0, getTotal(stack) - decrement);

      return total > 0 ? [{ ...stack, total }] : [];
    });

    const objectType = getObjectType(identity);

    if (objectType != null) {
      types = types
        .map((type) => type.name === objectType
          ? { ...type, total: Math.max(0, getTotal(type) - decrement) }
          : type)
        .filter((type) => type.total == null || type.total > 0);
    }
  }

  return {
    ...bundle,
    notifications,
    stacks,
    types,
    unread_count: typeof bundle.unread_count === "number"
      ? Math.max(0, bundle.unread_count - removedCount)
      : bundle.unread_count,
  };
}

function applySocketEventToBundle(
  bundle: NotificationBundle | null,
  message: RealtimeSocketEvent,
) {
  if (message.event === "new" && typeof message.data === "object" && message.data != null) {
    return upsertBundleNotification(bundle, message.data as NotificationPayload);
  }

  if (message.event === "read" || message.event === "delete") {
    return removeBundleNotifications(bundle, message.data);
  }

  return bundle;
}

function splitNotificationCount(current: NotificationCounters, objectType: string | null, delta: number) {
  if (delta === 0) {
    return current;
  }

  if (objectType === "channel") {
    return {
      ...current,
      chat: Math.max(0, current.chat + delta),
    };
  }

  return {
    ...current,
    notifications: Math.max(0, current.notifications + delta),
  };
}

function getBundleNotificationCounts(bundle: NotificationBundle): NotificationCounters {
  if (Array.isArray(bundle.stacks) && bundle.stacks.length > 0) {
    return bundle.stacks.reduce<NotificationCounters>((counts, stack) => {
      return splitNotificationCount(counts, getObjectType(stack), getTotal(stack));
    }, initialNotificationCounts);
  }

  if (Array.isArray(bundle.types) && bundle.types.length > 0) {
    return bundle.types.reduce<NotificationCounters>((counts, type) => {
      const name = typeof type.name === "string" ? type.name : null;

      return splitNotificationCount(counts, name, getTotal(type));
    }, initialNotificationCounts);
  }

  if (typeof bundle.unread_count === "number" && Number.isFinite(bundle.unread_count)) {
    return {
      chat: 0,
      notifications: bundle.unread_count,
    };
  }

  return initialNotificationCounts;
}

function isUnreadNotification(value: NotificationPayload) {
  const isRead = typeof value.is_read === "boolean" ? value.is_read : value.isRead;

  return isRead !== true;
}

function getIncomingChatMessageCount(data: unknown, currentUserId: number | null) {
  if (typeof data !== "object" || data == null || !("messages" in data)) {
    return 0;
  }

  const messages = (data as { messages?: unknown }).messages;

  if (!Array.isArray(messages)) {
    return 0;
  }

  return messages.filter((message) => {
    if (currentUserId == null || typeof message !== "object" || message == null) {
      return true;
    }

    const senderId = "sender_id" in message
      ? (message as { sender_id?: unknown }).sender_id
      : (message as { senderId?: unknown }).senderId;

    return senderId !== currentUserId;
  }).length;
}

function getMessageNumber(value: unknown, snakeKey: string, camelKey: string) {
  if (typeof value !== "object" || value == null) {
    return null;
  }

  const raw = snakeKey in value
    ? (value as Record<string, unknown>)[snakeKey]
    : (value as Record<string, unknown>)[camelKey];

  return typeof raw === "number" && Number.isFinite(raw) ? raw : null;
}

function getMessageText(value: unknown, key: string) {
  if (typeof value !== "object" || value == null || !(key in value)) {
    return null;
  }

  const raw = (value as Record<string, unknown>)[key];

  return typeof raw === "string" && raw.trim().length > 0 ? raw : null;
}

function getIncomingChatMessagePreviews(data: unknown, currentUserId: number | null) {
  if (typeof data !== "object" || data == null || !("messages" in data)) {
    return [];
  }

  const messages = (data as { messages?: unknown }).messages;

  if (!Array.isArray(messages)) {
    return [];
  }

  return messages.flatMap<ChatMessagePreview>((message) => {
    const senderId = getMessageNumber(message, "sender_id", "senderId");

    if (currentUserId != null && senderId === currentUserId) {
      return [];
    }

    const channelId = getMessageNumber(message, "channel_id", "channelId");
    const content = getMessageText(message, "content");

    if (channelId == null || content == null) {
      return [];
    }

    const sender = typeof message === "object" && message != null && "sender" in message
      ? (message as { sender?: unknown }).sender
      : null;

    return [{
      avatarUrl: getMessageText(sender, "avatar_url") ?? getMessageText(sender, "avatarUrl"),
      channelId,
      content,
      messageId: getMessageNumber(message, "message_id", "messageId"),
      senderId,
      timestamp: getMessageText(message, "timestamp"),
      username: getMessageText(sender, "username"),
    }];
  });
}

function applyChatMessagePreviews(
  current: ChatMessagePreview[],
  message: RealtimeSocketEvent,
  currentUserId: number | null,
) {
  if (message.event !== "chat.message.new") {
    return current;
  }

  const incoming = getIncomingChatMessagePreviews(message.data, currentUserId);

  if (incoming.length === 0) {
    return current;
  }

  const byChannel = new Map<number, ChatMessagePreview>();

  for (const preview of current) {
    byChannel.set(preview.channelId, preview);
  }

  for (const preview of incoming) {
    byChannel.set(preview.channelId, preview);
  }

  return [...byChannel.values()]
    .sort((left, right) => (right.messageId ?? 0) - (left.messageId ?? 0))
    .slice(0, 8);
}

function removeReadChatMessagePreviews(current: ChatMessagePreview[], data: unknown) {
  const readChannelIds = new Set(
    getNotifications(data)
      .filter((identity) => getObjectType(identity) === "channel")
      .map(getObjectId)
      .filter((id): id is number => id != null),
  );

  if (readChannelIds.size === 0) {
    return current;
  }

  return current.filter((preview) => !readChannelIds.has(preview.channelId));
}

function applySocketEventToCounts(
  counts: NotificationCounters,
  message: RealtimeSocketEvent,
  currentUserId: number | null,
) {
  if (message.event === "new" && typeof message.data === "object" && message.data != null) {
    const notification = message.data as NotificationPayload;

    if (isUnreadNotification(notification)) {
      return splitNotificationCount(counts, getObjectType(notification), 1);
    }
  }

  if (message.event === "read" || message.event === "delete") {
    const notifications = getNotifications(message.data);
    const readCount = getReadCount(message.data);

    if (notifications.length === 1) {
      return splitNotificationCount(counts, getObjectType(notifications[0]), -Math.max(1, readCount));
    }

    if (notifications.length > 1) {
      return notifications.reduce<NotificationCounters>((nextCounts, notification) => {
        return splitNotificationCount(nextCounts, getObjectType(notification), -1);
      }, counts);
    }
  }

  if (message.event === "chat.message.new") {
    return splitNotificationCount(counts, "channel", getIncomingChatMessageCount(message.data, currentUserId));
  }

  return counts;
}

function parseSseMessage(rawMessage: string): SseMessage | null {
  const lines = rawMessage.split(/\r?\n/);
  let event = "message";
  const data: string[] = [];

  for (const line of lines) {
    if (line.startsWith(":")) {
      continue;
    }

    if (line.startsWith("event:")) {
      event = line.slice("event:".length).trim();
      continue;
    }

    if (line.startsWith("data:")) {
      data.push(line.slice("data:".length).trimStart());
    }
  }

  if (data.length === 0) {
    return null;
  }

  return {
    data: data.join("\n"),
    event,
  };
}

function dispatchBrowserRealtimeEvent(message: RealtimeSocketEvent) {
  window.dispatchEvent(new CustomEvent("osunovemo:realtime", { detail: message }));

  if (message.event.startsWith("chat.")) {
    window.dispatchEvent(new CustomEvent("osunovemo:chat", { detail: message }));
    return;
  }

  window.dispatchEvent(new CustomEvent("osunovemo:notification", { detail: message }));
}

async function readEventStream(
  body: ReadableStream<Uint8Array>,
  onMessage: (message: SseMessage) => void,
) {
  const reader = body.getReader();
  const decoder = new TextDecoder();
  let buffer = "";

  try {
    while (true) {
      const { done, value } = await reader.read();

      if (done) {
        break;
      }

      buffer += decoder.decode(value, { stream: true });
      const messages = buffer.split(/\r?\n\r?\n/);
      buffer = messages.pop() ?? "";

      for (const message of messages) {
        const parsed = parseSseMessage(message);

        if (parsed != null) {
          onMessage(parsed);
        }
      }
    }

    buffer += decoder.decode();

    if (buffer.trim().length > 0) {
      const parsed = parseSseMessage(buffer);

      if (parsed != null) {
        onMessage(parsed);
      }
    }
  } finally {
    reader.releaseLock();
  }
}

export function RealtimeProvider({ children }: { children: ReactNode }) {
  const { data: session, status: sessionStatus } = useSession();
  const [connectionStatus, setConnectionStatus] =
    useState<RealtimeConnectionStatus>("idle");
  const [chatMessagePreviews, setChatMessagePreviews] = useState<ChatMessagePreview[]>([]);
  const [lastEvent, setLastEvent] = useState<RealtimeSocketEvent | null>(null);
  const [notificationBundle, setNotificationBundle] =
    useState<NotificationBundle | null>(null);
  const [notificationCounts, setNotificationCounts] =
    useState<NotificationCounters>(initialNotificationCounts);
  const userId = session?.user.id ?? null;
  const isAuthenticated = sessionStatus === "authenticated" && userId != null;

  useEffect(() => {
    if (!isAuthenticated) {
      return;
    }

    const abortController = new AbortController();
    let active = true;
    let reconnectTimer: number | null = null;
    let reconnectAttempt = 0;

    const clearReconnectTimer = () => {
      if (reconnectTimer != null) {
        window.clearTimeout(reconnectTimer);
        reconnectTimer = null;
      }
    };

    const connect = async (isReconnect = false) => {
      clearReconnectTimer();
      setConnectionStatus(isReconnect ? "reconnecting" : "connecting");

      try {
        const response = await fetch("/api/realtime/notifications/stream", {
          cache: "no-store",
          signal: abortController.signal,
        });

        if (response.status === 401) {
          setConnectionStatus("disconnected");
          return;
        }

        if (!response.ok || response.body == null) {
          throw new Error(`Realtime stream returned ${response.status}.`);
        }

        await readEventStream(response.body, (message) => {
          if (!active) {
            return;
          }

          const data = JSON.parse(message.data) as unknown;

          if (message.event === "realtime.status") {
            const statusValue =
              typeof data === "object" &&
              data != null &&
              "status" in data &&
              typeof (data as { status?: unknown }).status === "string"
                ? (data as { status: RealtimeConnectionStatus }).status
                : "connected";

            // Reset only once the realtime socket is actually up. The SSE response itself always
            // returns 200 and starts streaming, so resetting on response.ok meant a bridge that
            // opened and immediately died kept retrying at the base delay forever.
            if (statusValue === "connected") {
              reconnectAttempt = 0;
            }

            setConnectionStatus(statusValue);
            return;
          }

          if (message.event === "notification.bundle") {
            const bundle = data as NotificationBundle;

            setChatMessagePreviews([]);
            setNotificationBundle(bundle);
            setNotificationCounts(getBundleNotificationCounts(bundle));
            return;
          }

          if (message.event === "socket.message") {
            const socketEvent = data as RealtimeSocketEvent;
            setLastEvent(socketEvent);
            setNotificationBundle((bundle) => applySocketEventToBundle(bundle, socketEvent));
            setChatMessagePreviews((previews) => {
              if (socketEvent.event === "read" || socketEvent.event === "delete") {
                return removeReadChatMessagePreviews(previews, socketEvent.data);
              }

              return applyChatMessagePreviews(previews, socketEvent, userId);
            });
            setNotificationCounts((counts) => applySocketEventToCounts(counts, socketEvent, userId));
            dispatchBrowserRealtimeEvent(socketEvent);
          }
        });
      } catch {
        if (!active || abortController.signal.aborted) {
          return;
        }
      }

      if (active && !abortController.signal.aborted) {
        reconnectAttempt += 1;
        setConnectionStatus("reconnecting");
        reconnectTimer = window.setTimeout(() => {
          void connect(true);
        }, getReconnectDelayMs(reconnectAttempt));
      }
    };

    void connect();

    return () => {
      active = false;
      clearReconnectTimer();
      abortController.abort();
    };
  }, [isAuthenticated, userId]);

  const value = useMemo<RealtimeContextValue>(
    () => ({
      chatMessagePreviews: isAuthenticated ? chatMessagePreviews : [],
      connectionStatus: isAuthenticated ? connectionStatus : "idle",
      lastEvent: isAuthenticated ? lastEvent : null,
      notificationBundle: isAuthenticated ? notificationBundle : null,
      notificationCounts: isAuthenticated ? notificationCounts : initialNotificationCounts,
      notificationsReady: isAuthenticated && notificationBundle != null,
    }),
    [chatMessagePreviews, connectionStatus, isAuthenticated, lastEvent, notificationBundle, notificationCounts],
  );

  return <RealtimeContext.Provider value={value}>{children}</RealtimeContext.Provider>;
}

export function useRealtime() {
  const value = useContext(RealtimeContext);

  if (value == null) {
    throw new Error("useRealtime must be used within RealtimeProvider.");
  }

  return value;
}

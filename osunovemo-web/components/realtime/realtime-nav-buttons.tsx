"use client";

import { forwardRef, useMemo, useState } from "react";
import { Bell, Check, Hash, Inbox, MessageCircle } from "lucide-react";
import {
  Popover,
  PopoverContent,
  PopoverTrigger,
} from "@/components/ui/popover";
import { Skeleton } from "@/components/ui/skeleton";
import {
  Tooltip,
  TooltipContent,
  TooltipTrigger,
} from "@/components/ui/tooltip";
import { useRealtime } from "@/components/realtime/realtime-provider";
import type {
  ChatMessagePreview,
  NotificationBundle,
  NotificationPayload,
  NotificationStack,
  NotificationTypeSummary,
} from "@/lib/realtime/types";
import { cn } from "@/lib/utils";

type RealtimeNavButtonsProps = {
  variant: "desktop" | "mobile";
};

type PanelKind = "chat" | "notifications";
type NotificationListItem =
  | { item: ChatMessagePreview; kind: "chat-preview" }
  | { item: NotificationPayload; kind: "notification" }
  | { item: NotificationStack; kind: "stack" };

const numberFormatter = new Intl.NumberFormat();

function formatCount(count: number, ready: boolean) {
  return ready ? numberFormatter.format(count) : "...";
}

function humanize(value: string | null | undefined) {
  return value?.replace(/_/g, " ").trim() ?? "";
}

function firstText(...values: Array<string | null | undefined>) {
  return values.find((value) => value != null && value.trim().length > 0) ?? null;
}

function getObjectType(value: NotificationPayload | NotificationStack) {
  return value.object_type ?? value.objectType ?? null;
}

function getObjectId(value: NotificationPayload | NotificationStack) {
  return value.object_id ?? value.objectId ?? null;
}

function isChatItem(value: NotificationPayload | NotificationStack) {
  return getObjectType(value) === "channel";
}

function getStackKey(stack: NotificationStack) {
  return [
    getObjectType(stack) ?? "notification",
    getObjectId(stack) ?? "unknown",
    stack.category ?? "default",
  ].join(":");
}

function getNotificationKey(notification: NotificationPayload) {
  return notification.id ?? [
    getObjectType(notification) ?? "notification",
    getObjectId(notification) ?? "unknown",
    notification.category ?? notification.name ?? "default",
  ].join(":");
}

function getChatPreviewKey(preview: ChatMessagePreview) {
  return [
    "chat-preview",
    preview.channelId,
    preview.messageId ?? preview.timestamp ?? preview.content,
  ].join(":");
}

function isUnreadNotification(notification: NotificationPayload) {
  const isRead = notification.is_read ?? notification.isRead;

  return isRead !== true;
}

function getMatchingNotification(stack: NotificationStack, bundle: NotificationBundle | null) {
  const objectType = getObjectType(stack);
  const objectId = getObjectId(stack);
  const category = stack.category;

  return bundle?.notifications?.find((notification) => {
    const notificationCategory = notification.category ?? notification.name;

    return getObjectType(notification) === objectType &&
      getObjectId(notification) === objectId &&
      (category == null || notificationCategory === category);
  });
}

function getPreviewChannelId(preview: ChatMessagePreview) {
  return preview.channelId;
}

function getNotificationTitle(notification: NotificationPayload, isChat: boolean) {
  const details = notification.details ?? {};

  if (isChat) {
    return firstText(details.name, details.title) ?? "chat";
  }

  return firstText(
    details.title,
    details.content,
    details.name,
    notification.name,
    notification.category,
    getObjectType(notification),
  ) ?? "notification";
}

function getNotificationDescription(notification: NotificationPayload, isChat: boolean) {
  const details = notification.details ?? {};

  if (isChat) {
    return firstText(details.username, humanize(details.type)) ?? "unread message";
  }

  return firstText(
    details.username,
    details.version,
    details.content,
    humanize(notification.category),
    humanize(notification.name),
    humanize(getObjectType(notification)),
  ) ?? "unread";
}

function getNotificationTime(notification: NotificationPayload | null) {
  if (notification?.created_at == null) {
    return null;
  }

  const date = new Date(notification.created_at);

  if (Number.isNaN(date.valueOf())) {
    return null;
  }

  const seconds = Math.max(0, Math.floor((Date.now() - date.getTime()) / 1000));

  if (seconds < 45) {
    return "a few seconds ago";
  }

  if (seconds < 90) {
    return "a minute ago";
  }

  const minutes = Math.floor(seconds / 60);

  if (minutes < 60) {
    return `${minutes} minutes ago`;
  }

  const hours = Math.floor(minutes / 60);

  if (hours < 2) {
    return "an hour ago";
  }

  if (hours < 24) {
    return `${hours} hours ago`;
  }

  if (hours < 48) {
    return "yesterday";
  }

  return date.toLocaleDateString([], {
    day: "numeric",
    month: "short",
  });
}

function getStackTitle(stack: NotificationStack, bundle: NotificationBundle | null, isChat: boolean) {
  const notification = getMatchingNotification(stack, bundle);

  if (notification != null) {
    return getNotificationTitle(notification, isChat);
  }

  if (isChat) {
    return "chat";
  }

  return firstText(humanize(stack.category), humanize(getObjectType(stack))) ?? "notification";
}

function getStackDescription(stack: NotificationStack, bundle: NotificationBundle | null, isChat: boolean) {
  const notification = getMatchingNotification(stack, bundle);

  if (notification != null) {
    return getNotificationDescription(notification, isChat);
  }

  return isChat ? "unread messages" : "unread notifications";
}

function getPanelItems(
  bundle: NotificationBundle | null,
  kind: PanelKind,
  chatMessagePreviews: ChatMessagePreview[],
): NotificationListItem[] {
  const isChat = kind === "chat";
  const stacks = bundle?.stacks
    ?.filter((stack) => isChatItem(stack) === isChat && (stack.total ?? 0) > 0)
    ?? [];

  if (isChat) {
    const previewsByChannel = new Map(chatMessagePreviews.map((preview) => [preview.channelId, preview]));
    const representedChannelIds = new Set<number>();
    const items: NotificationListItem[] = [];

    for (const stack of stacks) {
      const channelId = getObjectId(stack);
      const preview = typeof channelId === "number" ? previewsByChannel.get(channelId) : null;
      const notification = getMatchingNotification(stack, bundle);

      if (typeof channelId === "number") {
        representedChannelIds.add(channelId);
      }

      items.push(
        notification == null && preview != null
          ? { item: preview, kind: "chat-preview" }
          : { item: stack, kind: "stack" },
      );
    }

    for (const notification of bundle?.notifications ?? []) {
      if (!isChatItem(notification) || !isUnreadNotification(notification)) {
        continue;
      }

      const channelId = getObjectId(notification);

      if (typeof channelId === "number" && representedChannelIds.has(channelId)) {
        continue;
      }

      if (typeof channelId === "number") {
        representedChannelIds.add(channelId);
      }

      items.push({ item: notification, kind: "notification" });
    }

    for (const preview of chatMessagePreviews) {
      if (representedChannelIds.has(getPreviewChannelId(preview))) {
        continue;
      }

      representedChannelIds.add(getPreviewChannelId(preview));
      items.push({ item: preview, kind: "chat-preview" });
    }

    return items.slice(0, 8);
  }

  if (stacks.length > 0) {
    return stacks.map((item) => ({ item, kind: "stack" as const })).slice(0, 8);
  }

  return bundle?.notifications
    ?.filter((notification) => isChatItem(notification) === isChat && isUnreadNotification(notification))
    .map((item) => ({ item, kind: "notification" as const }))
    .slice(0, 8) ?? [];
}

function getFilterTypes(bundle: NotificationBundle | null) {
  return bundle?.types
    ?.filter((type): type is NotificationTypeSummary & { name: string } => {
      return typeof type.name === "string" && type.name !== "channel" && (type.total ?? 0) > 0;
    }) ?? [];
}

function itemMatchesFilter(item: NotificationListItem, filter: string | null) {
  if (filter == null) {
    return true;
  }

  if (item.kind === "chat-preview") {
    return filter === "channel";
  }

  return getObjectType(item.item) === filter;
}

function getPanelHistoryHref(kind: PanelKind) {
  return kind === "chat" ? "/community/chat" : "/home/search";
}

function getPanelHistoryText(kind: PanelKind) {
  return kind === "chat" ? "see chat" : "see all";
}

function getItemHref(item: NotificationPayload | NotificationStack, panelKind: PanelKind) {
  if (panelKind === "chat") {
    const channelId = getObjectId(item);

    return typeof channelId === "number" ? `/community/chat/${channelId}` : "/community/chat";
  }

  return "/community/chat";
}

function getChatPreviewHref(preview: ChatMessagePreview) {
  return `/community/chat/${preview.channelId}`;
}

function getChatNotificationUsername(notification: NotificationPayload | null) {
  const details = notification?.details ?? {};

  return firstText(details.username, details.name) ?? "someone";
}

function getChatNotificationMessage(notification: NotificationPayload | null) {
  const details = notification?.details ?? {};

  return firstText(details.title, details.content) ?? "new message";
}

function getChatNotificationCoverUrl(notification: NotificationPayload | null) {
  const details = notification?.details ?? {};
  const fallbackUserId = notification?.source_user_id ?? notification?.sourceUserId;
  const coverUrl = firstText(details.cover_url, details.coverUrl);

  if (coverUrl != null) {
    return coverUrl;
  }

  return typeof fallbackUserId === "number" ? `https://a.ppy.sh/${fallbackUserId}` : null;
}

const RealtimeIconButton = forwardRef<HTMLButtonElement, {
  active: boolean;
  children: React.ReactNode;
  count: number;
  label: string;
  ready: boolean;
  variant: RealtimeNavButtonsProps["variant"];
} & React.ComponentPropsWithoutRef<"button">>(function RealtimeIconButton({
  active,
  children,
  className,
  count,
  label,
  ready,
  variant,
  ...props
}, ref) {
  const hasUnread = ready && count > 0;
  const countText = formatCount(count, ready);

  return (
    <button
      ref={ref}
      aria-label={label}
      aria-pressed={active}
      className={cn(
        "relative inline-flex items-center justify-center text-osu-c1 transition-colors hover:text-white aria-pressed:text-white",
        hasUnread && "text-white [text-shadow:0_0_10px_rgba(255,255,255,0.5)]",
        variant === "desktop"
          ? "min-h-10 rounded-full px-2.5 py-[7px] hover:bg-white/20 aria-pressed:bg-white/20"
          : "h-10 w-10 hover:text-white before:absolute before:bottom-[-2px] before:left-1/4 before:right-1/4 before:h-1 before:rounded-full before:bg-osu-h1 before:opacity-0 before:content-[''] hover:before:opacity-100 aria-pressed:before:opacity-100",
        className,
      )}
      title={label}
      type="button"
      {...props}
    >
      <span
        className={cn(
          "flex items-center justify-center",
          variant === "mobile" && "h-full flex-col",
        )}
      >
        {children}
        <span
          className={cn(
            "font-medium leading-none",
            variant === "desktop"
              ? "ml-[5px] min-w-[1ch] text-sm"
              : "absolute right-0.5 bottom-0.5 rounded-full bg-osu-d3 px-[5px] py-0.5 text-[10px] text-osu-c1",
          )}
        >
          {countText}
        </span>
      </span>
    </button>
  );
});

function PanelItem({
  bundle,
  item,
  itemKind,
  onNavigate,
  panelKind,
}: {
  bundle: NotificationBundle | null;
  item: NotificationPayload | NotificationStack;
  itemKind: NotificationListItem["kind"];
  onNavigate: () => void;
  panelKind: PanelKind;
}) {
  const isChat = panelKind === "chat";
  const notification = itemKind === "stack"
    ? getMatchingNotification(item as NotificationStack, bundle) ?? null
    : item as NotificationPayload;
  const title = itemKind === "stack"
    ? getStackTitle(item as NotificationStack, bundle, isChat)
    : getNotificationTitle(item as NotificationPayload, isChat);
  const description = itemKind === "stack"
    ? getStackDescription(item as NotificationStack, bundle, isChat)
    : getNotificationDescription(item as NotificationPayload, isChat);
  const category = isChat ? "chat" : humanize(notification?.category ?? notification?.name ?? getObjectType(item));
  const count = itemKind === "stack" ? (item as NotificationStack).total ?? 0 : 1;
  const time = getNotificationTime(notification);

  return (
    <a
      className="relative flex min-h-12 w-full overflow-hidden rounded-lg bg-osu-b3 text-osu-c1 no-underline transition-colors hover:bg-osu-b1"
      href={getItemHref(item, panelKind)}
      onClick={onNavigate}
    >
      <span className="flex w-[60px] shrink-0 items-center justify-center bg-osu-b2 text-osu-c1">
        {isChat ? <Hash className="h-4 w-4" /> : <Bell className="h-4 w-4" />}
      </span>
      <span className="relative flex min-w-0 flex-1 flex-col justify-center px-3 py-1.5">
        <span className="truncate text-[11px] font-bold uppercase leading-4 text-osu-c1">
          {category}
        </span>
        <span className="break-words text-[13px] font-semibold leading-5 text-white">
          {title}
        </span>
        <span className="truncate text-xs leading-4 text-osu-f1">
          {count > 1 ? `${numberFormatter.format(count)} unread - ` : null}
          {description}
          {time == null ? null : ` - ${time}`}
        </span>
      </span>
      <span className="absolute inset-y-0 left-0 w-2 rounded-l-lg shadow-[inset_2px_0_hsl(var(--hsl-h1))]" />
    </a>
  );
}

function ChatMessageCard({
  bundle,
  item,
  itemKind,
  onNavigate,
}: {
  bundle: NotificationBundle | null;
  item: ChatMessagePreview | NotificationPayload | NotificationStack;
  itemKind: NotificationListItem["kind"];
  onNavigate: () => void;
}) {
  const preview = itemKind === "chat-preview" ? item as ChatMessagePreview : null;
  const notification = itemKind === "stack"
    ? getMatchingNotification(item as NotificationStack, bundle) ?? null
    : itemKind === "notification"
      ? item as NotificationPayload
      : null;
  const username = preview?.username ?? getChatNotificationUsername(notification);
  const message = preview?.content ?? getChatNotificationMessage(notification);
  const coverUrl = preview?.avatarUrl ?? getChatNotificationCoverUrl(notification);
  const time = preview?.timestamp == null
    ? getNotificationTime(notification) ?? "a few seconds ago"
    : getNotificationTime({ created_at: preview.timestamp });

  return (
    <a
      className="group relative flex min-h-16 w-full overflow-hidden rounded-md bg-osu-b3 text-osu-c1 no-underline transition-colors hover:bg-osu-b1"
      href={preview == null ? getItemHref(item as NotificationPayload | NotificationStack, "chat") : getChatPreviewHref(preview)}
      onClick={onNavigate}
    >
      <span
        className={cn(
          "relative flex w-16 shrink-0 overflow-hidden bg-osu-b2 bg-cover bg-center",
          coverUrl == null && "items-center justify-center text-osu-l2",
        )}
        style={coverUrl == null ? undefined : { backgroundImage: `url(${JSON.stringify(coverUrl)})` }}
      >
        {coverUrl == null ? <Hash className="h-5 w-5" /> : null}
        <span className="absolute inset-x-0 bottom-0 bg-gradient-to-t from-black/75 to-transparent px-1 pb-1 pt-5 text-center text-[11px] font-semibold leading-none text-white [text-shadow:0_1px_2px_rgba(0,0,0,0.75)]">
          {username}
        </span>
      </span>

      <span className="flex min-w-0 flex-1 flex-col justify-center px-3 py-2 pr-10">
        <span className="text-[11px] font-black uppercase leading-4 text-white">
          new message
        </span>
        <span className="truncate text-[13px] font-bold leading-5 text-white">
          {username} says &quot;{message}&quot;
        </span>
        <span className="truncate text-xs leading-4 text-osu-f1">
          {time ?? "a few seconds ago"}
        </span>
      </span>

      <span className="absolute top-1/2 right-3 -translate-y-1/2 text-osu-f1 transition-colors group-hover:text-white">
        <Check className="h-4 w-4" />
      </span>
    </a>
  );
}

function RealtimePanel({
  kind,
  onNavigate,
  variant,
}: {
  kind: PanelKind;
  onNavigate: () => void;
  variant: RealtimeNavButtonsProps["variant"];
}) {
  const {
    chatMessagePreviews,
    connectionStatus,
    notificationBundle,
    notificationCounts,
    notificationsReady,
  } = useRealtime();
  const [filter, setFilter] = useState<string | null>(null);
  const isChat = kind === "chat";
  const count = isChat ? notificationCounts.chat : notificationCounts.notifications;
  const filters = isChat ? [] : getFilterTypes(notificationBundle);
  const items = getPanelItems(notificationBundle, kind, chatMessagePreviews).filter((item) => itemMatchesFilter(item, filter));
  const fallbackVisible = notificationsReady && items.length === 0 && count > 0;

  return (
    <div className="bg-osu-b4 p-2.5 text-osu-c1 md:max-w-[400px] md:rounded-lg md:border md:border-osu-b3 md:p-[15px_10px] md:shadow-[0_12px_32px_rgba(0,0,0,0.32)]">
      <div
        className={cn(
          "grid gap-2.5 overflow-y-auto px-1",
          variant === "desktop" ? "max-h-[calc(100vh-120px)]" : "max-h-[calc(100svh-110px)]",
        )}
      >
        {!isChat && notificationsReady && filters.length > 0 ? (
          <div className="grid grid-cols-2 gap-[5px]">
            <button
              className={cn(
                "text-left text-sm text-osu-l2 transition-colors hover:text-osu-c1",
                filter == null && "text-osu-c1",
              )}
              onClick={() => setFilter(null)}
              type="button"
            >
              <span className="mr-[5px] rounded-full bg-osu-b2 px-2.5 py-0.5 text-xs text-osu-c2">
                {formatCount(count, true)}
              </span>
              all
            </button>
            {filters.map((type) => (
              <button
                key={type.name}
                className={cn(
                  "text-left text-sm text-osu-l2 transition-colors hover:text-osu-c1",
                  filter === type.name && "text-osu-c1",
                )}
                onClick={() => setFilter(type.name)}
                type="button"
              >
                <span className="mr-[5px] rounded-full bg-osu-b2 px-2.5 py-0.5 text-xs text-osu-c2">
                  {formatCount(type.total ?? 0, true)}
                </span>
                {humanize(type.name)}
              </button>
            ))}
          </div>
        ) : null}

        <div className="flex items-center gap-3">
          <a
            className="text-sm text-osu-l2 no-underline transition-colors hover:text-osu-c1"
            href={getPanelHistoryHref(kind)}
            onClick={onNavigate}
          >
            {getPanelHistoryText(kind)}
          </a>
          <span
            className={cn(
              "ml-auto h-2 w-2 rounded-full",
              connectionStatus === "connected" ? "bg-osu-green-2" : "bg-osu-orange-3",
            )}
            title={connectionStatus}
          />
        </div>

        {!notificationsReady ? (
          <>
            <Skeleton className="h-[52px] w-full" />
            <Skeleton className="h-[52px] w-full" />
            <Skeleton className="h-[52px] w-full" />
          </>
        ) : fallbackVisible ? (
          <a
            className="relative flex min-h-12 w-full overflow-hidden rounded-lg bg-osu-b3 text-osu-c1 no-underline transition-colors hover:bg-osu-b1"
            href={getPanelHistoryHref(kind)}
            onClick={onNavigate}
          >
            <span className="flex w-[60px] shrink-0 items-center justify-center bg-osu-b2 text-osu-c1">
              {isChat ? <Hash className="h-4 w-4" /> : <Bell className="h-4 w-4" />}
            </span>
            <span className="relative flex min-w-0 flex-1 flex-col justify-center px-3 py-1.5">
              <span className="truncate text-[11px] font-bold uppercase leading-4 text-osu-c1">
                {isChat ? "chat" : "notifications"}
              </span>
              <span className="text-[13px] font-semibold leading-5 text-white">
                {formatCount(count, true)} unread
              </span>
              <span className="truncate text-xs leading-4 text-osu-f1">
                open history to view details
              </span>
            </span>
            <span className="absolute inset-y-0 left-0 w-2 rounded-l-lg shadow-[inset_2px_0_hsl(var(--hsl-h1))]" />
          </a>
        ) : items.length === 0 ? (
          <p className="m-[10px_0_5px] whitespace-nowrap text-sm text-osu-c1">
            {isChat ? "no unread chat messages" : "all caught up"}
          </p>
        ) : (
          items.map(({ item, kind: itemKind }) => (
            isChat ? (
              <ChatMessageCard
                key={itemKind === "chat-preview"
                  ? getChatPreviewKey(item as ChatMessagePreview)
                  : itemKind === "stack"
                    ? getStackKey(item as NotificationStack)
                    : getNotificationKey(item as NotificationPayload)}
                bundle={notificationBundle}
                item={item}
                itemKind={itemKind}
                onNavigate={onNavigate}
              />
            ) : (
              <PanelItem
                key={itemKind === "stack" ? getStackKey(item as NotificationStack) : getNotificationKey(item as NotificationPayload)}
                bundle={notificationBundle}
                item={item}
                itemKind={itemKind}
                onNavigate={onNavigate}
                panelKind={kind}
              />
            )
          ))
        )}
      </div>
    </div>
  );
}

export function RealtimeNavButtons({ variant }: RealtimeNavButtonsProps) {
  const { notificationCounts, notificationsReady } = useRealtime();
  const [openPanel, setOpenPanel] = useState<PanelKind | null>(null);
  const buttonData = useMemo(
    () => [
      {
        count: notificationCounts.chat,
        icon: <MessageCircle className={variant === "desktop" ? "h-[18px] w-[18px]" : "h-[17px] w-[17px]"} />,
        kind: "chat" as const,
        label: "chat",
      },
      {
        count: notificationCounts.notifications,
        icon: variant === "desktop"
          ? <Bell className="h-[18px] w-[18px]" />
          : <Inbox className="h-[17px] w-[17px]" />,
        kind: "notifications" as const,
        label: "notifications",
      },
    ],
    [notificationCounts.chat, notificationCounts.notifications, variant],
  );

  return (
    <div
      className={cn(
        "flex items-center",
        variant === "desktop" ? "gap-0.5" : "shrink-0",
      )}
    >
      {buttonData.map((button) => (
        <Popover
          key={button.kind}
          onOpenChange={(open) => setOpenPanel(open ? button.kind : null)}
          open={openPanel === button.kind}
        >
          <Tooltip>
            <PopoverTrigger asChild>
              <TooltipTrigger asChild>
                <RealtimeIconButton
                  active={openPanel === button.kind}
                  count={button.count}
                  label={button.label}
                  ready={notificationsReady}
                  variant={variant}
                >
                  {button.icon}
                </RealtimeIconButton>
              </TooltipTrigger>
            </PopoverTrigger>
            <TooltipContent side="bottom">{button.label}</TooltipContent>
          </Tooltip>

          <PopoverContent
            align="center"
            className={cn(
              "border-0 bg-transparent p-0 shadow-none",
              variant === "desktop" ? "w-[min(400px,90vw)]" : "w-screen rounded-none",
            )}
            collisionPadding={12}
            sideOffset={variant === "desktop" ? 20 : 15}
          >
            <RealtimePanel
              kind={button.kind}
              onNavigate={() => setOpenPanel(null)}
              variant={variant}
            />
          </PopoverContent>
        </Popover>
      ))}
    </div>
  );
}

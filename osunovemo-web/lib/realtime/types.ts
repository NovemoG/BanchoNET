export type RealtimeConnectionStatus =
  | "connected"
  | "connecting"
  | "disconnected"
  | "idle"
  | "reconnecting";

export type NotificationBundle = {
  notification_endpoint?: string;
  notifications?: NotificationPayload[];
  stacks?: NotificationStack[];
  timestamp?: string;
  types?: NotificationTypeSummary[];
  unread_count?: number;
  [key: string]: unknown;
};

export type NotificationCounters = {
  chat: number;
  notifications: number;
};

export type ChatMessagePreview = {
  avatarUrl?: string | null;
  channelId: number;
  content: string;
  messageId?: number | null;
  senderId?: number | null;
  timestamp?: string | null;
  username?: string | null;
};

export type NotificationPayload = {
  category?: string;
  created_at?: string;
  details?: {
    cover_url?: string;
    coverUrl?: string;
    content?: string;
    name?: string;
    title?: string;
    title_unicode?: string;
    type?: string;
    username?: string;
    version?: string;
    [key: string]: unknown;
  };
  id?: number;
  is_read?: boolean;
  isRead?: boolean;
  name?: string;
  object_id?: number;
  objectId?: number;
  object_type?: string;
  objectType?: string;
  source_user_id?: number;
  sourceUserId?: number;
  [key: string]: unknown;
};

export type NotificationStack = {
  category?: string;
  cursor?: unknown;
  object_id?: number;
  objectId?: number;
  object_type?: string;
  objectType?: string;
  total?: number;
  [key: string]: unknown;
};

export type NotificationTypeSummary = {
  name?: string | null;
  total?: number;
  [key: string]: unknown;
};

export type RealtimeSocketEvent<TData = unknown> = {
  data?: TData;
  event: string;
  [key: string]: unknown;
};

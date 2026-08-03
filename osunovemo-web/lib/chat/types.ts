export type ChatChannelType =
  | "ANNOUNCE"
  | "GROUP"
  | "PM"
  | "PUBLIC"
  | "TEAM"
  | string;

export type ChatUser = {
  avatar_url?: string;
  avatarUrl?: string;
  country_code?: string;
  countryCode?: string;
  id: number;
  is_bot?: boolean;
  isBot?: boolean;
  username: string;
  [key: string]: unknown;
};

export type ChatChannel = {
  channelId?: number;
  channel_id?: number;
  currentUserAttributes?: {
    canListUsers?: boolean;
    canMessage?: boolean;
    canMessageError?: string | null;
    lastReadId?: number | null;
  };
  current_user_attributes?: {
    can_list_users?: boolean;
    can_message?: boolean;
    can_message_error?: string | null;
    last_read_id?: number | null;
  };
  description?: string;
  icon?: string | null;
  lastMessageId?: number | null;
  last_message_id?: number | null;
  lastReadId?: number | null;
  last_read_id?: number | null;
  messageLengthLimit?: number;
  message_length_limit?: number;
  name?: string;
  type?: ChatChannelType;
  users?: number[];
  uuid?: string | null;
  [key: string]: unknown;
};

export type ChatMessage = {
  channelId?: number;
  channel_id?: number;
  content: string;
  isAction?: boolean;
  is_action?: boolean;
  messageId?: number;
  message_id?: number;
  sender?: ChatUser;
  senderId?: number;
  sender_id?: number;
  timestamp?: string;
  type?: "action" | "markdown" | "plain" | string;
  uuid?: string | null;
};

export type ChatUpdatesResponse = {
  presence?: ChatChannel[];
};

export type ChatChannelDetailsResponse = {
  channel?: ChatChannel;
  users?: ChatUser[];
};

export type ChatMessagesNewEventData = {
  messages?: ChatMessage[];
  users?: ChatUser[];
};

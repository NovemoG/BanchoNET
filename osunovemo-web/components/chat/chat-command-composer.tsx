"use client";

import {
  type FormEvent,
  type KeyboardEvent,
  type ReactNode,
  useCallback,
  useEffect,
  useMemo,
  useRef,
  useState,
} from "react";
import {
  ChatComposerShell,
  parseOutgoingMessage,
  type ParsedOutgoingMessage,
} from "@/components/chat/chat-conversation";
import type { ChatBeatmapCardBeatmap } from "@/components/chat/beatmap-card-message";
import type { ChatProfileCardUser } from "@/components/chat/profile-card-message";
import type { ChatUser } from "@/lib/chat/types";
import type { ProfileUser } from "@/lib/profile";
import { cn } from "@/lib/utils";

export type NowPlayingContext = ChatBeatmapCardBeatmap & {
  label: string;
};

type ProfileCommandUser = ChatProfileCardUser;
type ProfileCommandMode = "fruits" | "mania" | "osu" | "taiko";
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

  if (command.kind === "np") {
    return {
      is_action: true,
      message: `is viewing [${command.value.url} ${command.value.label}]`,
    };
  }

  throw new Error("Unsupported chat command.");
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

export function ChatCommandComposer({
  buttonLabel,
  buttonVariant = "page",
  currentUser,
  disabled,
  maxLength,
  name,
  nowPlaying = null,
  onSubmit,
  placeholder,
  sending,
  shouldSubmitOnEnter,
  usersById,
}: {
  buttonLabel?: ReactNode;
  buttonVariant?: "icon" | "page";
  currentUser: ChatUser | null;
  disabled: boolean;
  maxLength?: number;
  name?: string;
  nowPlaying?: NowPlayingContext | null;
  onSubmit: (message: ParsedOutgoingMessage) => Promise<void>;
  placeholder: string;
  sending: boolean;
  shouldSubmitOnEnter?: (event: KeyboardEvent<HTMLTextAreaElement>) => boolean;
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

    const submitOnEnter = shouldSubmitOnEnter == null
      ? event.key === "Enter" && !event.shiftKey
      : shouldSubmitOnEnter(event);

    if (!submitOnEnter) {
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
      buttonLabel={buttonLabel}
      buttonVariant={buttonVariant}
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
      name={name}
      onChange={setMessageText}
      onKeyDown={handleKeyDown}
      onSubmit={handleSubmit}
      placeholder={activeCommand == null ? placeholder : commandParameterActive ? commandParameterPlaceholder : ""}
      readOnly={activeCommand != null && !commandParameterActive}
      resizeSignal={activeCommand}
      sendDisabled={sendDisabled}
      sending={sending || submitting}
      textareaRef={inputRef}
      value={messageText}
    />
  );
}

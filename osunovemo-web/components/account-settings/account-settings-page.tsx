"use client";

import {
  AtSign,
  Bold,
  BriefcaseBusiness,
  Check,
  ChevronDown,
  Globe,
  Hash,
  ImageIcon,
  Italic,
  LinkIcon,
  LogOut,
  MapPin,
  MessageCircle,
  Monitor,
  Pencil,
  Quote,
  Settings,
  Upload,
  UserRound,
  X,
} from "lucide-react";
import Link from "next/link";
import {
  type ChangeEvent,
  type ComponentType,
  type CSSProperties,
  type FormEvent,
  type ReactNode,
  useEffect,
  useMemo,
  useRef,
  useState,
} from "react";
import {signOut} from "@/app/actions/auth";
import {
  type AccountSession,
  AccountSettingsError,
  listSessions,
  revokeSession,
  updateAccount,
  updateCover,
  updateEmail,
  updateNotificationOptions,
  updateOptions,
  updatePassword,
  uploadAvatar,
} from "@/lib/account-settings-client";
import {buildAccountSettingsData} from "@/lib/account-settings";
import type {AuthUser} from "@/lib/auth/types";
import {Header} from "@/components/header";
import {PageWrapper} from "@/components/page-wrapper";
import {Alert, AlertDescription} from "@/components/ui/alert";
import {Button} from "@/components/ui/button";
import {FieldDescription, FieldGroup} from "@/components/ui/field";
import {ToggleGroup, ToggleGroupItem} from "@/components/ui/toggle-group";
import type {AccountSettingsData} from "@/lib/account-settings-types";
import {cn} from "@/lib/utils";

type AccountSettingsPageProps = {
  initialData: AccountSettingsData;
  sessionExpires: string;
};

type SettingsIcon = ComponentType<{className?: string; "aria-hidden"?: boolean}>;
type ProfileKey = keyof AccountSettingsData["profile"];
type PrivacyKey = keyof AccountSettingsData["privacy"];
type OptionsKey = keyof Omit<AccountSettingsData["options"], "beatmapsetDownload">;
type NotificationsKey = keyof Pick<
  AccountSettingsData["notifications"],
  "autoSubscribeTopics" | "commentReply"
>;
type NotificationModesKey = keyof Pick<
  AccountSettingsData["notifications"],
  "beatmapsetDiscussionQualifiedProblemModes" | "beatmapsetDisqualifyModes" | "newsPostSeries"
>;

const localAccountSettingsHref = "/home/account/edit";
const savedMessage = "Saved.";

// Mirrors ProfileAssetUrls.PresetCoverCount on the server.
const presetCoverIds = [1, 2, 3, 4, 5, 6, 7, 8] as const;

/**
 * Presets and custom uploads share a directory, so the asset host is taken from whatever cover
 * the server already sent rather than reconstructed from the API origin.
 */
function formatSessionDate(value: string) {
  const date = new Date(value);

  return Number.isNaN(date.getTime())
    ? "unknown"
    : new Intl.DateTimeFormat("en-US", {
      day: "numeric",
      hour: "2-digit",
      minute: "2-digit",
      month: "short",
      year: "numeric",
    }).format(date);
}

function presetCoverUrl(coverUrl: string | null, coverId: number) {
  if (coverUrl == null) {
    return null;
  }

  const separator = coverUrl.lastIndexOf("/");

  return separator < 0 ? null : `${coverUrl.slice(0, separator + 1)}${coverId}.jpg`;
}

const accountHeaderLinks = [
  {active: false, href: "/", label: "dashboard"},
  {active: false, href: "/friends", label: "friends"},
  {active: true, href: localAccountSettingsHref, label: "settings"},
] as const;

const profileFields: Array<{
  field: ProfileKey;
  icon: SettingsIcon;
  label: string;
  maxLength: number;
}> = [
  {field: "location", icon: MapPin, label: "current location", maxLength: 100},
  {field: "interests", icon: Hash, label: "interests", maxLength: 255},
  {field: "occupation", icon: BriefcaseBusiness, label: "occupation", maxLength: 100},
  {field: "twitter", icon: AtSign, label: "twitter", maxLength: 100},
  {field: "discord", icon: MessageCircle, label: "discord", maxLength: 100},
  {field: "website", icon: Globe, label: "website", maxLength: 255},
];

const playstyleOptions = [
  {label: "mouse", value: "mouse"},
  {label: "keyboard", value: "keyboard"},
  {label: "tablet", value: "tablet"},
  {label: "touch", value: "touch"},
] as const;

const rulesetOptions = [
  {label: "osu!", value: "osu"},
  {label: "taiko", value: "taiko"},
  {label: "catch", value: "fruits"},
  {label: "mania", value: "mania"},
] as const;

const downloadOptions = [
  {label: "with video if available", value: "all"},
  {label: "without video", value: "no_video"},
  {label: "open in osu!direct", value: "direct"},
] as const;

const deliveryRows = [
  {label: "guest difficulty", value: "beatmap_owner_change"},
  {label: "beatmap modding", value: "beatmapset:modding"},
  {label: "private chat messages", value: "channel_message"},
  {label: "chat mention", value: "channel_mention"},
  {label: "team chat messages", value: "channel_team"},
  {label: "new comments", value: "comment_new"},
  {label: "topic reply", value: "forum_topic_reply"},
  {label: "beatmap mapper", value: "mapping"},
  {label: "news posts", value: "news_post"},
] as const;

const newsSeriesOptions = [
  {label: "osu!", value: "osu"},
  {label: "community", value: "community"},
  {label: "dev", value: "dev"},
  {label: "store", value: "store"},
] as const;

const inputClassName =
  "min-h-8 w-full max-w-[250px] rounded-md border-2 border-transparent bg-osu-b3 px-2 py-1 text-sm font-normal text-white outline-none transition-colors placeholder:text-osu-f1 focus:border-osu-l1 disabled:opacity-60";

const wideInputClassName = cn(inputClassName, "max-w-full");

function isExternalHref(href: string) {
  return href.startsWith("http://") || href.startsWith("https://");
}

function AccountHeaderNav() {
  const activeLink = accountHeaderLinks.find((link) => link.active) ?? accountHeaderLinks[0];

  return (
    <>
      <ul className="relative hidden items-center gap-5 text-xs md:flex md:text-sm before:absolute before:bottom-0 before:left-0 before:right-0 before:h-px before:bg-osu-h1">
        {accountHeaderLinks.map((link) => (
          <li className="relative flex" key={link.label}>
            {isExternalHref(link.href) ? (
              <a
                className={cn(
                  "relative z-1 flex items-baseline gap-1.25 py-2.5 text-osu-c1 transition-colors before:absolute before:-bottom-0.5 before:left-0 before:hidden before:h-1.25 before:w-full before:scale-y-0 before:rounded-full before:bg-osu-h1 before:transition-transform hover:text-white md:py-4 md:before:block hover:before:scale-y-100",
                  link.active && "font-semibold text-white before:scale-y-100",
                )}
                href={link.href}
              >
                <span>{link.label}</span>
              </a>
            ) : (
              <Link
                className={cn(
                  "relative z-1 flex items-baseline gap-1.25 py-2.5 text-osu-c1 transition-colors before:absolute before:-bottom-0.5 before:left-0 before:hidden before:h-1.25 before:w-full before:scale-y-0 before:rounded-full before:bg-osu-h1 before:transition-transform hover:text-white md:py-4 md:before:block hover:before:scale-y-100",
                  link.active && "font-semibold text-white before:scale-y-100",
                )}
                href={link.href}
              >
                <span>{link.label}</span>
              </Link>
            )}
          </li>
        ))}
      </ul>

      <details className="group relative w-full text-xs md:hidden">
        <summary className="relative block w-max max-w-full list-none py-2.5 text-white marker:hidden before:absolute before:-bottom-0.5 before:left-0 before:block before:h-1 before:w-full before:rounded-full before:bg-osu-h1 before:content-['']">
          {activeLink.label}
          <span className="absolute top-0 left-full flex h-full items-center pl-2.5 text-[0.8em] transition-transform group-open:rotate-180">
            <ChevronDown aria-hidden className="size-4" />
          </span>
        </summary>
        <ul className="absolute inset-x-0 top-full z-101 -mx-4 hidden list-none bg-osu-d5 p-0 group-open:grid sm:-mx-6 lg:-mx-8">
          {accountHeaderLinks.map((link) => (
            <li key={link.label}>
              {isExternalHref(link.href) ? (
                <a
                  className={cn(
                    "flex px-4 py-3 text-white/70 transition-colors hover:bg-white/5 hover:text-white sm:px-6 lg:px-8",
                    link.active && "bg-white/8 text-white",
                  )}
                  href={link.href}
                >
                  {link.label}
                </a>
              ) : (
                <Link
                  className={cn(
                    "flex px-4 py-3 text-white/70 transition-colors hover:bg-white/5 hover:text-white sm:px-6 lg:px-8",
                    link.active && "bg-white/8 text-white",
                  )}
                  href={link.href}
                >
                  {link.label}
                </Link>
              )}
            </li>
          ))}
        </ul>
      </details>
    </>
  );
}

function AccountSection({
  children,
  first = false,
  id,
  title,
}: {
  children: ReactNode;
  first?: boolean;
  id?: string;
  title: string;
}) {
  return (
    <section
      className={cn(
        "relative flex flex-col bg-osu-b5 text-sm text-white shadow-[0_2px_10px_rgba(0,0,0,0.26)] md:flex-row",
        first && "after:pointer-events-none after:absolute after:inset-0 after:shadow-[inset_0_8px_14px_rgba(0,0,0,0.24)]",
      )}
      id={id}
    >
      <div className="w-full flex-none px-5 py-5 md:w-[250px] md:pl-8">
        <h2 className="m-0 overflow-wrap-anywhere text-xl font-bold text-white">{title}</h2>
      </div>
      <div className="grid flex-1 gap-px">{children}</div>
    </section>
  );
}

function InputGroup({children}: {children: ReactNode}) {
  return (
    <FieldGroup className="gap-0 bg-osu-b4 p-2.5">
      {children}
    </FieldGroup>
  );
}

// One fixed label column for the whole screen, rendered on every row whether it has a label or
// not. shrink-0/grow-0 are required: without them a long label widens the column and a short one
// lets it collapse, so controls never line up.
//
// SettingsEntry deliberately does not use Field's "responsive" orientation. That variant forces
// flex-auto onto the label, switches the row to items-start whenever a FieldContent is present
// (which is why labels sat above their inputs instead of centred), and is driven by container
// queries rather than viewport breakpoints.
const labelColumnClassName = "md:w-40 md:shrink-0 md:grow-0 md:pr-2.5";

function SettingsEntry({
  children,
  description,
  label,
  topPinned = false,
}: {
  children: ReactNode;
  description?: ReactNode;
  label?: string;
  topPinned?: boolean;
}) {
  return (
    <div
      className={cn(
        "flex flex-col gap-1 py-2.5 md:flex-row md:gap-0",
        // Centre the label against its control by default; topPinned rows have tall content
        // (textareas, image pickers) where the label belongs at the top instead.
        topPinned ? "md:items-start" : "md:items-center",
      )}
    >
      {/* Rendered even without a label, so the content column starts in the same place either
          way. Padding the row instead would narrow the content and stop full width blocks such
          as the notification matrix from stretching. */}
      <div
        className={cn(
          labelColumnClassName,
          "text-sm text-osu-f1 md:text-right",
          topPinned && "md:pt-2",
        )}
      >
        {label}
      </div>

      <div className="flex min-w-0 flex-1 flex-col gap-1">
        {children}
        {description == null ? null : <FieldDescription>{description}</FieldDescription>}
      </div>
    </div>
  );
}

function ReadonlyValue({children}: {children: ReactNode}) {
  return (
    <div className="flex min-h-8 max-w-full items-center rounded-md px-2 py-1 text-white">
      {children}
    </div>
  );
}

function TextInput({
  icon: Icon,
  value,
  ...props
}: Omit<React.ComponentProps<"input">, "value"> & {
  icon?: SettingsIcon;
  value: string;
}) {
  return (
    <div className="relative w-full max-w-[250px]">
      {Icon == null ? null : (
        <Icon aria-hidden className="pointer-events-none absolute left-2 top-1/2 size-4 -translate-y-1/2 text-osu-f1" />
      )}
      <input
        className={cn(inputClassName, Icon != null && "pl-8")}
        value={value}
        {...props}
      />
    </div>
  );
}

function TextArea({
  className,
  ...props
}: React.ComponentProps<"textarea">) {
  return (
    <textarea
      className={cn(
        wideInputClassName,
        "min-h-36 resize-y leading-5",
        className,
      )}
      {...props}
    />
  );
}

function SwitchControl({
  checked,
  description,
  label,
  onCheckedChange,
}: {
  checked: boolean;
  description?: ReactNode;
  label: ReactNode;
  onCheckedChange: (checked: boolean) => void;
}) {
  return (
    <label className="flex cursor-pointer flex-wrap items-start gap-x-2.5 gap-y-1 md:flex-nowrap">
      <input
        checked={checked}
        className="peer sr-only"
        onChange={(event) => onCheckedChange(event.currentTarget.checked)}
        type="checkbox"
      />
      <span className="relative mt-0.5 inline-flex h-5 w-9 shrink-0 rounded-full bg-osu-b2 shadow-[inset_0_0_0_2px_rgba(0,0,0,0.25)] transition-colors peer-focus-visible:ring-2 peer-focus-visible:ring-osu-c2/40 peer-checked:bg-osu-h2 after:absolute after:left-0.5 after:top-0.5 after:size-4 after:rounded-full after:bg-white after:transition-transform peer-checked:after:translate-x-4" />
      <span className="min-w-0 text-sm font-semibold text-white">
        {label}
        {description == null ? null : (
          <span className="mt-1 block text-xs font-normal leading-normal text-osu-f1">
            {description}
          </span>
        )}
      </span>
    </label>
  );
}

function StatusNotice({message}: {message?: string}) {
  if (message == null) {
    return null;
  }

  return (
    <Alert className="mt-3 shadow-none">
      <AlertDescription>{message}</AlertDescription>
    </Alert>
  );
}

function SaveRow({
  busy,
  label = "save",
  message,
  onSave,
}: {
  busy: boolean;
  label?: string;
  message?: string;
  onSave: () => void;
}) {
  return (
    <InputGroup>
      <SettingsEntry>
        <Button disabled={busy} onClick={onSave} type="button">
          {label}
          <Check data-icon="inline-end" />
        </Button>
        <StatusNotice message={message} />
      </SettingsEntry>
    </InputGroup>
  );
}

function ToolbarButton({
  children,
  onClick,
  title,
}: {
  children: ReactNode;
  onClick: () => void;
  title: string;
}) {
  return (
    <button
      className="inline-flex size-8 items-center justify-center rounded-md bg-osu-b5 text-osu-l1 transition-colors hover:bg-osu-b3 hover:text-white focus-visible:ring-2 focus-visible:ring-osu-c2/40"
      onClick={onClick}
      title={title}
      type="button"
    >
      {children}
      <span className="sr-only">{title}</span>
    </button>
  );
}

function ToggleOptionGroup({
  onValueChange,
  type,
  value,
  values,
}: {
  onValueChange: (value: string | string[]) => void;
  type: "multiple" | "single";
  value: string | string[];
  values: ReadonlyArray<{label: string; value: string}>;
}) {
  if (type === "multiple") {
    return (
      <ToggleGroup
        className="w-full"
        onValueChange={(nextValue) => onValueChange(nextValue)}
        type="multiple"
        value={Array.isArray(value) ? value : []}
      >
        {values.map((option) => (
          <ToggleGroupItem key={option.value} value={option.value}>
            {option.label}
          </ToggleGroupItem>
        ))}
      </ToggleGroup>
    );
  }

  return (
    <ToggleGroup
      className="w-full"
      onValueChange={(nextValue) => onValueChange(nextValue)}
      type="single"
      value={typeof value === "string" ? value : undefined}
    >
      {values.map((option) => (
        <ToggleGroupItem key={option.value} value={option.value}>
          {option.label}
        </ToggleGroupItem>
      ))}
    </ToggleGroup>
  );
}

export function AccountSettingsPage({
  initialData,
  sessionExpires,
}: AccountSettingsPageProps) {
  const [data, setData] = useState(initialData);
  const [sectionMessages, setSectionMessages] = useState<Record<string, string | undefined>>({});
  const [savingSection, setSavingSection] = useState<string | null>(null);
  const [sessions, setSessions] = useState<AccountSession[] | null>(null);
  const [pendingAvatarFile, setPendingAvatarFile] = useState<File | null>(null);
  const [pendingCoverFile, setPendingCoverFile] = useState<File | null>(null);
  const [pendingCoverId, setPendingCoverId] = useState<number | null>(null);
  const objectUrlRef = useRef<string | null>(null);
  const signatureTextAreaRef = useRef<HTMLTextAreaElement | null>(null);

  useEffect(() => {
    return () => {
      if (objectUrlRef.current != null) {
        URL.revokeObjectURL(objectUrlRef.current);
      }
    };
  }, []);

  useEffect(() => {
    let cancelled = false;

    listSessions()
      .then((loaded) => {
        if (!cancelled) setSessions(loaded);
      })
      .catch((error: unknown) => {
        if (cancelled) return;

        setSectionMessages((current) => ({
          ...current,
          sessions:
            error instanceof AccountSettingsError ? error.message : "Could not load sessions.",
        }));
      });

    return () => {
      cancelled = true;
    };
  }, []);

  const headerBackgroundStyle = useMemo<CSSProperties | undefined>(() => {
    return data.coverUrl == null
      ? undefined
      : {backgroundImage: `url(${JSON.stringify(data.coverUrl)})`};
  }, [data.coverUrl]);

  const countryText = data.countryName ?? data.countryCode ?? "unknown";
  const sessionExpiresDate = new Date(sessionExpires);
  const formattedSessionExpiry = Number.isNaN(sessionExpiresDate.getTime())
    ? "unknown"
    : new Intl.DateTimeFormat("en-US", {
      day: "numeric",
      hour: "2-digit",
      minute: "2-digit",
      month: "short",
      year: "numeric",
    }).format(sessionExpiresDate);

  const setSectionMessage = (section: string, message = savedMessage) => {
    setSectionMessages((current) => ({
      ...current,
      [section]: message,
    }));
  };

  /**
   * Every mutating endpoint returns the same payload as GET /me/settings, so success is always
   * "replace the whole view model with what the server now says".
   */
  const runSave = async (section: string, action: () => Promise<AuthUser | null>) => {
    setSavingSection(section);
    setSectionMessage(section, "Saving…");

    try {
      const updated = await action();

      if (updated != null) {
        setData(buildAccountSettingsData(updated));
      }

      setSectionMessage(section, savedMessage);
      return true;
    } catch (error) {
      setSectionMessage(
        section,
        error instanceof AccountSettingsError ? error.message : "Something went wrong.",
      );
      return false;
    } finally {
      setSavingSection(null);
    }
  };

  const submitSection =
    (section: string, action: () => Promise<AuthUser | null>) =>
    (event: FormEvent<HTMLFormElement>) => {
      event.preventDefault();
      void runSave(section, action);
    };

  const updateProfileField = (field: ProfileKey, value: string) => {
    setData((current) => ({
      ...current,
      profile: {
        ...current.profile,
        [field]: value,
      },
    }));
  };

  const updatePrivacyField = (field: PrivacyKey, value: boolean) => {
    setData((current) => ({
      ...current,
      privacy: {
        ...current.privacy,
        [field]: value,
      },
    }));
  };

  const updateOptionField = (field: OptionsKey, value: boolean) => {
    setData((current) => ({
      ...current,
      options: {
        ...current.options,
        [field]: value,
      },
    }));
  };

  const updateNotificationField = (field: NotificationsKey, value: boolean) => {
    setData((current) => ({
      ...current,
      notifications: {
        ...current.notifications,
        [field]: value,
      },
    }));
  };

  const updateNotificationModes = (field: NotificationModesKey, value: string[]) => {
    setData((current) => ({
      ...current,
      notifications: {
        ...current.notifications,
        [field]: value,
      },
    }));
  };

  const updateDeliveryField = (row: string, mode: "mail" | "push", checked: boolean) => {
    setData((current) => ({
      ...current,
      notifications: {
        ...current.notifications,
        delivery: {
          ...current.notifications.delivery,
          [row]: {
            ...current.notifications.delivery[row],
            [mode]: checked,
          },
        },
      },
    }));
  };

  const submitPassword = (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();

    const form = event.currentTarget;
    const value = (name: string) =>
      (form.elements.namedItem(name) as HTMLInputElement | null)?.value ?? "";

    void runSave("password", () =>
      updatePassword({
        currentPassword: value("user[current_password]"),
        password: value("user[password]"),
        passwordConfirmation: value("user[password_confirmation]"),
      }),
    ).then((ok) => {
      // The caller's own session survives a password change, so there is nothing to redirect to.
      if (ok) form.reset();
    });
  };

  const submitEmail = (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();

    const form = event.currentTarget;
    const value = (name: string) =>
      (form.elements.namedItem(name) as HTMLInputElement | null)?.value ?? "";

    void runSave("email", () =>
      updateEmail({
        currentPassword: value("user[current_password]"),
        email: value("user[user_email]"),
        emailConfirmation: value("user[user_email_confirmation]"),
      }),
    ).then((ok) => {
      if (ok) form.reset();
    });
  };

  const saveProfile = () =>
    runSave("profile", () =>
      updateAccount({
        discord: data.profile.discord,
        interests: data.profile.interests,
        location: data.profile.location,
        occupation: data.profile.occupation,
        twitter: data.profile.twitter,
        website: data.profile.website,
      }),
    );

  const saveSignatureRequest = () => updateAccount({signature: data.profile.signature});

  const savePlaystyles = () =>
    runSave("playstyles", () => updateAccount({playstyles: data.playstyles}));

  const savePrivacy = () =>
    runSave("privacy", () =>
      updateAccount({
        hidePresence: data.privacy.hidePresence,
        pmFriendsOnly: data.privacy.pmFriendsOnly,
      }),
    );

  const saveOptions = () =>
    runSave("options", () =>
      updateOptions({
        beatmapsetDownload: data.options.beatmapsetDownload,
        beatmapsetShowAnimeCover: data.options.beatmapsetShowAnimeCover,
        beatmapsetShowNsfw: data.options.beatmapsetShowNsfw,
        beatmapsetTitleShowOriginal: data.options.beatmapsetTitleShowOriginal,
      }),
    );

  const saveNotifications = () =>
    runSave("notifications", async () => {
      // user_notify lives on the user row upstream, the rest are notification options.
      await updateAccount({userNotify: data.notifications.autoSubscribeTopics});

      return updateNotificationOptions([
        ...Object.entries(data.notifications.delivery).map(([name, modes]) => ({
          name,
          details:
            name === "comment_new"
              ? {...modes, comment_reply: data.notifications.commentReply}
              : name === "news_post"
                ? {...modes, series: data.notifications.newsPostSeries}
                : {...modes},
        })),
        {
          name: "beatmapset_disqualify",
          details: {modes: data.notifications.beatmapsetDisqualifyModes},
        },
        {
          name: "beatmapset_discussion_qualified_problem",
          details: {modes: data.notifications.beatmapsetDiscussionQualifiedProblemModes},
        },
      ]);
    });

  const saveAvatar = () => {
    if (pendingAvatarFile == null) {
      setSectionMessage("avatar", "Pick an image first.");
      return;
    }

    const file = pendingAvatarFile;

    void runSave("avatar", async () => {
      const updated = await uploadAvatar(file);

      // The server URL carries a fresh cache buster, so the local preview can go.
      if (objectUrlRef.current != null) {
        URL.revokeObjectURL(objectUrlRef.current);
        objectUrlRef.current = null;
      }

      setPendingAvatarFile(null);
      return updated;
    });
  };

  // Selecting a preset only stages it, same as picking a file. One save handler sends whichever
  // of the two is pending so the section behaves like every other one on the page.
  const saveCover = () => {
    if (pendingCoverFile != null) {
      const file = pendingCoverFile;

      void runSave("cover", async () => {
        const updated = await updateCover({file});
        setPendingCoverFile(null);
        return updated;
      });

      return;
    }

    if (pendingCoverId != null) {
      const coverId = pendingCoverId;

      void runSave("cover", async () => {
        const updated = await updateCover({coverId});
        setPendingCoverId(null);
        return updated;
      });

      return;
    }

    setSectionMessage("cover", "Pick a cover first.");
  };

  const loadSessions = async () => {
    try {
      setSessions(await listSessions());
    } catch (error) {
      setSectionMessage(
        "sessions",
        error instanceof AccountSettingsError ? error.message : "Could not load sessions.",
      );
    }
  };

  const endSession = async (id: string, current: boolean) => {
    try {
      await revokeSession(id);

      if (current) {
        // Our own token was just denylisted, so every following request would 401.
        await signOut();
        return;
      }

      await loadSessions();
      setSectionMessage("sessions", "Session ended.");
    } catch (error) {
      setSectionMessage(
        "sessions",
        error instanceof AccountSettingsError ? error.message : "Could not end that session.",
      );
    }
  };

  const handleAvatarFileChange = (event: ChangeEvent<HTMLInputElement>) => {
    const file = event.currentTarget.files?.[0];

    if (file == null) {
      return;
    }

    if (objectUrlRef.current != null) {
      URL.revokeObjectURL(objectUrlRef.current);
    }

    const objectUrl = URL.createObjectURL(file);
    objectUrlRef.current = objectUrl;
    setPendingAvatarFile(file);
    setData((current) => ({
      ...current,
      avatarUrl: objectUrl,
    }));
    setSectionMessage("avatar", "Ready to upload.");
  };

  const handleCoverFileChange = (event: ChangeEvent<HTMLInputElement>) => {
    const file = event.currentTarget.files?.[0];

    if (file == null) {
      return;
    }

    setPendingCoverFile(file);
    setPendingCoverId(null); // a file and a preset are mutually exclusive server side
    setSectionMessage("cover", "Ready to upload.");
  };

  const resetAvatarPreview = () => {
    if (objectUrlRef.current != null) {
      URL.revokeObjectURL(objectUrlRef.current);
      objectUrlRef.current = null;
    }

    setPendingAvatarFile(null);
    setData((current) => ({
      ...current,
      avatarUrl: initialData.avatarUrl,
    }));
    setSectionMessage("avatar", "Avatar preview reset.");
  };

  const wrapSignatureSelection = (prefix: string, suffix = prefix) => {
    const textArea = signatureTextAreaRef.current;
    const signature = data.profile.signature;

    if (textArea == null) {
      updateProfileField("signature", `${signature}${prefix}${suffix}`);
      return;
    }

    const start = textArea.selectionStart;
    const end = textArea.selectionEnd;
    const selectedText = signature.slice(start, end);
    const nextSignature = `${signature.slice(0, start)}${prefix}${selectedText}${suffix}${signature.slice(end)}`;

    updateProfileField("signature", nextSignature);
    requestAnimationFrame(() => {
      textArea.focus();
      textArea.setSelectionRange(start + prefix.length, start + prefix.length + selectedText.length);
    });
  };

  return (
    <main className="bg-osu-b6 pb-10">
      <Header
        background={(
          <div className="absolute inset-0 bg-osu-b4">
            {data.coverUrl == null ? (
              <div
                className="absolute inset-0 bg-[url('/layout/nav2-background-hue0.png')] bg-cover bg-center opacity-70"
              />
            ) : (
              <div
                className="absolute inset-0 bg-cover bg-center opacity-80"
                style={headerBackgroundStyle}
              />
            )}
            <div className="absolute inset-0 bg-linear-to-b from-osu-b6/5 via-osu-b6/45 to-osu-b6/85" />
          </div>
        )}
        bottom={<AccountHeaderNav />}
        bottomClassName="relative bg-osu-d4/95"
        icon={(
          <div className="flex w-10 flex-none items-center justify-center self-stretch">
            <Settings aria-hidden className="size-5 text-white" />
          </div>
        )}
        mobileSubtitle={data.username}
        title="account settings"
        topClassName="bg-osu-d5/92 text-osu-c1"
        topRight={(
          <div className="hidden min-w-0 items-center gap-2 text-sm text-osu-c1 sm:flex">
            <span
              className="size-8 shrink-0 rounded-full bg-osu-b4 bg-cover bg-center"
              style={data.avatarUrl == null ? undefined : {backgroundImage: `url(${JSON.stringify(data.avatarUrl)})`}}
            >
              {data.avatarUrl == null ? (
                <span className="flex size-full items-center justify-center">
                  <UserRound aria-hidden className="size-4" />
                </span>
              ) : null}
            </span>
            <span className="truncate font-semibold">{data.username}</span>
          </div>
        )}
      />

      <PageWrapper className="mb-10 grid gap-px">
        <AccountSection first title="Profile">
          <InputGroup>
            <SettingsEntry label="username">
              <div className="flex flex-wrap items-center gap-2">
                <ReadonlyValue>{data.username}</ReadonlyValue>
                <Button asChild size="sm" variant="outline">
                  <a href="https://osu.ppy.sh/store/products/username-change">
                    change
                    <Pencil data-icon="inline-end" />
                  </a>
                </Button>
              </div>
            </SettingsEntry>

            <SettingsEntry label="country" topPinned>
              <ReadonlyValue>
                <span className="mr-2 inline-flex min-w-8 items-center justify-center rounded-full bg-osu-b5 px-2 py-0.5 text-xs font-bold text-osu-c1">
                  {data.countryCode ?? "--"}
                </span>
                {countryText}
              </ReadonlyValue>
            </SettingsEntry>
          </InputGroup>

          <InputGroup>
            {profileFields.slice(0, 3).map(({field, icon, label, maxLength}) => (
              <SettingsEntry key={field} label={label}>
                <TextInput
                  icon={icon}
                  maxLength={maxLength}
                  onChange={(event) => updateProfileField(field, event.currentTarget.value)}
                  value={data.profile[field]}
                />
              </SettingsEntry>
            ))}
          </InputGroup>

          <InputGroup>
            {profileFields.slice(3).map(({field, icon, label, maxLength}) => (
              <SettingsEntry key={field} label={label}>
                <TextInput
                  icon={icon}
                  maxLength={maxLength}
                  onChange={(event) => updateProfileField(field, event.currentTarget.value)}
                  value={data.profile[field]}
                />
              </SettingsEntry>
            ))}
          </InputGroup>
          <SaveRow busy={savingSection === "profile"} label="save" message={sectionMessages.profile} onSave={saveProfile} />
        </AccountSection>

        <AccountSection id="avatar" title="Avatar">
          <InputGroup>
            <SettingsEntry>
              <div className="flex flex-col gap-3">
                <div
                  className="size-[120px] overflow-hidden rounded-md bg-osu-b5 bg-cover bg-center shadow-[inset_0_0_0_1px_rgba(255,255,255,0.08)]"
                  style={data.avatarUrl == null ? undefined : {backgroundImage: `url(${JSON.stringify(data.avatarUrl)})`}}
                >
                  {data.avatarUrl == null ? (
                    <span className="flex size-full items-center justify-center text-osu-f1">
                      <UserRound aria-hidden className="size-10" />
                    </span>
                  ) : null}
                </div>

                <div className="flex flex-wrap gap-2">
                  <Button asChild>
                    <label>
                      upload image
                      <Upload data-icon="inline-end" />
                      <input
                        accept="image/*"
                        className="sr-only"
                        onChange={handleAvatarFileChange}
                        type="file"
                      />
                    </label>
                  </Button>
                  <Button
                    disabled={pendingAvatarFile == null || savingSection === "avatar"}
                    onClick={saveAvatar}
                    type="button"
                  >
                    save
                    <Check data-icon="inline-end" />
                  </Button>
                  <Button onClick={resetAvatarPreview} type="button" variant="outline">
                    reset
                    <X data-icon="inline-end" />
                  </Button>
                </div>

                <p className="max-w-2xl text-xs leading-normal text-osu-f1">
                  Please ensure your avatar adheres to the Visual content considerations and is suitable for all ages.
                </p>
                <StatusNotice message={sectionMessages.avatar} />
              </div>
            </SettingsEntry>
          </InputGroup>
        </AccountSection>

        <AccountSection id="cover" title="Cover">
          <InputGroup>
            <SettingsEntry>
              <div className="flex flex-col gap-3">
                <div
                  className="h-[120px] w-full max-w-2xl overflow-hidden rounded-md bg-osu-b5 bg-cover bg-center shadow-[inset_0_0_0_1px_rgba(255,255,255,0.08)]"
                  style={data.coverUrl == null ? undefined : {backgroundImage: `url(${JSON.stringify(data.coverUrl)})`}}
                />

                <div className="flex flex-wrap gap-2">
                  {presetCoverIds.map((coverId) => {
                    const url = presetCoverUrl(data.cover.url ?? data.cover.customUrl, coverId);

                    return (
                      <button
                        className={cn(
                          "h-12 w-24 overflow-hidden rounded-md bg-osu-b5 bg-cover bg-center shadow-[inset_0_0_0_1px_rgba(255,255,255,0.08)] transition-opacity hover:opacity-80",
                          // Staged selection wins over the saved one so the pending choice is what
                          // is highlighted until the user presses save.
                          (pendingCoverId ?? Number(data.cover.id)) === coverId &&
                            pendingCoverFile == null &&
                            "ring-2 ring-osu-l1",
                        )}
                        disabled={savingSection === "cover"}
                        key={coverId}
                        onClick={() => {
                          setPendingCoverId(coverId);
                          setPendingCoverFile(null);
                          setSectionMessage("cover", "Ready to save.");
                        }}
                        style={url == null ? undefined : {backgroundImage: `url(${JSON.stringify(url)})`}}
                        title={`cover ${coverId}`}
                        type="button"
                      />
                    );
                  })}
                </div>

                {data.isSupporter ? (
                  <Button asChild variant="outline">
                    <label className="w-fit">
                      upload image
                      <Upload data-icon="inline-end" />
                      <input
                        accept="image/*"
                        className="sr-only"
                        onChange={handleCoverFileChange}
                        type="file"
                      />
                    </label>
                  </Button>
                ) : (
                  <p className="max-w-2xl text-xs leading-normal text-osu-f1">
                    Custom cover uploads are available to supporters.
                  </p>
                )}

                <Button
                  className="w-fit"
                  disabled={
                    (pendingCoverFile == null && pendingCoverId == null) ||
                    savingSection === "cover"
                  }
                  onClick={saveCover}
                  type="button"
                >
                  save
                  <Check data-icon="inline-end" />
                </Button>

                <StatusNotice message={sectionMessages.cover} />
              </div>
            </SettingsEntry>
          </InputGroup>
        </AccountSection>

        <AccountSection id="signature" title="Signature">
          <form onSubmit={submitSection("signature", saveSignatureRequest)}>
            <InputGroup>
              <SettingsEntry>
                <div className="max-h-28 overflow-hidden rounded-md bg-osu-b5/60 px-3 py-2 text-sm leading-5 text-osu-c1">
                  {data.profile.signature.trim().length > 0 ? (
                    <p className="whitespace-pre-wrap">{data.profile.signature}</p>
                  ) : (
                    <p className="text-osu-f1">No signature set.</p>
                  )}
                </div>
              </SettingsEntry>

              <SettingsEntry>
                <TextArea
                  name="user[user_sig]"
                  onChange={(event) => updateProfileField("signature", event.currentTarget.value)}
                  ref={signatureTextAreaRef}
                  rows={6}
                  value={data.profile.signature}
                />
              </SettingsEntry>

              <SettingsEntry>
                <div className="flex flex-wrap gap-1.5">
                  <ToolbarButton onClick={() => wrapSignatureSelection("[b]", "[/b]")} title="bold">
                    <Bold aria-hidden className="size-4" />
                  </ToolbarButton>
                  <ToolbarButton onClick={() => wrapSignatureSelection("[i]", "[/i]")} title="italic">
                    <Italic aria-hidden className="size-4" />
                  </ToolbarButton>
                  <ToolbarButton onClick={() => wrapSignatureSelection("[url=]", "[/url]")} title="link">
                    <LinkIcon aria-hidden className="size-4" />
                  </ToolbarButton>
                  <ToolbarButton onClick={() => wrapSignatureSelection("[img]", "[/img]")} title="image">
                    <ImageIcon aria-hidden className="size-4" />
                  </ToolbarButton>
                  <ToolbarButton onClick={() => wrapSignatureSelection("[quote]", "[/quote]")} title="quote">
                    <Quote aria-hidden className="size-4" />
                  </ToolbarButton>
                </div>
              </SettingsEntry>
            </InputGroup>

            <InputGroup>
              <SettingsEntry>
                <Button type="submit">
                  update
                  <Check data-icon="inline-end" />
                </Button>
                <StatusNotice message={sectionMessages.signature} />
              </SettingsEntry>
            </InputGroup>
          </form>
        </AccountSection>

        <AccountSection id="playstyles" title="Playstyles">
          <InputGroup>
            <SettingsEntry>
              <ToggleOptionGroup
                onValueChange={(value) => {
                  if (Array.isArray(value)) {
                    setData((current) => ({...current, playstyles: value}));
                  }
                }}
                type="multiple"
                value={data.playstyles}
                values={playstyleOptions}
              />
            </SettingsEntry>
          </InputGroup>
          <SaveRow busy={savingSection === "playstyles"} label="save" message={sectionMessages.playstyles} onSave={savePlaystyles} />
        </AccountSection>

        <AccountSection id="privacy" title="Privacy">
          <InputGroup>
            <SettingsEntry>
              <SwitchControl
                checked={data.privacy.pmFriendsOnly}
                label="block private messages from people not on your friends list"
                onCheckedChange={(checked) => updatePrivacyField("pmFriendsOnly", checked)}
              />
            </SettingsEntry>

            <SettingsEntry>
              <SwitchControl
                checked={data.privacy.hidePresence}
                description='this maps to the "appear offline" mode in osu!lazer'
                label="hide your online presence"
                onCheckedChange={(checked) => updatePrivacyField("hidePresence", checked)}
              />
            </SettingsEntry>
          </InputGroup>
          <SaveRow busy={savingSection === "privacy"} label="save" message={sectionMessages.privacy} onSave={savePrivacy} />
        </AccountSection>

        <AccountSection id="notifications" title="Notifications">
          <InputGroup>
            <SettingsEntry>
              <SwitchControl
                checked={data.notifications.autoSubscribeTopics}
                label="automatically enable notifications on new forum topics that you create or replied to"
                onCheckedChange={(checked) => updateNotificationField("autoSubscribeTopics", checked)}
              />
            </SettingsEntry>

            <SettingsEntry>
              <SwitchControl
                checked={data.notifications.commentReply}
                label="receive notifications for replies to your comments"
                onCheckedChange={(checked) => updateNotificationField("commentReply", checked)}
              />
            </SettingsEntry>
          </InputGroup>

          <InputGroup>
            <SettingsEntry description="receive notifications for when beatmaps of the following modes are disqualified">
              <ToggleOptionGroup
                onValueChange={(value) => {
                  if (Array.isArray(value)) {
                    updateNotificationModes("beatmapsetDisqualifyModes", value);
                  }
                }}
                type="multiple"
                value={data.notifications.beatmapsetDisqualifyModes}
                values={rulesetOptions}
              />
            </SettingsEntry>

            <SettingsEntry description="receive notifications for new problems on qualified beatmaps of the following modes">
              <ToggleOptionGroup
                onValueChange={(value) => {
                  if (Array.isArray(value)) {
                    updateNotificationModes("beatmapsetDiscussionQualifiedProblemModes", value);
                  }
                }}
                type="multiple"
                value={data.notifications.beatmapsetDiscussionQualifiedProblemModes}
                values={rulesetOptions}
              />
            </SettingsEntry>
          </InputGroup>

          <InputGroup>
            <SettingsEntry label="delivery options" topPinned>
              <div className="overflow-x-auto">
                <table className="w-full min-w-[420px] border-separate border-spacing-y-1 text-left text-xs">
                  <thead className="text-osu-f1">
                    <tr>
                      <th className="px-2 py-1 font-semibold">mail</th>
                      <th className="px-2 py-1 font-semibold">push</th>
                      <th className="px-2 py-1 font-semibold">notification</th>
                    </tr>
                  </thead>
                  <tbody>
                    {deliveryRows.map((row) => {
                      const delivery = data.notifications.delivery[row.value] ?? {mail: false, push: false};

                      return (
                        <tr key={row.value}>
                          <td className="rounded-l-md bg-osu-b5 px-2 py-1.5">
                            <input
                              checked={delivery.mail}
                              className="size-4 accent-osu-h1"
                              onChange={(event) => updateDeliveryField(row.value, "mail", event.currentTarget.checked)}
                              type="checkbox"
                            />
                          </td>
                          <td className="bg-osu-b5 px-2 py-1.5">
                            <input
                              checked={delivery.push}
                              className="size-4 accent-osu-h1"
                              onChange={(event) => updateDeliveryField(row.value, "push", event.currentTarget.checked)}
                              type="checkbox"
                            />
                          </td>
                          <td className="rounded-r-md bg-osu-b5 px-2 py-1.5 text-osu-c1">{row.label}</td>
                        </tr>
                      );
                    })}
                  </tbody>
                </table>
              </div>
            </SettingsEntry>
          </InputGroup>

          <InputGroup>
            <SettingsEntry description="news posts">
              <ToggleOptionGroup
                onValueChange={(value) => {
                  if (Array.isArray(value)) {
                    updateNotificationModes("newsPostSeries", value);
                  }
                }}
                type="multiple"
                value={data.notifications.newsPostSeries}
                values={newsSeriesOptions}
              />
            </SettingsEntry>
          </InputGroup>
          <SaveRow busy={savingSection === "notifications"} label="save" message={sectionMessages.notifications} onSave={saveNotifications} />
        </AccountSection>

        <AccountSection id="options" title="Options">
          <InputGroup>
            <SettingsEntry description="default beatmap download type">
              <ToggleOptionGroup
                onValueChange={(value) => {
                  if (typeof value === "string" && value.length > 0) {
                    setData((current) => ({
                      ...current,
                      options: {
                        ...current.options,
                        beatmapsetDownload: value === "direct" || value === "no_video" ? value : "all",
                      },
                    }));
                  }
                }}
                type="single"
                value={data.options.beatmapsetDownload}
                values={data.isSupporter ? downloadOptions : downloadOptions.filter((option) => option.value !== "direct")}
              />
            </SettingsEntry>
          </InputGroup>

          <InputGroup>
            <SettingsEntry>
              <SwitchControl
                checked={data.options.beatmapsetTitleShowOriginal}
                label="show beatmap metadata in original language"
                onCheckedChange={(checked) => updateOptionField("beatmapsetTitleShowOriginal", checked)}
              />
            </SettingsEntry>
            <SettingsEntry>
              <SwitchControl
                checked={data.options.beatmapsetShowAnimeCover}
                label="show anime style beatmap covers"
                onCheckedChange={(checked) => updateOptionField("beatmapsetShowAnimeCover", checked)}
              />
            </SettingsEntry>
            <SettingsEntry>
              <SwitchControl
                checked={data.options.beatmapsetShowNsfw}
                label="hide warnings for explicit content in beatmaps"
                onCheckedChange={(checked) => updateOptionField("beatmapsetShowNsfw", checked)}
              />
            </SettingsEntry>
          </InputGroup>
          <SaveRow busy={savingSection === "options"} label="save" message={sectionMessages.options} onSave={saveOptions} />
        </AccountSection>

        <AccountSection id="password" title="Password">
          <form onSubmit={submitPassword}>
            <InputGroup>
              <SettingsEntry label="current password">
                <input
                  autoComplete="current-password"
                  className={inputClassName}
                  name="user[current_password]"
                  required
                  type="password"
                />
              </SettingsEntry>
            </InputGroup>
            <InputGroup>
              <SettingsEntry label="new password">
                <input
                  autoComplete="new-password"
                  className={inputClassName}
                  name="user[password]"
                  required
                  type="password"
                />
              </SettingsEntry>
              <SettingsEntry label="password confirmation">
                <input
                  autoComplete="new-password"
                  className={inputClassName}
                  name="user[password_confirmation]"
                  required
                  type="password"
                />
              </SettingsEntry>
            </InputGroup>
            <InputGroup>
              <SettingsEntry>
                <Button type="submit">
                  update
                  <Check data-icon="inline-end" />
                </Button>
                <StatusNotice message={sectionMessages.password} />
              </SettingsEntry>
            </InputGroup>
          </form>
        </AccountSection>

        <AccountSection id="email" title="Email">
          <form onSubmit={submitEmail}>
            <InputGroup>
              <SettingsEntry label="current password">
                <input
                  autoComplete="current-password"
                  className={inputClassName}
                  name="user[current_password]"
                  required
                  type="password"
                />
              </SettingsEntry>
            </InputGroup>
            <InputGroup>
              <SettingsEntry label="new email">
                <input
                  autoComplete="email"
                  className={inputClassName}
                  name="user[user_email]"
                  required
                  type="email"
                />
              </SettingsEntry>
              <SettingsEntry label="email confirmation">
                <input
                  autoComplete="email"
                  className={inputClassName}
                  name="user[user_email_confirmation]"
                  required
                  type="email"
                />
              </SettingsEntry>
            </InputGroup>
            <InputGroup>
              <SettingsEntry>
                <Button type="submit">
                  update
                  <Check data-icon="inline-end" />
                </Button>
                <StatusNotice message={sectionMessages.email} />
              </SettingsEntry>
            </InputGroup>
          </form>
        </AccountSection>

        <AccountSection id="security" title="Security">
          <InputGroup>
            <SettingsEntry label="web sessions" topPinned>
              <div className="grid gap-2">
                {(sessions ?? []).map((session) => (
                  <div
                    className="rounded-md bg-osu-b5 px-3 py-2.5 shadow-[inset_0_0_0_1px_rgba(255,255,255,0.04)]"
                    key={session.id}
                  >
                    <div className="flex flex-wrap items-center gap-2 text-white">
                      <Monitor aria-hidden className="size-4 text-osu-l1" />
                      <span className="font-semibold">{session.client?.name ?? "unknown client"}</span>
                      {session.current ? (
                        <span className="rounded bg-osu-h2 px-1.5 py-0.5 text-[11px] font-bold uppercase leading-none text-white">
                          current
                        </span>
                      ) : null}
                      <Button
                        className="ml-auto"
                        onClick={() => void endSession(session.id, session.current)}
                        size="sm"
                        type="button"
                        variant="outline"
                      >
                        end
                        <X data-icon="inline-end" />
                      </Button>
                    </div>
                    <div className="mt-1 flex flex-wrap items-center gap-x-4 gap-y-1 text-xs text-osu-f1">
                      <span>{session.ip ?? "unknown ip"}</span>
                      <span>Last active: {formatSessionDate(session.last_used_at ?? session.created_at)}</span>
                      <span>Expires: {formatSessionDate(session.expires_at)}</span>
                    </div>
                  </div>
                ))}

                {sessions == null ? (
                  <p className="text-xs text-osu-f1">Loading sessions…</p>
                ) : null}

                {sessions != null && sessions.length === 0 ? (
                  <p className="text-xs text-osu-f1">
                    No other sessions. This browser expires {formattedSessionExpiry}.
                  </p>
                ) : null}

                <StatusNotice message={sectionMessages.sessions} />

                <form action={signOut}>
                  <input name="callbackUrl" type="hidden" value="/" />
                  <Button type="submit" variant="outline">
                    End Session
                    <LogOut data-icon="inline-end" />
                  </Button>
                </form>
              </div>
            </SettingsEntry>
          </InputGroup>
        </AccountSection>
      </PageWrapper>
    </main>
  );
}

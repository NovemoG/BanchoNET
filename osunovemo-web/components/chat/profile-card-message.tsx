import Link from "next/link";
import type { ReactNode } from "react";
import { Bell, Heart, Play, Target, Trophy, UserRound, UsersRound } from "lucide-react";
import { CountryFlag } from "@/components/country-flag";
import { cn } from "@/lib/utils";

type ChatProfileCardGroup = {
  colour?: string | null;
  id: number;
  name: string;
  shortName: string;
};

export type ChatProfileCardUser = {
  avatarUrl?: string | null;
  countryName?: string | null;
  countryCode?: string | null;
  coverUrl?: string | null;
  groups?: ChatProfileCardGroup[];
  id: number;
  isOnline?: boolean | null;
  isSupporter?: boolean | null;
  lastVisit?: string | null;
  playmode?: string | null;
  statistics?: {
    accuracy: number;
    globalRank: number | null;
    playCount: number;
    pp: number;
  } | null;
  supportLevel?: number | null;
  username: string;
};

export type ChatProfileCardPayload = {
  format: "card";
  type: "profile";
  user: ChatProfileCardUser;
  version: 1;
};

const profileCardPayloadType = "profile";
const profileCardPayloadFormat = "card";

function isRecord(value: unknown): value is Record<string, unknown> {
  return typeof value === "object" && value !== null && !Array.isArray(value);
}

function readString(value: unknown) {
  return typeof value === "string" ? value : null;
}

function readNumber(value: unknown) {
  return typeof value === "number" && Number.isFinite(value) ? value : null;
}

function readBoolean(value: unknown) {
  return typeof value === "boolean" ? value : null;
}

function readProfileCardGroups(value: unknown): ChatProfileCardGroup[] {
  if (!Array.isArray(value)) {
    return [];
  }

  return value.flatMap((entry) => {
    if (!isRecord(entry)) {
      return [];
    }

    const id = readNumber(entry.id);
    const name = readString(entry.name);
    const shortName = readString(entry.shortName) ?? readString(entry.short_name);

    if (id == null || name == null || shortName == null) {
      return [];
    }

    return {
      colour: readString(entry.colour) ?? readString(entry.color),
      id,
      name,
      shortName,
    };
  });
}

function normalizeProfileCardUser(value: ChatProfileCardUser): ChatProfileCardUser {
  return {
    avatarUrl: value.avatarUrl ?? null,
    countryName: value.countryName ?? null,
    countryCode: value.countryCode ?? null,
    coverUrl: value.coverUrl ?? value.avatarUrl ?? null,
    groups: value.groups ?? [],
    id: value.id,
    isOnline: value.isOnline ?? null,
    isSupporter: value.isSupporter ?? null,
    lastVisit: value.lastVisit ?? null,
    playmode: value.playmode ?? null,
    statistics: value.statistics == null
      ? null
      : {
          accuracy: value.statistics.accuracy,
          globalRank: value.statistics.globalRank,
          playCount: value.statistics.playCount,
          pp: value.statistics.pp,
        },
    supportLevel: value.supportLevel ?? null,
    username: value.username,
  };
}

function readProfileCardUser(value: unknown): ChatProfileCardUser | null {
  if (!isRecord(value)) {
    return null;
  }

  const id = readNumber(value.id);
  const username = readString(value.username);

  if (id == null || username == null || username.length === 0) {
    return null;
  }

  const statisticsRecord = isRecord(value.statistics) ? value.statistics : null;
  const accuracy = readNumber(statisticsRecord?.accuracy);
  const globalRank = statisticsRecord?.globalRank == null ? null : readNumber(statisticsRecord.globalRank);
  const playCount = readNumber(statisticsRecord?.playCount);
  const pp = readNumber(statisticsRecord?.pp);

  return normalizeProfileCardUser({
    avatarUrl: readString(value.avatarUrl),
    countryName: readString(value.countryName),
    countryCode: readString(value.countryCode),
    coverUrl: readString(value.coverUrl),
    groups: readProfileCardGroups(value.groups),
    id,
    isOnline: readBoolean(value.isOnline),
    isSupporter: readBoolean(value.isSupporter),
    lastVisit: readString(value.lastVisit),
    playmode: readString(value.playmode),
    statistics: accuracy == null || playCount == null || pp == null
      ? null
      : {
          accuracy,
          globalRank,
          playCount,
          pp,
        },
    supportLevel: readNumber(value.supportLevel),
    username,
  });
}

export function serializeChatProfileCardPayload(user: ChatProfileCardUser) {
  return JSON.stringify({
    format: profileCardPayloadFormat,
    type: profileCardPayloadType,
    user: normalizeProfileCardUser(user),
    version: 1,
  } satisfies ChatProfileCardPayload);
}

export function parseChatProfileCardPayload(value: string): ChatProfileCardPayload | null {
  if (!value.trimStart().startsWith("{")) {
    return null;
  }

  try {
    const parsed = JSON.parse(value) as unknown;

    if (!isRecord(parsed)) {
      return null;
    }

    if (
      parsed.type !== profileCardPayloadType ||
      parsed.format !== profileCardPayloadFormat ||
      parsed.version !== 1
    ) {
      return null;
    }

    const user = readProfileCardUser(parsed.user);

    return user == null
      ? null
      : {
          format: profileCardPayloadFormat,
          type: profileCardPayloadType,
          user,
          version: 1,
        };
  } catch {
    return null;
  }
}

function formatProfileInteger(value: number) {
  return new Intl.NumberFormat().format(Math.round(value));
}

const percentageFormatter = new Intl.NumberFormat("en-US", {
  maximumFractionDigits: 2,
  minimumFractionDigits: 2,
  style: "percent",
});

function formatAccuracy(value: number) {
  return percentageFormatter.format(value > 1 ? value / 100 : value);
}

const relativeFormatter = new Intl.RelativeTimeFormat("en-US", { numeric: "auto" });

function formatRelativeTime(value?: string | null) {
  if (value == null || value.length === 0) {
    return null;
  }

  const date = new Date(value);

  if (Number.isNaN(date.getTime())) {
    return null;
  }

  const seconds = Math.round((date.getTime() - Date.now()) / 1000);
  const units: Array<[Intl.RelativeTimeFormatUnit, number]> = [
    ["year", 60 * 60 * 24 * 365],
    ["month", 60 * 60 * 24 * 30],
    ["week", 60 * 60 * 24 * 7],
    ["day", 60 * 60 * 24],
    ["hour", 60 * 60],
    ["minute", 60],
  ];

  for (const [unit, scale] of units) {
    if (Math.abs(seconds) >= scale) {
      return relativeFormatter.format(Math.round(seconds / scale), unit);
    }
  }

  return relativeFormatter.format(seconds, "second");
}

function resolveProfileCardAsset(value?: string | null) {
  if (value == null || value.length === 0) {
    return null;
  }

  return value.startsWith("local:/") ? value.replace("local:", "") : value;
}

function getBackgroundStyle(value: string | null) {
  return value == null ? undefined : { backgroundImage: `url(${JSON.stringify(value)})` };
}

function ProfilePanelStat({
  className,
  icon,
  title,
  value,
}: {
  className?: string;
  icon: ReactNode;
  title: string;
  value: string;
}) {
  return (
    <div className={cn("flex min-w-0 items-center text-xs font-semibold text-osu-c2", className)} title={title}>
      <div className="mr-1 flex size-3 shrink-0 items-center justify-center text-osu-c2 [&_svg]:size-3 [&_svg]:stroke-current">
        {icon}
      </div>
      <div className="truncate text-white">{value}</div>
    </div>
  );
}

function getProfileInfoStats(user: ChatProfileCardUser) {
  const statistics = user.statistics;
  const playmode = getProfileMode(user.playmode);
  const stats: Array<{ icon: React.ReactNode; label: string; value: string }> = [];

  if (statistics == null) {
    return stats;
  }

  stats.push({
    icon: <span className={`fa-extra-mode-${playmode} text-[13px] leading-none`} />,
    label: "pp",
    value: `${formatProfileInteger(statistics.pp)}pp`,
  });

  if (statistics.globalRank != null) {
    stats.push({
      icon: <Trophy aria-hidden className="fill-current" />,
      label: "global rank",
      value: `#${formatProfileInteger(statistics.globalRank)}`,
    });
  }

  stats.push({
    icon: <Play aria-hidden className="fill-current" />,
    label: "play count",
    value: formatProfileInteger(statistics.playCount),
  });
  stats.push({
    icon: <Target aria-hidden className="fill-none" strokeWidth={3} />,
    label: "accuracy",
    value: formatAccuracy(statistics.accuracy),
  });

  return stats;
}

function getProfileMode(value?: string | null) {
  return value === "taiko" || value === "fruits" || value === "mania" ? value : "osu";
}

export function ChatProfileCard({
  payload,
  variant,
}: {
  payload: ChatProfileCardPayload;
  variant: "overlay" | "page";
}) {
  const { user } = payload;
  const avatarUrl = resolveProfileCardAsset(user.avatarUrl);
  const coverUrl = resolveProfileCardAsset(user.coverUrl ?? user.avatarUrl);
  const profileHref = `/users/${encodeURIComponent(String(user.id))}`;
  const countryCode = user.countryCode?.toUpperCase() ?? null;
  const status = user.isOnline === true ? "Online" : "Offline";
  const lastSeen = user.isOnline === true ? null : formatRelativeTime(user.lastVisit);
  const infoStats = getProfileInfoStats(user);
  const playmode = getProfileMode(user.playmode);
  const leftInfoStats = infoStats.filter((stat) => stat.label === "pp" || stat.label === "global rank");
  const rightInfoStats = infoStats.filter((stat) => stat.label === "play count" || stat.label === "accuracy");

  return (
    <div
      className={cn(
        "w-[280px] max-w-full overflow-hidden rounded-[10px] bg-osu-b2 text-white",
        variant === "page" && "sm:w-[300px]",
      )}
    >
      <div className="group/card relative h-[120px] overflow-hidden rounded-[10px] bg-osu-b4">
        <Link aria-label={`${user.username} profile`} className="absolute inset-0" href={profileHref}>
          {coverUrl == null ? null : (
            <span className="absolute inset-0 bg-cover bg-center" style={getBackgroundStyle(coverUrl)} />
          )}
          <span
            className={cn(
              "absolute inset-0 rounded-[10px]",
              user.isOnline === true ? "bg-osu-b5/60" : "bg-osu-b5/70",
            )}
          />
        </Link>

        <div className="pointer-events-none relative z-10 flex h-full flex-col justify-between">
          <div className="grid grid-cols-[60px_minmax(0,1fr)] gap-2.5 p-2.5">
            <div className="flex h-[60px] min-w-0 items-center">
              <Link
                aria-label={`${user.username} profile avatar`}
                className="pointer-events-auto flex size-[60px] shrink-0 items-center justify-center overflow-hidden rounded-md bg-osu-b4 bg-cover bg-center text-lg font-bold text-osu-c1"
                href={profileHref}
                style={getBackgroundStyle(avatarUrl)}
              >
                {avatarUrl == null ? <UserRound aria-hidden className="size-7" /> : null}
              </Link>
            </div>

            <div className="grid min-w-0 grid-rows-[26px_minmax(0,1fr)]">
              <div className="flex min-w-0 items-center gap-1.5">
                {countryCode == null ? null : (
                  <Link
                    className="pointer-events-auto block text-[26px] leading-none"
                    href={`/rankings/${playmode}/global/performance?country=${encodeURIComponent(countryCode)}`}
                    title={user.countryName ?? countryCode}
                  >
                    <CountryFlag code={countryCode} name={user.countryName} />
                  </Link>
                )}
                {user.isSupporter === true ? (
                  <Link
                    aria-label="supporter"
                    className="pointer-events-auto flex size-[26px] items-center justify-center rounded-full bg-osu-h2 text-white"
                    href="https://osu.ppy.sh/home/support"
                  >
                    <Heart aria-hidden className="size-3.5 fill-current" />
                  </Link>
                ) : null}
                <span className="flex size-[26px] items-center justify-center rounded-full bg-osu-h2 text-white" title="user">
                  <UsersRound aria-hidden className="size-3.5" />
                </span>
                <span className="flex size-[26px] items-center justify-center rounded-full bg-osu-b6 text-white" title="notifications">
                  <Bell aria-hidden className="size-3.5 fill-current" />
                </span>
              </div>

              <div className="flex min-w-0 items-center">
                <Link
                  className="pointer-events-auto truncate text-sm font-semibold text-white transition-colors hover:text-osu-l1"
                  href={profileHref}
                >
                  {user.username}
                </Link>
                {user.groups?.slice(0, 2).map((group) => (
                  <span
                    key={group.id}
                    className="ml-1 rounded-sm px-1 text-[10px] font-bold text-white"
                    style={{ backgroundColor: group.colour ?? "hsl(var(--hsl-b6))" }}
                    title={group.name}
                  >
                    {group.shortName}
                  </span>
                ))}
              </div>
            </div>
          </div>

          <div className="flex min-w-0 items-center justify-between p-2.5 pt-0">
            <div className="flex min-w-0 items-center">
              <div className="flex w-[60px] shrink-0 items-center justify-center">
                <span
                  className={cn(
                    "size-[25px] rounded-full border-4 border-black",
                    user.isOnline === true && "border-osu-green-1 bg-osu-b6",
                  )}
                />
              </div>
              <div className="ml-2.5 min-w-0 text-sm font-semibold leading-4">
                <div className="truncate text-xs text-white">
                  {lastSeen == null ? "\u00A0" : `Last seen ${lastSeen}`}
                </div>
                <div className="truncate text-white">{status}</div>
              </div>
            </div>
          </div>
        </div>
      </div>

      {infoStats.length === 0 ? null : (
        <div className="flex min-w-0 items-center justify-between gap-3 bg-osu-b2 px-3 py-2">
          <div className="flex min-w-0 items-center gap-3">
            {leftInfoStats.map((stat) => (
              <ProfilePanelStat key={stat.label} icon={stat.icon} title={stat.label} value={stat.value} />
            ))}
            {user.isSupporter === true && user.supportLevel != null && user.supportLevel > 0 ? (
              <ProfilePanelStat
                icon={<Heart aria-hidden />}
                title="supporter"
                value={`lv. ${user.supportLevel}`}
              />
            ) : null}
          </div>
          <div className="ml-auto flex min-w-0 items-center justify-end gap-3">
            {rightInfoStats.map((stat) => (
              <ProfilePanelStat key={stat.label} icon={stat.icon} title={stat.label} value={stat.value} />
            ))}
          </div>
          <span className="sr-only">{playmode}</span>
        </div>
      )}
    </div>
  );
}

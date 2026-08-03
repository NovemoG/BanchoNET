import type { ReactNode } from "react";
import Link from "next/link";
import type { ChatUser } from "@/lib/chat/types";

const osuLinkPattern = /\[(https:\/\/osu\.novemo\.dev\/(b|u|users)\/(\d+))\s+([^\]\n]+)\]/g;
const mentionPattern = /(^|[^\w/@.])@([A-Za-z0-9_-]{1,32})/g;
const urlPattern = /(https?:\/\/[^\s<\]]+)/g;

export type ChatMentionUser = Pick<ChatUser, "id" | "username">;

function renderTextWithLineBreaks(value: string, keyPrefix: string) {
  return value.split("\n").flatMap((line, lineIndex, lines) => {
    const key = `${keyPrefix}-line-${lineIndex}`;

    if (lineIndex === lines.length - 1) {
      return <span key={key}>{line}</span>;
    }

    return [
      <span key={key}>{line}</span>,
      <br key={`${key}-br`} />,
    ];
  });
}

function renderTextWithMentions(
  value: string,
  keyPrefix: string,
  usersByUsername: ReadonlyMap<string, ChatMentionUser>,
) {
  const nodes: ReactNode[] = [];
  let lastIndex = 0;
  let match: RegExpExecArray | null;

  mentionPattern.lastIndex = 0;

  while ((match = mentionPattern.exec(value)) != null) {
    const [, prefix, username] = match;
    const user = usersByUsername.get(username.toLowerCase());

    if (user == null) {
      continue;
    }

    const mentionStart = match.index + prefix.length;
    const mentionEnd = mentionStart + username.length + 1;

    if (mentionStart > lastIndex) {
      nodes.push(...renderTextWithLineBreaks(value.slice(lastIndex, mentionStart), `${keyPrefix}-text-${lastIndex}`));
    }

    nodes.push(
      <Link
        key={`${keyPrefix}-mention-${mentionStart}-${user.id}`}
        className="text-osu-h1 hover:text-white"
        href={`/users/${encodeURIComponent(String(user.id))}`}
      >
        {value.slice(mentionStart, mentionEnd)}
      </Link>,
    );

    lastIndex = mentionEnd;
  }

  if (lastIndex < value.length) {
    nodes.push(...renderTextWithLineBreaks(value.slice(lastIndex), `${keyPrefix}-text-${lastIndex}`));
  }

  return nodes;
}

function renderPlainTextWithUrls(
  value: string,
  keyPrefix: string,
  usersByUsername: ReadonlyMap<string, ChatMentionUser>,
) {
  return value.split(urlPattern).flatMap((part, index) => {
    if (part.match(urlPattern) != null) {
      return (
        <a
          key={`${keyPrefix}-url-${index}-${part}`}
          className="text-osu-h1 hover:text-white"
          href={part}
          rel="noreferrer"
          target="_blank"
        >
          {part}
        </a>
      );
    }

    return renderTextWithMentions(part, `${keyPrefix}-text-${index}`, usersByUsername);
  });
}

export function renderChatTextWithLinks(
  value: string,
  usersByUsername: ReadonlyMap<string, ChatMentionUser> = new Map(),
): ReactNode[] {
  const nodes: ReactNode[] = [];
  let lastIndex = 0;
  let match: RegExpExecArray | null;

  osuLinkPattern.lastIndex = 0;

  while ((match = osuLinkPattern.exec(value)) != null) {
    const [raw, href, type, id, label] = match;
    const internalHref = type === "b" ? null : `/users/${encodeURIComponent(id)}`;

    if (match.index > lastIndex) {
      nodes.push(...renderPlainTextWithUrls(value.slice(lastIndex, match.index), `plain-${lastIndex}`, usersByUsername));
    }

    nodes.push(
      internalHref == null ? (
        <a
          key={`osu-link-${match.index}-${href}`}
          className="text-osu-h1 hover:text-white"
          href={href}
          rel="noreferrer"
          target="_blank"
        >
          {label}
        </a>
      ) : (
        <Link
          key={`osu-link-${match.index}-${href}`}
          className="text-osu-h1 hover:text-white"
          href={internalHref}
        >
          {label}
        </Link>
      ),
    );

    lastIndex = match.index + raw.length;
  }

  if (lastIndex < value.length) {
    nodes.push(...renderPlainTextWithUrls(value.slice(lastIndex), `plain-${lastIndex}`, usersByUsername));
  }

  return nodes;
}

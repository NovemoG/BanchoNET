"use client";

import {
  createContext,
  type ReactNode,
  useCallback,
  useContext,
  useEffect,
  useMemo,
  useState,
} from "react";
import type { AuthUser, ClientSession } from "@/lib/auth/types";
import { parseApiErrorBody } from "@/lib/osu-api-errors";

type SessionStatus = "authenticated" | "loading" | "unauthenticated";

type SessionContextValue = {
  data: ClientSession | null;
  status: SessionStatus;
  update: () => Promise<ClientSession | null>;
};

type SignInOptions = {
  callbackUrl?: string;
  password: string;
  redirect?: boolean;
  username: string;
};

type SignOutOptions = {
  callbackUrl?: string;
  redirect?: boolean;
};

const SessionContext = createContext<SessionContextValue | null>(null);
const meStorageKey = "me";

function writeStoredMe(user: AuthUser | null) {
  if (typeof window === "undefined") {
    return;
  }

  try {
    if (user == null) {
      window.localStorage.removeItem(meStorageKey);
      return;
    }

    window.localStorage.setItem(meStorageKey, JSON.stringify(user));
  } catch {
    // localStorage can be unavailable in private or locked-down browser contexts.
  }
}

async function fetchSession() {
  const response = await fetch("/api/auth/session", {
    cache: "no-store",
  });

  if (!response.ok) {
    return null;
  }

  return (await response.json()) as ClientSession | null;
}

async function fetchMe() {
  const response = await fetch("/api/auth/me", {
    cache: "no-store",
  });

  if (!response.ok) {
    return null;
  }

  return (await response.json()) as AuthUser | null;
}

async function syncStoredMe(session: ClientSession | null) {
  if (session == null) {
    writeStoredMe(null);
    return;
  }

  try {
    const user = await fetchMe();
    writeStoredMe(user ?? session.user);
  } catch {
    writeStoredMe(session.user);
  }
}

export function SessionProvider({
  children,
  initialSession = null,
}: {
  children: ReactNode;
  initialSession?: ClientSession | null;
}) {
  const [session, setSession] = useState<ClientSession | null>(initialSession);
  const [status, setStatus] = useState<SessionStatus>(
    initialSession == null ? "loading" : "authenticated",
  );

  const update = useCallback(async () => {
    setStatus((current) => (current === "authenticated" ? current : "loading"));
    const nextSession = await fetchSession();

    setSession(nextSession);
    setStatus(nextSession == null ? "unauthenticated" : "authenticated");
    await syncStoredMe(nextSession);

    return nextSession;
  }, []);

  useEffect(() => {
    let active = true;

    fetchSession()
      .then((nextSession) => {
        if (!active) {
          return;
        }

        setSession(nextSession);
        setStatus(nextSession == null ? "unauthenticated" : "authenticated");
        void syncStoredMe(nextSession);
      })
      .catch(() => {
        if (!active) {
          return;
        }

        setSession(null);
        setStatus("unauthenticated");
        writeStoredMe(null);
      });

    return () => {
      active = false;
    };
  }, []);

  const value = useMemo<SessionContextValue>(
    () => ({
      data: session,
      status,
      update,
    }),
    [session, status, update],
  );

  return <SessionContext.Provider value={value}>{children}</SessionContext.Provider>;
}

export function useSession() {
  const value = useContext(SessionContext);

  if (value == null) {
    throw new Error("useSession must be used within SessionProvider.");
  }

  return value;
}

export async function signIn(provider: "credentials", options: SignInOptions) {
  if (provider !== "credentials") {
    return {
      error: "Unsupported provider.",
      ok: false,
      status: 400,
      url: null,
    };
  }

  const response = await fetch("/api/auth/signin", {
    body: JSON.stringify({
      password: options.password,
      username: options.username,
    }),
    cache: "no-store",
    headers: {
      "Content-Type": "application/json",
    },
    method: "POST",
  });

  const callbackUrl = options.callbackUrl ?? window.location.href;

  if (response.ok) {
    await syncStoredMe((await response.clone().json()) as ClientSession);
  }

  if (response.ok && options.redirect !== false) {
    window.location.assign(callbackUrl);
  }

  return {
    error: response.ok ? null : ((await response.json()) as { message?: string }).message ?? "Sign in failed.",
    ok: response.ok,
    status: response.status,
    url: response.ok ? callbackUrl : null,
  };
}

export type RegisterResult = {
  /** Keyed by field name as the API reports it: username, user_email, password. */
  fieldErrors: Record<string, string[]>;
  message: string | null;
  ok: boolean;
};

/**
 * Creates an account and, unless `check` is set, signs straight in with it.
 *
 * `check: true` runs the API's validation-only pass so fields can be checked while the user is
 * still typing without creating anything.
 */
export async function register(options: {
  check?: boolean;
  email: string;
  password: string;
  username: string;
}): Promise<RegisterResult> {
  const response = await fetch("/api/auth/register", {
    body: JSON.stringify({
      check: options.check === true,
      email: options.email,
      password: options.password,
      username: options.username,
    }),
    cache: "no-store",
    headers: { "Content-Type": "application/json" },
    method: "POST",
  });

  if (response.ok) {
    if (options.check !== true) {
      await syncStoredMe((await response.clone().json()) as ClientSession);
    }

    return { fieldErrors: {}, message: null, ok: true };
  }

  const parsed = parseApiErrorBody(await response.text(), "Registration failed.");

  return {
    // parseApiErrorBody keys these as "user[username]"; the form wants the bare field name.
    fieldErrors: Object.fromEntries(
      Object.entries(parsed.fields).map(([key, messages]) => [
        /\[([^\]]+)\]$/.exec(key)?.[1] ?? key,
        messages,
      ]),
    ),
    message: parsed.message,
    ok: false,
  };
}

export async function signOut(options: SignOutOptions = {}) {
  const response = await fetch("/api/auth/signout", {
    cache: "no-store",
    method: "POST",
  });
  const callbackUrl = options.callbackUrl ?? window.location.href;

  writeStoredMe(null);

  if (options.redirect !== false) {
    window.location.assign(callbackUrl);
  }

  return {
    ok: response.ok,
    status: response.status,
    url: callbackUrl,
  };
}

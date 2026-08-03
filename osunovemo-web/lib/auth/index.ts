import "server-only";

import { cache } from "react";
import {
  AuthRequestError,
  fetchCurrentUser,
  requestPasswordToken,
  type TokenRequestOrigin,
  requestRefreshToken,
} from "@/lib/auth/osu-auth-client";
import {
  clearAuthSession,
  getAuthSession,
  isSessionExpired,
  setAuthSession,
  toClientSession,
} from "@/lib/auth/session";
import type { AuthActionState, AuthSession, ClientSession } from "@/lib/auth/types";

function getExpiresAt(expiresInSeconds: number) {
  return Date.now() + Math.max(0, expiresInSeconds) * 1000;
}

function compactUserForSession(user: AuthSession["user"]): AuthSession["user"] {
  return {
    avatar_url: user.avatar_url,
    country_code: user.country_code,
    id: user.id,
    is_supporter: user.is_supporter,
    session_verification_method: user.session_verification_method,
    session_verified: user.session_verified,
    username: user.username,
  };
}

function normalizeAuthError(error: unknown) {
  if (error instanceof AuthRequestError) {
    if (error.code === "invalid_grant" || error.status === 401 || error.status === 403) {
      return "Incorrect username or password.";
    }

    if (error.code === "invalid_request" || error.status === 400 || error.status === 422) {
      return error.message;
    }

    return "Authentication service is unavailable.";
  }

  return "Could not sign in. Try again in a moment.";
}

export const auth = cache(async (): Promise<ClientSession | null> => {
  return toClientSession(await getAuthSession());
});

export async function createSessionFromPassword({
  origin,
  password,
  username,
}: {
  /** Forwarded to the API so the session records the browser, not this server. */
  origin?: TokenRequestOrigin;
  password: string;
  username: string;
}) {
  const tokens = await requestPasswordToken({ origin, password, username });
  const user = await fetchCurrentUser(tokens.access_token);
  const session: AuthSession = {
    accessToken: tokens.access_token,
    demoSessionCode: tokens.demo_session_code,
    expiresAt: getExpiresAt(tokens.expires_in),
    refreshToken: tokens.refresh_token,
    scope: tokens.scope,
    tokenType: tokens.token_type,
    user: compactUserForSession(user),
  };

  await setAuthSession(session);

  return toClientSession(session);
}

export async function refreshAuthSession() {
  const currentSession = await getAuthSession();

  if (currentSession == null) {
    return null;
  }

  if (!isSessionExpired(currentSession, 60_000)) {
    return toClientSession(currentSession);
  }

  try {
    const tokens = await requestRefreshToken(currentSession.refreshToken);
    const user = await fetchCurrentUser(tokens.access_token);
    const session: AuthSession = {
      accessToken: tokens.access_token,
      demoSessionCode: tokens.demo_session_code ?? currentSession.demoSessionCode,
      expiresAt: getExpiresAt(tokens.expires_in),
      refreshToken: tokens.refresh_token,
      scope: tokens.scope ?? currentSession.scope,
      tokenType: tokens.token_type,
      user: compactUserForSession(user),
    };

    await setAuthSession(session);

    return toClientSession(session);
  } catch {
    await clearAuthSession();

    return null;
  }
}

export async function getCurrentAuthUser() {
  let currentSession = await getAuthSession();

  if (currentSession == null) {
    return null;
  }

  if (isSessionExpired(currentSession, 60_000)) {
    await refreshAuthSession();
    currentSession = await getAuthSession();

    if (currentSession == null) {
      return null;
    }
  }

  try {
    return await fetchCurrentUser(currentSession.accessToken);
  } catch (error) {
    if (error instanceof AuthRequestError && (error.status === 401 || error.status === 403)) {
      await clearAuthSession();
    }

    throw error;
  }
}

export async function destroySession() {
  await clearAuthSession();
}

export function buildIdleAuthActionState(): AuthActionState {
  return {
    status: "idle",
  };
}

export function buildAuthErrorState(error: unknown): AuthActionState {
  return {
    message: normalizeAuthError(error),
    status: "error",
  };
}

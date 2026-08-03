import "server-only";

import { osuApiBaseUrl, getOsuApiOrigin } from "@/lib/osu-api-common";
import type { AuthUser, OAuthTokenResponse } from "@/lib/auth/types";

// chat.read/chat.write are required by ChatController; without them the chat endpoints 403 and the
// realtime bridge fails, which used to show up as a reconnect loop rather than an auth error.
const defaultScope =
  process.env.OSU_AUTH_SCOPE ?? "public identify friends.read chat.read chat.write";
const defaultClientId = process.env.OSU_OAUTH_CLIENT_ID ?? "5";
const defaultClientSecret = process.env.OSU_OAUTH_CLIENT_SECRET ?? "novemo-web";

export class AuthRequestError extends Error {
  constructor(
    message: string,
    readonly status: number,
    readonly code?: string,
  ) {
    super(message);
    this.name = "AuthRequestError";
  }
}

function getTokenUrl() {
  return new URL("/oauth/token", `${getOsuApiOrigin()}/`);
}

function getMeUrl() {
  return new URL("me", `${osuApiBaseUrl}/`);
}

function appendClientCredentials(body: URLSearchParams) {
  body.set("client_id", defaultClientId);
  body.set("client_secret", defaultClientSecret);
}

async function parseErrorResponse(response: Response) {
  const text = await response.text();

  try {
    const json = JSON.parse(text) as {
      error?: string;
      hint?: string;
      message?: string;
      errors?: Record<string, string[]>;
    };

    if (json.errors != null) {
      const messages = Object.entries(json.errors).flatMap(([field, errors]) =>
        errors.map((message) => `${field}: ${message}`),
      );

      if (messages.length > 0) {
        return {
          code: json.error,
          message: messages.join(" "),
        };
      }
    }

    return {
      code: json.error,
      message: json.hint ?? json.message ?? json.error ?? response.statusText,
    };
  } catch {
    return {
      code: undefined,
      message: text.length > 0 ? text.slice(0, 280) : response.statusText,
    };
  }
}

/**
 * Who the token is really being issued to. Token requests are made by this server on the
 * browser's behalf, so without forwarding these the API records every web session as coming from
 * the container: the sessions list showed "node" for everyone, and sessions from different
 * browsers were indistinguishable.
 */
export type TokenRequestOrigin = {
  ip?: string | null;
  userAgent?: string | null;
};

async function requestToken(
  body: URLSearchParams,
  origin?: TokenRequestOrigin,
): Promise<OAuthTokenResponse> {
  appendClientCredentials(body);

  const headers: Record<string, string> = {
    Accept: "application/json",
    "Content-Type": "application/x-www-form-urlencoded",
  };

  if (origin?.userAgent != null && origin.userAgent.length > 0) {
    headers["User-Agent"] = origin.userAgent;
  }

  if (origin?.ip != null && origin.ip.length > 0) {
    headers["X-Forwarded-For"] = origin.ip;
  }

  const response = await fetch(getTokenUrl(), {
    body,
    cache: "no-store",
    headers,
    method: "POST",
  });

  if (!response.ok) {
    const error = await parseErrorResponse(response);
    throw new AuthRequestError(error.message, response.status, error.code);
  }

  return (await response.json()) as OAuthTokenResponse;
}

export function requestPasswordToken({
  origin,
  password,
  scope = defaultScope,
  username,
}: {
  origin?: TokenRequestOrigin;
  password: string;
  scope?: string;
  username: string;
}) {
  const body = new URLSearchParams({
    grant_type: "password",
    password,
    username,
  });

  if (scope.length > 0) {
    body.set("scope", scope);
  }

  return requestToken(body, origin);
}

let cachedClientToken: { expiresAt: number; token: string } | null = null;

/**
 * App level token for reading public data without a signed in user. Every api/v2 endpoint is
 * [Authorize]d, so an anonymous visitor cannot read rankings or profiles at all without one — the
 * public read paths are [AllowClientCredentials] precisely so this works.
 *
 * Cached in process and refreshed a minute early; it is a server only module, so this never
 * reaches the browser.
 */
export async function getClientCredentialsToken(): Promise<string> {
  const now = Date.now();

  if (cachedClientToken != null && cachedClientToken.expiresAt > now) {
    return cachedClientToken.token;
  }

  const response = await requestToken(
    new URLSearchParams({ grant_type: "client_credentials", scope: "public" }),
  );

  cachedClientToken = {
    expiresAt: now + Math.max(0, (response.expires_in ?? 3600) - 60) * 1000,
    token: response.access_token,
  };

  return cachedClientToken.token;
}

export function requestRefreshToken(refreshToken: string) {
  const body = new URLSearchParams({
    grant_type: "refresh_token",
    refresh_token: refreshToken,
  });

  if (defaultScope.length > 0) {
    body.set("scope", defaultScope);
  }

  return requestToken(body);
}

export async function fetchCurrentUser(accessToken: string): Promise<AuthUser> {
  const response = await fetch(getMeUrl(), {
    cache: "no-store",
    headers: {
      Accept: "application/json",
      Authorization: `Bearer ${accessToken}`,
    },
  });

  if (!response.ok) {
    const error = await parseErrorResponse(response);
    throw new AuthRequestError(error.message, response.status, error.code);
  }

  return (await response.json()) as AuthUser;
}

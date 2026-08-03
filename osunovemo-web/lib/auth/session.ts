import "server-only";

import { cookies } from "next/headers";
import type { AuthSession, ClientSession } from "@/lib/auth/types";

const cookieName = "osunovemo.session";
const cookieMaxAgeSeconds = 60 * 60 * 24 * 30;
const cookieVersion = "v1";
const encoder = new TextEncoder();
const decoder = new TextDecoder();

function getAuthSecret() {
  const secret =
    process.env.OSU_AUTH_SECRET ??
    process.env.AUTH_SECRET ??
    process.env.NEXTAUTH_SECRET;

  if (secret != null && secret.length >= 32) {
    return secret;
  }

  if (process.env.NODE_ENV === "production") {
    throw new Error("Missing OSU_AUTH_SECRET, AUTH_SECRET, or NEXTAUTH_SECRET with at least 32 characters.");
  }

  return "development-only-osunovemo-auth-secret-change-before-production";
}

async function getSessionKey() {
  const digest = await crypto.subtle.digest("SHA-256", encoder.encode(getAuthSecret()));

  return crypto.subtle.importKey(
    "raw",
    digest,
    { name: "AES-GCM" },
    false,
    ["decrypt", "encrypt"],
  );
}

function encodeBase64Url(bytes: Uint8Array) {
  return Buffer.from(bytes).toString("base64url");
}

function decodeBase64Url(value: string) {
  return new Uint8Array(Buffer.from(value, "base64url"));
}

async function sealSession(session: AuthSession) {
  const iv = crypto.getRandomValues(new Uint8Array(12));
  const key = await getSessionKey();
  const payload = encoder.encode(JSON.stringify(session));
  const encrypted = await crypto.subtle.encrypt(
    {
      iv,
      name: "AES-GCM",
    },
    key,
    payload,
  );

  return `${cookieVersion}.${encodeBase64Url(iv)}.${encodeBase64Url(new Uint8Array(encrypted))}`;
}

async function openSession(value?: string) {
  if (value == null || value.length === 0) {
    return null;
  }

  const [version, iv, encrypted] = value.split(".");

  if (version !== cookieVersion || iv == null || encrypted == null) {
    return null;
  }

  try {
    const key = await getSessionKey();
    const decrypted = await crypto.subtle.decrypt(
      {
        iv: decodeBase64Url(iv),
        name: "AES-GCM",
      },
      key,
      decodeBase64Url(encrypted),
    );

    return JSON.parse(decoder.decode(decrypted)) as AuthSession;
  } catch {
    return null;
  }
}

export function isSessionExpired(session: AuthSession, leewayMs = 30_000) {
  return session.expiresAt <= Date.now() + leewayMs;
}

export async function getAuthSession() {
  const cookieStore = await cookies();

  return openSession(cookieStore.get(cookieName)?.value);
}

export async function getAccessToken() {
  const session = await getAuthSession();

  if (session == null || isSessionExpired(session)) {
    return null;
  }

  return session.accessToken;
}

export function toClientSession(session: AuthSession | null): ClientSession | null {
  if (session == null || isSessionExpired(session)) {
    return null;
  }

  return {
    expires: new Date(session.expiresAt).toISOString(),
    user: session.user,
  };
}

export async function setAuthSession(session: AuthSession) {
  const cookieStore = await cookies();
  const secure = process.env.NODE_ENV === "production";

  cookieStore.set(cookieName, await sealSession(session), {
    httpOnly: true,
    maxAge: cookieMaxAgeSeconds,
    path: "/",
    sameSite: "lax",
    secure,
  });
}

export async function clearAuthSession() {
  const cookieStore = await cookies();

  cookieStore.delete(cookieName);
}

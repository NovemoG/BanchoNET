"use server";

import { redirect } from "next/navigation";
import {
  buildAuthErrorState,
  createSessionFromPassword,
  destroySession,
  refreshAuthSession,
} from "@/lib/auth";
import type { AuthActionState } from "@/lib/auth/types";

function getFormString(formData: FormData, key: string) {
  const value = formData.get(key);

  return typeof value === "string" ? value.trim() : "";
}

function getCallbackUrl(formData?: FormData) {
  const value = formData == null ? null : formData.get("callbackUrl");

  if (typeof value !== "string" || value.length === 0) {
    return "/";
  }

  try {
    const url = new URL(value, "http://localhost");

    return `${url.pathname}${url.search}${url.hash}`;
  } catch {
    return "/";
  }
}

export async function signIn(
  _state: AuthActionState | undefined,
  formData: FormData,
): Promise<AuthActionState> {
  void _state;

  const username = getFormString(formData, "username");
  const password = getFormString(formData, "password");
  const fieldErrors: AuthActionState["fieldErrors"] = {};

  if (username.length === 0) {
    fieldErrors.username = "Username is required.";
  }

  if (password.length === 0) {
    fieldErrors.password = "Password is required.";
  }

  if (Object.keys(fieldErrors).length > 0) {
    return {
      fieldErrors,
      status: "error",
    };
  }

  try {
    await createSessionFromPassword({ password, username });
  } catch (error) {
    return buildAuthErrorState(error);
  }

  redirect(getCallbackUrl(formData));
}

export async function signOut(formData?: FormData) {
  await destroySession();
  redirect(getCallbackUrl(formData));
}

export async function updateSession() {
  return refreshAuthSession();
}

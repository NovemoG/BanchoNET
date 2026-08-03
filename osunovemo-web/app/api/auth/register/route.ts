import type { NextRequest } from "next/server";
import { buildAuthErrorState, createSessionFromPassword } from "@/lib/auth";
import { getOsuApiOrigin } from "@/lib/osu-api-common";
import { getRequestOrigin } from "@/lib/auth/request-origin";

export const dynamic = "force-dynamic";
export const runtime = "nodejs";

type RegisterInput = {
  check: boolean;
  email: string;
  password: string;
  username: string;
};

async function readInput(request: NextRequest): Promise<RegisterInput> {
  const body = (await request.json()) as Record<string, unknown>;
  const read = (key: string) => (typeof body[key] === "string" ? (body[key] as string) : "");

  return {
    // Validation only pass, used while the user is still typing.
    check: body.check === true,
    email: read("email"),
    password: read("password"),
    username: read("username"),
  };
}

export async function POST(request: NextRequest) {
  const input = await readInput(request);

  // Registration predates api/v2 and lives at /users with osu-web's bracket field names.
  const body = new URLSearchParams({
    "user[username]": input.username,
    "user[user_email]": input.email,
    "user[password]": input.password,
    Check: input.check ? "1" : "0",
  });

  const response = await fetch(new URL("/users", `${getOsuApiOrigin()}/`), {
    body,
    cache: "no-store",
    headers: {
      Accept: "application/json",
      "Content-Type": "application/x-www-form-urlencoded",
    },
    method: "POST",
  });

  const text = await response.text();

  if (!response.ok) {
    // Already osu-web's {"form_error": {"user": {...}}} shape, so pass it through untouched for
    // the shared error parser to pick apart.
    return new Response(text, {
      headers: { "Content-Type": response.headers.get("content-type") ?? "application/json" },
      status: response.status,
    });
  }

  if (input.check) {
    return Response.json({ status: "ok" });
  }

  try {
    // The account exists now, so sign straight in rather than bouncing to the login form.
    return Response.json(
      await createSessionFromPassword({
        origin: getRequestOrigin(request),
        password: input.password,
        username: input.username,
      }),
    );
  } catch (error) {
    return Response.json(buildAuthErrorState(error), { status: 401 });
  }
}

import type { NextRequest } from "next/server";
import { buildAuthErrorState, createSessionFromPassword } from "@/lib/auth";
import { getRequestOrigin } from "@/lib/auth/request-origin";

async function readCredentials(request: NextRequest) {
  const contentType = request.headers.get("content-type") ?? "";

  if (contentType.includes("application/json")) {
    const body = (await request.json()) as {
      password?: unknown;
      username?: unknown;
    };

    return {
      password: typeof body.password === "string" ? body.password : "",
      username: typeof body.username === "string" ? body.username : "",
    };
  }

  const formData = await request.formData();

  return {
    password: typeof formData.get("password") === "string" ? String(formData.get("password")) : "",
    username: typeof formData.get("username") === "string" ? String(formData.get("username")) : "",
  };
}

export async function POST(request: NextRequest) {
  const credentials = await readCredentials(request);

  if (credentials.username.trim().length === 0 || credentials.password.length === 0) {
    return Response.json(
      {
        message: "Username and password are required.",
        status: "error",
      },
      { status: 400 },
    );
  }

  try {
    const session = await createSessionFromPassword({
      ...credentials,
      origin: getRequestOrigin(request),
    });

    return Response.json(session);
  } catch (error) {
    return Response.json(buildAuthErrorState(error), { status: 401 });
  }
}

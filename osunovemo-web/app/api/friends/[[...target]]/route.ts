import type { NextRequest } from "next/server";
import { refreshAuthSession } from "@/lib/auth";
import { getAccessToken } from "@/lib/auth/session";
import { osuApiBaseUrl } from "@/lib/osu-api-common";

export const dynamic = "force-dynamic";
export const runtime = "nodejs";

type FriendRouteContext = {
  params: Promise<{ target?: string[] }>;
};

async function authorize() {
  await refreshAuthSession();

  return getAccessToken();
}

export async function POST(request: NextRequest) {
  const accessToken = await authorize();

  if (accessToken == null) {
    return Response.json({ error: "Unauthorized." }, { status: 401 });
  }

  const target = request.nextUrl.searchParams.get("target");

  if (target == null || !/^\d+$/.test(target)) {
    return Response.json({ error: "Invalid target." }, { status: 400 });
  }

  const url = new URL("friends", `${osuApiBaseUrl}/`);
  url.searchParams.set("target", target);

  const response = await fetch(url, {
    cache: "no-store",
    headers: { Accept: "application/json", Authorization: `Bearer ${accessToken}` },
    method: "POST",
  });

  return new Response(await response.text(), {
    headers: { "Content-Type": response.headers.get("content-type") ?? "application/json" },
    status: response.status,
  });
}

export async function DELETE(_request: NextRequest, context: FriendRouteContext) {
  const accessToken = await authorize();

  if (accessToken == null) {
    return Response.json({ error: "Unauthorized." }, { status: 401 });
  }

  const target = (await context.params).target?.[0];

  if (target == null || !/^\d+$/.test(target)) {
    return Response.json({ error: "Invalid target." }, { status: 400 });
  }

  const response = await fetch(new URL(`friends/${target}`, `${osuApiBaseUrl}/`), {
    cache: "no-store",
    headers: { Accept: "application/json", Authorization: `Bearer ${accessToken}` },
    method: "DELETE",
  });

  return new Response(null, { status: response.ok ? 204 : response.status });
}

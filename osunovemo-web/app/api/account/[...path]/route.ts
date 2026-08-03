import type { NextRequest } from "next/server";
import { refreshAuthSession } from "@/lib/auth";
import { getAccessToken } from "@/lib/auth/session";
import { osuApiBaseUrl } from "@/lib/osu-api-common";

export const dynamic = "force-dynamic";
export const runtime = "nodejs";

const allowedPathPatterns = [
  /^settings$/,
  /^account$/,
  /^account\/avatar$/,
  /^account\/cover$/,
  /^account\/options$/,
  /^account\/notification-options$/,
  /^account\/password$/,
  /^account\/email$/,
  /^account\/sessions$/,
  /^account\/sessions\/[0-9a-fA-F-]{36}$/,
];

type AccountRouteContext = {
  params: Promise<{
    path: string[];
  }>;
};

function isAllowedAccountPath(path: string) {
  return allowedPathPatterns.some((pattern) => pattern.test(path));
}

function buildTargetUrl(path: string, request: NextRequest) {
  const url = new URL(`me/${path}`, `${osuApiBaseUrl}/`);

  for (const [key, value] of request.nextUrl.searchParams.entries()) {
    url.searchParams.append(key, value);
  }

  return url;
}

type ProxyBody = {
  body: BodyInit;
  contentType: string | null;
  duplex?: "half";
};

async function getRequestBody(request: NextRequest): Promise<ProxyBody | null> {
  if (request.method === "GET" || request.method === "HEAD") {
    return null;
  }

  const contentType = request.headers.get("content-type") ?? "";

  // Multipart has to stream through untouched so the boundary and the file parts survive.
  // Re-encoding it as urlencoded (which the chat proxy does) silently drops every File.
  if (contentType.includes("multipart/form-data")) {
    return {
      body: request.body as BodyInit,
      contentType,
      duplex: "half",
    };
  }

  const text = await request.text();

  if (text.length === 0) {
    return null;
  }

  return {
    body: text,
    contentType: contentType.length > 0 ? contentType : "application/x-www-form-urlencoded",
  };
}

async function proxyAccountRequest(
  request: NextRequest,
  context: AccountRouteContext,
) {
  await refreshAuthSession();

  const accessToken = await getAccessToken();

  if (accessToken == null) {
    return Response.json({ error: "Unauthorized." }, { status: 401 });
  }

  const { path: pathSegments } = await context.params;
  const decodedPath = pathSegments.join("/");

  if (!isAllowedAccountPath(decodedPath)) {
    return Response.json({ error: "Unsupported account endpoint." }, { status: 404 });
  }

  const body = await getRequestBody(request);
  const headers: Record<string, string> = {
    Accept: "application/json",
    Authorization: `Bearer ${accessToken}`,
  };

  if (body?.contentType != null && body.contentType.length > 0) {
    headers["Content-Type"] = body.contentType;
  }

  const targetUrl = buildTargetUrl(
    pathSegments.map((segment) => encodeURIComponent(segment)).join("/"),
    request,
  );

  const upstreamResponse = await fetch(targetUrl, {
    body: body?.body,
    cache: "no-store",
    headers,
    method: request.method,
    ...(body?.duplex === "half" ? { duplex: "half" } : {}),
  } as RequestInit);

  if (upstreamResponse.status === 204) {
    return new Response(null, { status: 204 });
  }

  const responseHeaders = new Headers();
  const responseContentType = upstreamResponse.headers.get("content-type");

  if (responseContentType != null) {
    responseHeaders.set("Content-Type", responseContentType);
  }

  return new Response(await upstreamResponse.text(), {
    headers: responseHeaders,
    status: upstreamResponse.status,
    statusText: upstreamResponse.statusText,
  });
}

export async function GET(request: NextRequest, context: AccountRouteContext) {
  return proxyAccountRequest(request, context);
}

export async function POST(request: NextRequest, context: AccountRouteContext) {
  return proxyAccountRequest(request, context);
}

export async function PUT(request: NextRequest, context: AccountRouteContext) {
  return proxyAccountRequest(request, context);
}

export async function DELETE(request: NextRequest, context: AccountRouteContext) {
  return proxyAccountRequest(request, context);
}

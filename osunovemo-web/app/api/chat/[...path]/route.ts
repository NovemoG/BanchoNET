import type { NextRequest } from "next/server";
import { refreshAuthSession } from "@/lib/auth";
import { getAccessToken } from "@/lib/auth/session";
import { osuApiBaseUrl } from "@/lib/osu-api-common";

export const dynamic = "force-dynamic";
export const runtime = "nodejs";

const allowedPathPatterns = [
  /^ack$/,
  /^new$/,
  /^updates$/,
  /^channels$/,
  /^channels\/\d+$/,
  /^channels\/\d+\/messages$/,
  /^channels\/\d+\/users\/\d+$/,
  /^channels\/\d+\/mark-as-read\/\d+$/,
];

type ChatRouteContext = {
  params: Promise<{
    path: string[];
  }>;
};

function appendBodyValue(params: URLSearchParams, key: string, value: unknown) {
  if (value == null) {
    return;
  }

  if (Array.isArray(value)) {
    for (const item of value) {
      appendBodyValue(params, key, item);
    }

    return;
  }

  params.append(key, typeof value === "boolean" ? String(value).toLowerCase() : String(value));
}

function getChatPath(segments: string[]) {
  return segments.map((segment) => encodeURIComponent(segment)).join("/");
}

function isAllowedChatPath(path: string) {
  return allowedPathPatterns.some((pattern) => pattern.test(path));
}

function buildTargetUrl(path: string, request: NextRequest) {
  const url = new URL(`chat/${path}`, `${osuApiBaseUrl}/`);

  for (const [key, value] of request.nextUrl.searchParams.entries()) {
    url.searchParams.append(key, value);
  }

  return url;
}

async function getRequestBody(request: NextRequest) {
  if (request.method === "GET" || request.method === "HEAD") {
    return null;
  }

  const contentType = request.headers.get("content-type") ?? "";

  if (contentType.includes("application/json")) {
    const json = (await request.json()) as Record<string, unknown>;
    const params = new URLSearchParams();

    for (const [key, value] of Object.entries(json)) {
      appendBodyValue(params, key, value);
    }

    return {
      body: params,
      contentType: "application/x-www-form-urlencoded",
    };
  }

  if (contentType.includes("multipart/form-data") || contentType.includes("application/x-www-form-urlencoded")) {
    const formData = await request.formData();
    const params = new URLSearchParams();

    for (const [key, value] of formData.entries()) {
      if (typeof value === "string") {
        params.append(key, value);
      }
    }

    return {
      body: params,
      contentType: "application/x-www-form-urlencoded",
    };
  }

  const text = await request.text();

  if (text.length === 0) {
    return null;
  }

  return {
    body: text,
    contentType,
  };
}

async function proxyChatRequest(
  request: NextRequest,
  context: ChatRouteContext,
) {
  await refreshAuthSession();

  const accessToken = await getAccessToken();

  if (accessToken == null) {
    return Response.json({ error: "Unauthorized." }, { status: 401 });
  }

  const { path: pathSegments } = await context.params;
  const decodedPath = pathSegments.join("/");

  if (!isAllowedChatPath(decodedPath)) {
    return Response.json({ error: "Unsupported chat endpoint." }, { status: 404 });
  }

  const body = await getRequestBody(request);
  const headers: Record<string, string> = {
    Accept: "application/json",
    Authorization: `Bearer ${accessToken}`,
  };

  if (body?.contentType != null && body.contentType.length > 0) {
    headers["Content-Type"] = body.contentType;
  }

  const upstreamResponse = await fetch(buildTargetUrl(getChatPath(pathSegments), request), {
    body: body?.body,
    cache: "no-store",
    headers,
    method: request.method,
  });

  if (upstreamResponse.status === 204) {
    return new Response(null, { status: 204 });
  }

  const responseHeaders = new Headers();
  const contentType = upstreamResponse.headers.get("content-type");

  if (contentType != null) {
    responseHeaders.set("Content-Type", contentType);
  }

  return new Response(await upstreamResponse.text(), {
    headers: responseHeaders,
    status: upstreamResponse.status,
    statusText: upstreamResponse.statusText,
  });
}

export async function GET(
  request: NextRequest,
  context: ChatRouteContext,
) {
  return proxyChatRequest(request, context);
}

export async function POST(
  request: NextRequest,
  context: ChatRouteContext,
) {
  return proxyChatRequest(request, context);
}

export async function PUT(
  request: NextRequest,
  context: ChatRouteContext,
) {
  return proxyChatRequest(request, context);
}

export async function DELETE(
  request: NextRequest,
  context: ChatRouteContext,
) {
  return proxyChatRequest(request, context);
}

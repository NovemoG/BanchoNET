import type { NextRequest } from "next/server";
import { refreshAuthSession } from "@/lib/auth";
import { getAccessToken } from "@/lib/auth/session";
import { osuApiBaseUrl } from "@/lib/osu-api-common";
import {
  type AuthenticatedWebSocket,
  type WebSocketMessageData,
  createAuthenticatedWebSocket,
  socketMessageToText,
  webSocketReadyState,
} from "@/lib/realtime/authenticated-websocket";
import type { NotificationBundle, RealtimeSocketEvent } from "@/lib/realtime/types";

export const dynamic = "force-dynamic";
export const runtime = "nodejs";

const encoder = new TextEncoder();
const heartbeatIntervalMs = 25_000;

function formatSse(event: string, data: unknown) {
  const payload = JSON.stringify(data).replace(/\r?\n/g, "\ndata: ");

  return `event: ${event}\ndata: ${payload}\n\n`;
}

function buildNotificationsUrl() {
  const url = new URL("notifications", `${osuApiBaseUrl}/`);
  url.searchParams.set("unread", "1");

  return url;
}

async function fetchNotificationBundle(accessToken: string, signal: AbortSignal) {
  const response = await fetch(buildNotificationsUrl(), {
    cache: "no-store",
    headers: {
      Accept: "application/json",
      Authorization: `Bearer ${accessToken}`,
    },
    signal,
  });

  if (!response.ok) {
    throw new Error(`Notifications endpoint returned ${response.status}.`);
  }

  return (await response.json()) as NotificationBundle;
}

function getNotificationEndpoint(bundle: NotificationBundle) {
  const endpoint =
    process.env.OSU_NOTIFICATION_ENDPOINT ??
    process.env.NEXT_PUBLIC_OSU_NOTIFICATION_ENDPOINT ??
    bundle.notification_endpoint;

  if (typeof endpoint !== "string" || endpoint.length === 0) {
    throw new Error("Notifications endpoint response did not include notification_endpoint.");
  }

  return endpoint;
}

function parseSocketEvent(text: string): RealtimeSocketEvent {
  const value = JSON.parse(text) as unknown;

  if (
    typeof value === "object" &&
    value != null &&
    "event" in value &&
    typeof (value as { event?: unknown }).event === "string"
  ) {
    return value as RealtimeSocketEvent;
  }

  return {
    data: value,
    event: "message",
  };
}

function waitForSocketOpen(socket: AuthenticatedWebSocket, signal: AbortSignal) {
  if (signal.aborted) {
    return Promise.reject(new Error("Realtime stream was aborted."));
  }

  if (socket.readyState === webSocketReadyState.open) {
    return Promise.resolve();
  }

  if (
    socket.readyState === webSocketReadyState.closed ||
    socket.readyState === webSocketReadyState.closing
  ) {
    return Promise.reject(new Error("Notifications websocket closed before opening."));
  }

  return new Promise<void>((resolve, reject) => {
    const cleanup = () => {
      signal.removeEventListener("abort", handleAbort);
      socket.off("close", handleClose);
      socket.off("error", handleError);
      socket.off("open", handleOpen);
    };

    const handleAbort = () => {
      cleanup();
      reject(new Error("Realtime stream was aborted."));
    };

    const handleClose = (code: number, reason: Buffer) => {
      cleanup();
      reject(new Error(`Notifications websocket closed before opening (${code}: ${reason.toString("utf8")}).`));
    };

    const handleError = (error: Error) => {
      cleanup();
      reject(error);
    };

    const handleOpen = () => {
      cleanup();
      resolve();
    };

    signal.addEventListener("abort", handleAbort, { once: true });
    socket.once("close", handleClose);
    socket.once("error", handleError);
    socket.once("open", handleOpen);
  });
}

function waitForSocketClose(socket: AuthenticatedWebSocket, signal: AbortSignal) {
  if (signal.aborted || socket.readyState === webSocketReadyState.closed) {
    return Promise.resolve();
  }

  return new Promise<void>((resolve, reject) => {
    const cleanup = () => {
      signal.removeEventListener("abort", handleAbort);
      socket.off("close", handleClose);
      socket.off("error", handleError);
    };

    const handleAbort = () => {
      cleanup();
      resolve();
    };

    const handleClose = () => {
      cleanup();
      resolve();
    };

    const handleError = (error: Error) => {
      cleanup();
      reject(error);
    };

    signal.addEventListener("abort", handleAbort, { once: true });
    socket.once("close", handleClose);
    socket.once("error", handleError);
  });
}

async function startRealtimeBridge(
  controller: ReadableStreamDefaultController<Uint8Array>,
  accessToken: string,
  signal: AbortSignal,
) {
  let socket: AuthenticatedWebSocket | null = null;
  let closed = false;
  let heartbeat: ReturnType<typeof setInterval> | null = null;
  let handleSocketMessage: ((data: WebSocketMessageData) => void) | null = null;

  const send = (event: string, data: unknown) => {
    if (closed) {
      return;
    }

    try {
      controller.enqueue(encoder.encode(formatSse(event, data)));
    } catch {
      closed = true;
    }
  };

  try {
    send("realtime.status", { status: "connecting" });

    heartbeat = setInterval(() => {
      send("realtime.ping", { at: new Date().toISOString() });
    }, heartbeatIntervalMs);

    const bundle = await fetchNotificationBundle(accessToken, signal);
    const endpoint = getNotificationEndpoint(bundle);

    send("notification.bundle", bundle);

    socket = createAuthenticatedWebSocket(endpoint, accessToken, signal);
    handleSocketMessage = (data) => {
      try {
        send("socket.message", parseSocketEvent(socketMessageToText(data)));
      } catch {
        send("socket.message", {
          data: socketMessageToText(data),
          event: "message",
        });
      }
    };
    socket.on("message", handleSocketMessage);

    await waitForSocketOpen(socket, signal);

    send("realtime.status", { status: "connected" });
    socket.send(JSON.stringify({ event: "chat.start" }));

    await waitForSocketClose(socket, signal);
  } catch (error) {
    if (!signal.aborted) {
      send("realtime.error", {
        message: error instanceof Error ? error.message : "Realtime connection failed.",
      });
    }
  } finally {
    closed = true;

    if (heartbeat != null) {
      clearInterval(heartbeat);
    }

    if (socket != null && handleSocketMessage != null) {
      socket.off("message", handleSocketMessage);
    }

    if (socket?.readyState === webSocketReadyState.open) {
      try {
        socket.send(JSON.stringify({ event: "chat.end" }));
      } catch {
        // The websocket is already closing.
      }

      socket.close(1000, "stream closed");
    } else if (socket?.readyState === webSocketReadyState.connecting) {
      socket.terminate?.();
    }

    try {
      controller.close();
    } catch {
      // The browser has already closed the stream.
    }
  }
}

export async function GET(request: NextRequest) {
  await refreshAuthSession();

  const accessToken = await getAccessToken();

  if (accessToken == null) {
    return Response.json({ message: "Unauthorized." }, { status: 401 });
  }

  const abortController = new AbortController();
  const abort = () => abortController.abort();

  if (request.signal.aborted) {
    abort();
  } else {
    request.signal.addEventListener("abort", abort, { once: true });
  }

  const stream = new ReadableStream<Uint8Array>({
    cancel() {
      abortController.abort();
      request.signal.removeEventListener("abort", abort);
    },
    start(controller) {
      void startRealtimeBridge(controller, accessToken, abortController.signal).finally(() => {
        request.signal.removeEventListener("abort", abort);
      });
    },
  });

  return new Response(stream, {
    headers: {
      "Cache-Control": "no-store, no-transform",
      Connection: "keep-alive",
      "Content-Type": "text/event-stream; charset=utf-8",
      "X-Accel-Buffering": "no",
    },
  });
}

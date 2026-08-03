import "server-only";

import { createRequire } from "node:module";

export type WebSocketMessageData =
  | ArrayBuffer
  | Buffer
  | Buffer[]
  | string
  | Uint8Array;

export type AuthenticatedWebSocket = {
  close: (code?: number, reason?: string) => void;
  off: {
    (event: "close", listener: (code: number, reason: Buffer) => void): AuthenticatedWebSocket;
    (event: "error", listener: (error: Error) => void): AuthenticatedWebSocket;
    (event: "message", listener: (data: WebSocketMessageData, isBinary: boolean) => void): AuthenticatedWebSocket;
    (event: "open", listener: () => void): AuthenticatedWebSocket;
  };
  on: {
    (event: "close", listener: (code: number, reason: Buffer) => void): AuthenticatedWebSocket;
    (event: "error", listener: (error: Error) => void): AuthenticatedWebSocket;
    (event: "message", listener: (data: WebSocketMessageData, isBinary: boolean) => void): AuthenticatedWebSocket;
    (event: "open", listener: () => void): AuthenticatedWebSocket;
  };
  once: {
    (event: "close", listener: (code: number, reason: Buffer) => void): AuthenticatedWebSocket;
    (event: "error", listener: (error: Error) => void): AuthenticatedWebSocket;
    (event: "message", listener: (data: WebSocketMessageData, isBinary: boolean) => void): AuthenticatedWebSocket;
    (event: "open", listener: () => void): AuthenticatedWebSocket;
  };
  readyState: number;
  send: (data: string) => void;
  terminate?: () => void;
};

type WebSocketConstructor = {
  new (
    url: string,
    protocols: string[],
    options: {
      handshakeTimeout: number;
      headers: Record<string, string>;
    },
  ): AuthenticatedWebSocket;
  CLOSED: number;
  CLOSING: number;
  CONNECTING: number;
  OPEN: number;
};

const require = createRequire(import.meta.url);
const WebSocket = require("next/dist/compiled/ws") as WebSocketConstructor;

export const webSocketReadyState = {
  closed: WebSocket.CLOSED,
  closing: WebSocket.CLOSING,
  connecting: WebSocket.CONNECTING,
  open: WebSocket.OPEN,
} as const;

export function createAuthenticatedWebSocket(
  endpoint: string,
  accessToken: string,
  signal?: AbortSignal,
) {
  const socket = new WebSocket(endpoint, [], {
    handshakeTimeout: 15_000,
    headers: {
      Authorization: `Bearer ${accessToken}`,
    },
  });

  if (signal != null) {
    const abort = () => {
      if (socket.readyState === WebSocket.OPEN) {
        socket.close(1000, "aborted");
        return;
      }

      socket.terminate?.();
    };

    if (signal.aborted) {
      abort();
    } else {
      signal.addEventListener("abort", abort, { once: true });
      socket.once("close", () => signal.removeEventListener("abort", abort));
    }
  }

  return socket;
}

export function socketMessageToText(data: WebSocketMessageData) {
  if (typeof data === "string") {
    return data;
  }

  if (Buffer.isBuffer(data)) {
    return data.toString("utf8");
  }

  if (Array.isArray(data)) {
    return Buffer.concat(data).toString("utf8");
  }

  if (data instanceof ArrayBuffer) {
    return Buffer.from(new Uint8Array(data)).toString("utf8");
  }

  return Buffer.from(data).toString("utf8");
}

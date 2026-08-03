import "server-only";

import type { NextRequest } from "next/server";
import type { TokenRequestOrigin } from "@/lib/auth/osu-auth-client";

/**
 * The browser's identity, to forward to the API when minting a token on its behalf.
 *
 * Token requests are made by this server, so without forwarding these the API attributes every
 * web session to the container: the sessions list read "node" for every entry, and two different
 * browsers belonging to the same user were indistinguishable.
 *
 * nginx already sets X-Forwarded-For for the browser to server hop, so the first entry is the
 * real client.
 */
export function getRequestOrigin(request: NextRequest): TokenRequestOrigin {
  const forwardedFor = request.headers.get("x-forwarded-for");

  return {
    ip: forwardedFor?.split(",")[0]?.trim() ?? request.headers.get("x-real-ip"),
    userAgent: request.headers.get("user-agent"),
  };
}

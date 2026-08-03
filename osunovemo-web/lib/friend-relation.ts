import "server-only";

import { auth } from "@/lib/auth";
import { fetchOsuApi } from "@/lib/osu-api";

export type FriendRelation = "mutual" | "friend" | "none";

type RelationshipResponse = {
  mutual?: boolean;
  target_id?: number;
};

export async function getFriendRelation(targetId: number): Promise<FriendRelation | null> {
  const session = await auth();

  if (session == null || session.user.id === targetId) {
    return null;
  }

  try {
    const friends = await fetchOsuApi<RelationshipResponse[]>("friends", undefined, {
      auth: "user",
    });
    const match = friends.find((relationship) => relationship.target_id === targetId);

    if (match == null) {
      return "none";
    }

    return match.mutual === true ? "mutual" : "friend";
  } catch {
    return "none";
  }
}

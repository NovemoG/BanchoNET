import { fetchUserProfile, isProfileNotFound } from "@/lib/profile";
import { rankingModes, type Ruleset } from "@/lib/rankings";

export const dynamic = "force-dynamic";
export const runtime = "nodejs";

type UserRouteContext = {
  params: Promise<{
    user: string;
  }>;
};

function normalizeProfileMode(value: string | null): Ruleset | null {
  if (value == null || value.length === 0) {
    return null;
  }

  const normalized = value === "ctb" ? "fruits" : value;

  return rankingModes.includes(normalized as Ruleset) ? normalized as Ruleset : null;
}

export async function GET(request: Request, context: UserRouteContext) {
  const { user } = await context.params;
  const requestedMode = new URL(request.url).searchParams.get("mode");
  const mode = normalizeProfileMode(requestedMode);

  if (requestedMode != null && mode == null) {
    return Response.json({ error: "Invalid mode." }, { status: 400 });
  }

  try {
    return Response.json(await fetchUserProfile(user, mode ?? undefined));
  } catch (error) {
    if (isProfileNotFound(error)) {
      return Response.json({ error: "User not found." }, { status: 404 });
    }

    throw error;
  }
}

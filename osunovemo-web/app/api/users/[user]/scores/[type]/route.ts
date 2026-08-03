import type {NextRequest} from "next/server";
import {OsuApiError} from "@/lib/osu-api";
import {fetchProfileScores, isScoreSection, profileScoreBatchSize, profileScoreLoadMoreBatchSize} from "@/lib/profile";
import {rankingModes, type Ruleset} from "@/lib/rankings";

const maxScoreBatchSize = profileScoreLoadMoreBatchSize;

function parseInteger(value: string | null, fallback: number) {
    if (value == null) {
        return fallback;
    }

    const parsed = Number.parseInt(value, 10);
    return Number.isFinite(parsed) ? parsed : fallback;
}

export async function GET(
    request: NextRequest,
    context: RouteContext<"/api/users/[user]/scores/[type]">,
) {
    const {type, user} = await context.params;

    if (!isScoreSection(type)) {
        return Response.json({error: "Invalid score section."}, {status: 400});
    }

    const modeParam = request.nextUrl.searchParams.get("mode");
    if (modeParam == null || !rankingModes.includes(modeParam as Ruleset)) {
        return Response.json({error: "Invalid ruleset."}, {status: 400});
    }

    const limit = Math.max(
        1,
        Math.min(maxScoreBatchSize, parseInteger(request.nextUrl.searchParams.get("limit"), profileScoreBatchSize)),
    );
    const offset = Math.max(0, parseInteger(request.nextUrl.searchParams.get("offset"), 0));

    try {
        const scores = await fetchProfileScores(user, type, modeParam as Ruleset, {limit, offset});
        return Response.json({scores});
    } catch (error) {
        if (error instanceof OsuApiError) {
            return Response.json({error: "Failed to load scores."}, {status: error.status});
        }

        throw error;
    }
}

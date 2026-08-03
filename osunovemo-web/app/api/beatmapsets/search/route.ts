import type {NextRequest} from "next/server";
import {auth} from "@/lib/auth";
import {getPublicBeatmapsetSearchState, parseBeatmapsetSearchState} from "@/lib/beatmapset-search";
import {fetchBeatmapsetSearch} from "@/lib/beatmapset-search-server";
import type {BeatmapsetSearchCursor} from "@/lib/beatmapset-search-types";

function parseCursor(searchParams: URLSearchParams) {
    const cursor: BeatmapsetSearchCursor = {};

    searchParams.forEach((value, key) => {
        const match = /^cursor\[(.+)]$/.exec(key);

        if (match?.[1] != null) {
            cursor[match[1]] = value;
        }
    });

    return Object.keys(cursor).length > 0 ? cursor : null;
}

export async function GET(request: NextRequest) {
    const session = await auth();
    const parsedState = parseBeatmapsetSearchState(
        Object.fromEntries(request.nextUrl.searchParams.entries()),
    );
    const state = session == null ? getPublicBeatmapsetSearchState(parsedState) : parsedState;
    const data = await fetchBeatmapsetSearch(state, {
        cursor: parseCursor(request.nextUrl.searchParams),
    });

    if (!data.backendAvailable) {
        return Response.json(data, {status: 503});
    }

    return Response.json(data);
}

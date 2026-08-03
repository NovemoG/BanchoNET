import type { Metadata } from "next";
import { BeatmapsetSearchPage } from "@/components/beatmapsets-search/page";
import { getPublicBeatmapsetSearchState, parseBeatmapsetSearchState } from "@/lib/beatmapset-search";
import { auth } from "@/lib/auth";
import { fetchBeatmapsetSearch } from "@/lib/beatmapset-search-server";
import { getOsuApiOrigin } from "@/lib/osu-api-common";

type BeatmapsetsPageProps = {
  searchParams: Promise<Record<string, string | string[] | undefined>>;
};

export const metadata: Metadata = {
  description: "Search and filter beatmapsets.",
  title: "Beatmap Listing",
};

export default async function BeatmapsetsPage({
  searchParams,
}: BeatmapsetsPageProps) {
  const session = await auth();
  const parsedState = parseBeatmapsetSearchState(await searchParams);
  const state = session == null ? getPublicBeatmapsetSearchState(parsedState) : parsedState;
  const data = await fetchBeatmapsetSearch(state);

  return <BeatmapsetSearchPage data={data} osuApiOrigin={getOsuApiOrigin()} state={state} />;
}

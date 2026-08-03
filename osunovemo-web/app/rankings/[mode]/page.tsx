import { redirect } from "next/navigation";
import { buildRankingsHref, parseRankingState } from "@/lib/rankings";

type RankingsModeRedirectPageProps = {
  params: Promise<{
    mode: string;
  }>;
  searchParams: Promise<Record<string, string | string[] | undefined>>;
};

export default async function RankingsModeRedirectPage({
  params,
  searchParams,
}: RankingsModeRedirectPageProps) {
  const routeParams = await params;
  const state = parseRankingState(
    { mode: routeParams.mode, sort: undefined, type: undefined },
    await searchParams,
  );

  redirect(buildRankingsHref(state));
}

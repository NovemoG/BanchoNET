import { redirect } from "next/navigation";
import { buildRankingsHref, parseRankingState } from "@/lib/rankings";

type RankingsTypeRedirectPageProps = {
  params: Promise<{
    mode: string;
    type: string;
  }>;
  searchParams: Promise<Record<string, string | string[] | undefined>>;
};

export default async function RankingsTypeRedirectPage({
  params,
  searchParams,
}: RankingsTypeRedirectPageProps) {
  const routeParams = await params;
  const state = parseRankingState(
    { mode: routeParams.mode, sort: undefined, type: routeParams.type },
    await searchParams,
  );

  redirect(buildRankingsHref(state));
}

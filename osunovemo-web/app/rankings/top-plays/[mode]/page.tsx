import type { CSSProperties } from "react";
import { redirect } from "next/navigation";
import { RankingsHeader } from "@/components/rankings/rankings-header";
import { PageWrapper } from "@/components/page-wrapper";
import { TopPlaysList } from "@/components/rankings/top-plays-list";
import { TopPlaysPager } from "@/components/rankings/top-plays-pager";
import { DEFAULT_FILTER, DEFAULT_MODE, DEFAULT_SORT, rankingModes, type RankingSearchState } from "@/lib/rankings";
import { fetchTopPlaysPage, fetchTopPlaysSummary } from "@/lib/top-plays";

export const dynamic = "force-dynamic";

const rankingsHue = { "--base-hue": "125" } as CSSProperties;

type TopPlaysPageProps = {
  params: Promise<{
    mode: string;
  }>;
  searchParams: Promise<Record<string, string | string[] | undefined>>;
};

function parsePage(value: string | string[] | undefined) {
  const raw = Array.isArray(value) ? value[0] : value;
  const page = Number.parseInt(raw ?? "1", 10);

  return Number.isFinite(page) && page > 0 ? page : 1;
}

function buildNavigationState(mode: (typeof rankingModes)[number]): RankingSearchState {
  return {
    country: null,
    filter: DEFAULT_FILTER,
    mode,
    page: 1,
    sort: DEFAULT_SORT,
    type: "global",
    variant: mode === "mania" ? "all" : null,
  };
}

export default async function TopPlaysPage({ params, searchParams }: TopPlaysPageProps) {
  const routeParams = await params;

  if (!rankingModes.includes(routeParams.mode as (typeof rankingModes)[number])) {
    redirect(`/rankings/top-plays/${DEFAULT_MODE}`);
  }

  const mode = routeParams.mode as (typeof rankingModes)[number];
  const requestedPage = parsePage((await searchParams).page);
  const summary = await fetchTopPlaysSummary(mode);

  if (summary.totalPages > 0 && requestedPage > summary.totalPages) {
    redirect(
      summary.totalPages > 1
        ? `/rankings/top-plays/${mode}?page=${summary.totalPages}`
        : `/rankings/top-plays/${mode}`,
    );
  }

  const scores =
    summary.totalPages > 0
      ? await fetchTopPlaysPage(mode, requestedPage)
      : [];

  return (
    <main
      className="flex-1 bg-background text-foreground"
      style={rankingsHue}
    >
      <RankingsHeader mode={mode} state={buildNavigationState(mode)} type="top-plays" />

      <div className="flex flex-col pb-6">
        <PageWrapper modifiers="generic">
          <div id="scores" />
          <TopPlaysPager currentPage={requestedPage} mode={mode} totalPages={summary.totalPages} />

          <section className="overflow-x-auto text-xs text-white">
            {scores.length > 0 ? (
              <TopPlaysList mode={mode} page={requestedPage} scores={scores} />
            ) : (
              <div className="py-5 text-sm text-osu-f1">The data is being calculated...</div>
            )}
          </section>

          <TopPlaysPager currentPage={requestedPage} mode={mode} totalPages={summary.totalPages} />
        </PageWrapper>
      </div>
    </main>
  );
}

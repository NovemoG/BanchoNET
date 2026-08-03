import type {CSSProperties} from "react";
import {
    RankingPager,
    RankingSelect,
    RankingSortBar,
    RankingSortStrip,
    RankingUserFilter,
} from "@/components/rankings/client-controls";
import {CountryRankingTable, GlobalRankingTable, TeamRankingTable,} from "@/components/rankings/ranking-tables";
import {
    type CountryRankingEntry,
    fetchCountryOptions,
    fetchRankings,
    type GlobalRankingEntry,
    type TeamRankingEntry,
} from "@/lib/osu-api";
import {type CountryOption, getSortOptions, getVariantOptions, normalizeSortForType, PAGE_SIZE, parseRankingState,} from "@/lib/rankings";
import {PageWrapper} from "@/components/page-wrapper";
import {RankingsHeader} from "@/components/rankings/rankings-header";
import {auth} from "@/lib/auth";

export const dynamic = "force-dynamic";

const rankingsHue = {"--base-hue": "125"} as CSSProperties;

type RankingsPageProps = {
    params: Promise<{
        mode: string;
        sort: string;
        type: string;
    }>;
    searchParams: Promise<Record<string, string | string[] | undefined>>;
};

export default async function RankingsPage({params, searchParams}: RankingsPageProps) {
    const routeParams = await params;
    const state = parseRankingState(routeParams, await searchParams);
    const sortOptions = getSortOptions(state.type);
    const showSort = state.type !== "country";
    const variantOptions = getVariantOptions(state.mode);
    const [rankingData, countryOptions, session] = await Promise.all([
        fetchRankings(state),
        state.type === "global" ? fetchCountryOptions(state.mode) : Promise.resolve<CountryOption[]>([]),
        auth(),
    ]);

    const totalPages = Math.max(1, Math.ceil(rankingData.total / PAGE_SIZE));

    return (
        <main
            className="flex-1 bg-background text-foreground"
            style={rankingsHue}
        >
            <RankingsHeader mode={state.mode} state={state} type={state.type}/>

            <div className="flex flex-col pb-6">
                {state.type === "global" && (
                    <PageWrapper modifiers="ranking-info">
                        <div className="flex flex-col gap-x-5 gap-y-2.5 sm:flex-row sm:items-center sm:justify-end">
                            <RankingSelect
                                label="Country"
                                options={[
                                    {label: "All", value: ""},
                                    ...countryOptions.map((option) => ({
                                        label: option.label,
                                        value: option.value,
                                    })),
                                ]}
                                resetKeys={["page"]}
                                state={state}
                                value={state.country ?? ""}
                                className="lg:min-w-[18rem]"
                            />
                            {variantOptions !== null && (
                                <RankingSortStrip
                                    label="Variant"
                                    currentValue={state.variant ?? "all"}
                                    resetKeys={["page"]}
                                    options={variantOptions.map((value) => ({
                                        label: value.toUpperCase(),
                                        value,
                                    }))}
                                    state={state}
                                    updateKey="variant"
                                />
                            )}
                            <RankingUserFilter isAuthenticated={session != null} state={state}/>
                        </div>
                    </PageWrapper>
                )}

                <PageWrapper modifiers="generic">
                    {showSort && (
                        <RankingSortBar
                            currentValue={state.sort}
                            resetKeys={["page"]}
                            options={sortOptions.map((value) => ({
                                label:
                                    value === "performance"
                                        ? "Performance"
                                        : value === "score"
                                            ? "Ranked Score"
                                            : "Accuracy",
                                value,
                            }))}
                            state={state}
                            updateKey="sort"
                        />
                    )}

                    <div id="scores" />
                    <RankingPager currentPage={state.page} state={state} totalPages={totalPages}/>

                    <section className="overflow-x-auto text-xs text-white">
                        {state.type === "global" && (
                            <GlobalRankingTable
                                mode={state.mode}
                                page={state.page}
                                rankings={rankingData.ranking as GlobalRankingEntry[]}
                                sort={state.sort}
                                showRankChange={
                                    state.sort === "performance" &&
                                    state.country === null &&
                                    (state.variant === null || state.variant === "all")
                                }
                            />
                        )}

                        {state.type === "country" && (
                            <CountryRankingTable
                                page={state.page}
                                rankings={rankingData.ranking as CountryRankingEntry[]}
                            />
                        )}

                        {state.type === "team" && (
                            <TeamRankingTable
                                page={state.page}
                                rankings={rankingData.ranking as TeamRankingEntry[]}
                                sort={normalizeSortForType("team", state.sort)}
                            />
                        )}
                    </section>

                    <RankingPager currentPage={state.page} state={state} totalPages={totalPages}/>
                </PageWrapper>
            </div>
        </main>
    );
}

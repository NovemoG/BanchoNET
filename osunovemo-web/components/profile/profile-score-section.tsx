"use client";

import {useMemo, useState} from "react";
import {HighlightPeriodSelector} from "@/components/highlight-period-selector";
import {ProfileScoreList} from "@/components/profile/profile-score-list";
import {SectionTitle} from "@/components/profile/section-title";
import type {Score, ScoreSection} from "@/lib/profile";
import type {Ruleset} from "@/lib/rankings";
import {scoreHighlightOptions} from "@/lib/score-highlight";

type ProfileScoreSectionProps = {
    canLoadMore?: boolean;
    count?: number;
    initialScores: Score[];
    mode: Ruleset;
    section: ScoreSection;
    showHighlightControl?: boolean;
    showPpWeight?: boolean;
    title: string;
    totalCount?: number;
    userId: number;
};

export function ProfileScoreSection({
    canLoadMore = false,
    count,
    initialScores,
    mode,
    section,
    showHighlightControl = false,
    showPpWeight = false,
    title,
    totalCount,
    userId,
}: ProfileScoreSectionProps) {
    const [highlightOptionIndex, setHighlightOptionIndex] = useState([0]);

    const selectedHighlightOption = useMemo(
        () => scoreHighlightOptions[highlightOptionIndex[0]] ?? scoreHighlightOptions[0],
        [highlightOptionIndex],
    );

    return (
        <div>
            <SectionTitle
                actions={showHighlightControl ? (
                    <HighlightPeriodSelector
                        onSelectedIndexChange={setHighlightOptionIndex}
                        selectedIndex={highlightOptionIndex}
                    />
                ) : undefined}
                count={count}
                title={title}
            />
            <ProfileScoreList
                canLoadMore={canLoadMore}
                highlightPeriodDays={showHighlightControl ? selectedHighlightOption.days : null}
                initialScores={initialScores}
                mode={mode}
                section={section}
                showPpWeight={showPpWeight}
                totalCount={totalCount}
                userId={userId}
            />
        </div>
    );
}

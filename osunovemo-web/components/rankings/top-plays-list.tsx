"use client";

import {useMemo, useState} from "react";
import {HighlightPeriodSelector} from "@/components/highlight-period-selector";
import { RankingScoreCard } from "@/components/rankings/ranking-score-card";
import type { TopPlayScore } from "@/lib/top-plays";
import { PAGE_SIZE, type Ruleset } from "@/lib/rankings";
import {getScoreHighlightCutoff, isScoreHighlighted, scoreHighlightOptions} from "@/lib/score-highlight";

function getRank(page: number, index: number) {
  return (page - 1) * PAGE_SIZE + index + 1;
}

export function TopPlaysList({
  mode,
  page,
  scores,
}: {
  mode: Ruleset;
  page: number;
  scores: TopPlayScore[];
}) {
  const [highlightOptionIndex, setHighlightOptionIndex] = useState([0]);
  const [now] = useState(() => Date.now());
  const selectedHighlightOption = useMemo(
    () => scoreHighlightOptions[highlightOptionIndex[0]] ?? scoreHighlightOptions[0],
    [highlightOptionIndex],
  );
  const highlightCutoff = useMemo(
    () => getScoreHighlightCutoff(selectedHighlightOption.days, now),
    [now, selectedHighlightOption.days],
  );
  const highlightedCount = useMemo(() => {
    if (highlightCutoff == null) {
      return scores.length;
    }

    return scores.reduce(
      (count, score) => (isScoreHighlighted(score.ended_at, highlightCutoff) ? count + 1 : count),
      0,
    );
  }, [highlightCutoff, scores]);

  return (
    <div className="min-w-180 max-[900px]:min-w-0">
      <div className="mb-3 flex justify-end">
        <HighlightPeriodSelector
          onSelectedIndexChange={setHighlightOptionIndex}
          selectedIndex={highlightOptionIndex}
        />
      </div>
      {scores.map((score, index) => (
        <div key={score.id}>
          <RankingScoreCard
            highlightActive={highlightCutoff != null}
            highlighted={isScoreHighlighted(score.ended_at, highlightCutoff)}
            mode={mode}
            position={getRank(page, index)}
            score={score}
          />
        </div>
      ))}
      {highlightCutoff != null && highlightedCount === 0 ? (
        <p className="mt-2 text-center text-xs text-osu-f1">No scores landed in the selected period.</p>
      ) : null}
    </div>
  );
}

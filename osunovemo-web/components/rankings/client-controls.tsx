"use client";

import { ChevronLeftIcon, ChevronRightIcon } from "lucide-react";
import { useRouter, useSearchParams } from "next/navigation";
import { buildRankingsHref, type CountryOption, type RankingSearchState } from "@/lib/rankings";
import { cn } from "@/lib/utils";
import {
  Select,
  SelectContent,
  SelectGroup,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";

type QueryValue = string | null | undefined;
const ALL_SELECT_VALUE = "__all__";

type QueryTabItem = {
  label: string;
  value: string;
  disabled?: boolean;
};

const paginationContainerClassName =
  "flex items-center justify-center px-0 py-2.5 text-xs text-osu-l2 sm:px-2.5";
const paginationColumnClassName = "m-0.5 flex";
const paginationPagesClassName = "-m-0.5 flex flex-wrap list-none p-0";
const paginationItemClassName = "m-0.5";
const paginationLinkClassName =
  "block rounded-full border-0 bg-transparent px-2.5 py-1 leading-[1.2] text-inherit";
const paginationQuickLinkClassName =
  "inline-flex items-center justify-center gap-1.5 whitespace-nowrap rounded-full bg-osu-b4 px-[15px] py-1 uppercase leading-[1.2] text-inherit";

type NavigationUpdates = Partial<
  Pick<
    RankingSearchState,
    "country" | "filter" | "mode" | "page" | "sort" | "type" | "variant"
  >
>;

function useRankingNavigation(state: RankingSearchState) {
  const router = useRouter();
  const searchParams = useSearchParams();

  return (updates: NavigationUpdates, resetKeys: string[] = []) => {
    const nextSearchParams = new URLSearchParams(searchParams.toString());

    for (const key of resetKeys) {
      nextSearchParams.delete(key);
    }

    const nextState: RankingSearchState = {
      country: state.country,
      filter: state.filter,
      mode: state.mode,
      page: state.page,
      sort: state.sort,
      type: state.type,
      variant: state.variant,
    };

    for (const [key, value] of Object.entries(updates) as Array<
      [keyof NavigationUpdates, NavigationUpdates[keyof NavigationUpdates]]
    >) {
      if (
        key === "country" ||
        key === "filter" ||
        key === "mode" ||
        key === "sort" ||
        key === "type" ||
        key === "variant"
      ) {
        (nextState[key] as QueryValue | number) = value as QueryValue;
      } else if (key === "page" && typeof value === "number") {
        nextState.page = value;
      }
    }

    if (nextState.type !== "global") {
      nextState.filter = "all";
      nextState.country = null;
      nextState.variant = null;
    }

    if (nextState.mode !== "mania") {
      nextState.variant = null;
    }

    if (nextState.type === "country") {
      nextState.sort = "performance";
    }

    if (typeof nextState.page !== "number" || nextState.page < 1) {
      nextState.page = 1;
    }

    // Clicking the option that is already active must not navigate or refetch. Compared on the
    // resolved state rather than the href, because the current URL is not necessarily in the
    // canonical form buildRankingsHref produces (defaults are omitted from it).
    const unchanged =
      nextState.country === state.country &&
      nextState.filter === state.filter &&
      nextState.mode === state.mode &&
      nextState.page === state.page &&
      nextState.sort === state.sort &&
      nextState.type === state.type &&
      nextState.variant === state.variant;

    if (unchanged) {
      return;
    }

    router.push(buildRankingsHref(nextState));
  };
}

function SortButtons({
  currentValue,
  onSelect,
  options,
  showTitle = false,
  title = "Sort",
  variant = "default",
}: {
  currentValue: string;
  onSelect: (value: string) => void;
  options: QueryTabItem[];
  showTitle?: boolean;
  title?: string;
  variant?: "default" | "ranking-header";
}) {
  return (
    <div className="flex p-0 text-xs">
      <div className="-m-1 flex flex-wrap items-center">
        {showTitle && <div className="m-1 px-0 py-1">{title}</div>}
        {options.map((option) => {
          const isActive = option.value === currentValue;

          return (
            <button
              key={option.value}
              className={cn(
                "m-1 rounded border-0 bg-transparent px-2.5 py-1 text-osu-l1 outline-none transition-colors disabled:cursor-default disabled:text-osu-f1",
                variant === "ranking-header"
                  ? "hover:bg-osu-b4 focus-visible:bg-osu-b4"
                  : "hover:bg-osu-b3 focus-visible:bg-osu-b3",
                isActive &&
                  (variant === "ranking-header"
                    ? "bg-osu-b4 font-semibold text-osu-c1"
                    : "bg-osu-b3 font-semibold text-osu-c1"),
              )}
              disabled={option.disabled}
              onClick={() => !option.disabled && onSelect(option.value)}
              type="button"
            >
              {option.label}
            </button>
          );
        })}
      </div>
    </div>
  );
}

export function RankingSortBar({
  currentValue,
  options,
  resetKeys = [],
  state,
  updateKey,
}: {
  currentValue: string;
  options: QueryTabItem[];
  resetKeys?: string[];
  state: RankingSearchState;
  updateKey: "filter" | "sort" | "variant";
}) {
  const navigate = useRankingNavigation(state);

  return (
    <SortButtons
      currentValue={currentValue}
      onSelect={(value) =>
        navigate({ [updateKey]: value, page: 1 } as NavigationUpdates, resetKeys)
      }
      options={options}
      showTitle
      variant="default"
    />
  );
}

export function RankingSortStrip({
  label,
  currentValue,
  state,
  updateKey,
  options,
  resetKeys = [],
}: {
  label: string;
  currentValue: string;
  state: RankingSearchState;
  updateKey: "filter" | "sort" | "variant";
  options: QueryTabItem[];
  resetKeys?: string[];
}) {
  const navigate = useRankingNavigation(state);

  return (
    <div className="flex flex-1 items-center sm:block sm:flex-none">
      <div className="mb-0 mr-[15px] text-xs font-bold sm:mb-2.5">{label}</div>
      <SortButtons
        currentValue={currentValue}
        variant="ranking-header"
        onSelect={(value) =>
          navigate({ [updateKey]: value, page: 1 } as NavigationUpdates, resetKeys)
        }
        options={options}
      />
    </div>
  );
}

export function RankingSelect({
  label,
  state,
  value,
  options,
  resetKeys = [],
  className = "",
}: {
  label: string;
  state: RankingSearchState;
  value: string;
  options: CountryOption[];
  resetKeys?: string[];
  className?: string;
}) {
  const navigate = useRankingNavigation(state);

  return (
    <div className={cn("flex-1", className)}>
      <div className="mb-2.5 text-xs font-bold">{label}</div>
      <Select
        onValueChange={(nextValue) =>
          navigate(
            { country: nextValue === ALL_SELECT_VALUE ? null : nextValue, page: 1 },
            resetKeys,
          )
        }
        value={value || ALL_SELECT_VALUE}
      >
        <SelectTrigger className="w-full min-w-[14rem] justify-between">
          <SelectValue placeholder={label} />
        </SelectTrigger>
        <SelectContent>
          <SelectGroup>
            {options.map((option) => (
              <SelectItem
                key={option.value || ALL_SELECT_VALUE}
                value={option.value || ALL_SELECT_VALUE}
              >
                {option.label}
              </SelectItem>
            ))}
          </SelectGroup>
        </SelectContent>
      </Select>
    </div>
  );
}

export function RankingUserFilter({
  isAuthenticated = false,
  state,
}: {
  isAuthenticated?: boolean;
  state: RankingSearchState;
}) {
  return (
    <RankingSortStrip
      currentValue={state.filter}
      label="Filter"
      options={[
        { label: "All", value: "all" },
        // The server resolves "whose friends" from the token, so there is nothing to filter by
        // when signed out.
        { label: "Friends", value: "friends", disabled: !isAuthenticated },
      ]}
      resetKeys={["page"]}
      state={state}
      updateKey="filter"
    />
  );
}

/**
 * Sliding window around the current page only. First and last are rendered separately as anchors,
 * so including them here too rendered them twice.
 */
function getVisiblePages(currentPage: number, totalPages: number) {
  const pages = [currentPage - 2, currentPage - 1, currentPage, currentPage + 1, currentPage + 2];

  return pages
    .filter((page) => page >= 1 && page <= totalPages)
    .sort((left, right) => left - right);
}

export function RankingPager({
  currentPage,
  state,
  totalPages,
}: {
  currentPage: number;
  state: RankingSearchState;
  totalPages: number;
}) {
  const navigate = useRankingNavigation(state);
  const visiblePages = getVisiblePages(currentPage, totalPages);
  const firstVisible = visiblePages[0] ?? 1;
  const lastVisible = visiblePages[visiblePages.length - 1] ?? totalPages;

  // Derived from the window, so an anchor is never rendered for a page the window already shows.
  const showFirstPage = firstVisible > 1;
  const showLeadingEllipsis = firstVisible > 2;
  const showTrailingEllipsis = lastVisible < totalPages - 1;
  const showLastPage = lastVisible < totalPages;

  if (totalPages <= 1) {
    return null;
  }

  return (
    <nav aria-label="Rankings pagination" className={paginationContainerClassName}>
      <div className={paginationColumnClassName}>
        <button
          className={cn(
            paginationQuickLinkClassName,
            currentPage === 1 && "cursor-default opacity-50",
          )}
          disabled={currentPage === 1}
          onClick={() => navigate({ page: currentPage - 1 })}
          type="button"
        >
          <ChevronLeftIcon className="size-3.5" />
          <span className="hidden sm:inline">Previous</span>
        </button>
      </div>

      <ul className={cn(paginationColumnClassName, paginationPagesClassName)}>
        {showFirstPage && (
          <li className={paginationItemClassName}>
            <button
              className={paginationLinkClassName}
              onClick={() => navigate({ page: 1 })}
              type="button"
            >
              1
            </button>
          </li>
        )}

        {showLeadingEllipsis && (
          <li className={paginationItemClassName}>
            <span className={paginationLinkClassName}>...</span>
          </li>
        )}

        {visiblePages.map((page) => (
          <li key={page} className={paginationItemClassName}>
            {page === currentPage ? (
              <span className={cn(paginationLinkClassName, "bg-osu-h1 font-semibold text-osu-b5")}>
                {page}
              </span>
            ) : (
              <button
                className={paginationLinkClassName}
                onClick={() => navigate({ page })}
                type="button"
              >
                {page}
              </button>
            )}
          </li>
        ))}

        {showTrailingEllipsis && (
          <li className={paginationItemClassName}>
            <span className={paginationLinkClassName}>...</span>
          </li>
        )}

        {showLastPage && (
          <li className={paginationItemClassName}>
            <button
              className={paginationLinkClassName}
              onClick={() => navigate({ page: totalPages })}
              type="button"
            >
              {totalPages}
            </button>
          </li>
        )}
      </ul>

      <div className={paginationColumnClassName}>
        <button
          className={cn(
            paginationQuickLinkClassName,
            currentPage >= totalPages && "cursor-default opacity-50",
          )}
          disabled={currentPage >= totalPages}
          onClick={() => navigate({ page: currentPage + 1 })}
          type="button"
        >
          <span className="hidden sm:inline">Next</span>
          <ChevronRightIcon className="size-3.5" />
        </button>
      </div>
    </nav>
  );
}

import Link from "next/link";
import { cn } from "@/lib/utils";

const paginationContainerClassName =
  "flex items-center justify-center px-0 py-2.5 text-xs text-osu-l2 sm:px-2.5";
const paginationColumnClassName = "m-0.5 flex";
const paginationPagesClassName = "-m-0.5 flex flex-wrap list-none p-0";
const paginationItemClassName = "m-0.5";
const paginationLinkClassName =
  "block rounded-full border-0 bg-transparent px-2.5 py-1 leading-[1.2] text-inherit";
const paginationQuickLinkClassName =
  "inline-flex items-center justify-center whitespace-nowrap rounded-full bg-osu-b4 px-[15px] py-1 uppercase leading-[1.2] text-inherit";

function buildTopPlaysHref(mode: string, page: number) {
  return page > 1 ? `/rankings/top-plays/${mode}?page=${page}` : `/rankings/top-plays/${mode}`;
}

/**
 * The sliding window around the current page ONLY. First and last are rendered separately as
 * anchors, so including them here too is what produced two "1"s and two last-page entries.
 */
function getVisiblePages(currentPage: number, totalPages: number) {
  const pages: number[] = [];

  for (let page = currentPage - 2; page <= currentPage + 2; page += 1) {
    if (page >= 1 && page <= totalPages) {
      pages.push(page);
    }
  }

  return pages;
}

export function TopPlaysPager({
  currentPage,
  mode,
  totalPages,
}: {
  currentPage: number;
  mode: string;
  totalPages: number;
}) {
  const visiblePages = getVisiblePages(currentPage, totalPages);
  const firstVisible = visiblePages[0] ?? 1;
  const lastVisible = visiblePages[visiblePages.length - 1] ?? totalPages;

  // Derived from the window rather than from currentPage, so an anchor can never be rendered for a
  // page the window already contains, and an ellipsis only appears when there is a real gap.
  const showFirstPage = firstVisible > 1;
  const showLeadingEllipsis = firstVisible > 2;
  const showTrailingEllipsis = lastVisible < totalPages - 1;
  const showLastPage = lastVisible < totalPages;

  if (totalPages <= 1) {
    return null;
  }

  return (
    <nav aria-label="Top plays pagination" className={paginationContainerClassName}>
      <div className={paginationColumnClassName}>
        {currentPage > 1 ? (
          <Link
            className={paginationQuickLinkClassName}
            href={buildTopPlaysHref(mode, currentPage - 1)}
          >
            <span className="hidden sm:inline">Previous</span>
          </Link>
        ) : (
          <span
            className={cn(
              paginationQuickLinkClassName,
              "cursor-default opacity-50",
            )}
          >
            <span className="hidden sm:inline">Previous</span>
          </span>
        )}
      </div>

      <ul className={cn(paginationColumnClassName, paginationPagesClassName)}>
        {showFirstPage && (
          <li className={paginationItemClassName}>
            <Link className={paginationLinkClassName} href={buildTopPlaysHref(mode, 1)}>
              1
            </Link>
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
              <Link className={paginationLinkClassName} href={buildTopPlaysHref(mode, page)}>
                {page}
              </Link>
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
            <Link className={paginationLinkClassName} href={buildTopPlaysHref(mode, totalPages)}>
              {totalPages}
            </Link>
          </li>
        )}
      </ul>

      <div className={paginationColumnClassName}>
        {currentPage < totalPages ? (
          <Link
            className={paginationQuickLinkClassName}
            href={buildTopPlaysHref(mode, currentPage + 1)}
          >
            <span className="hidden sm:inline">Next</span>
          </Link>
        ) : (
          <span
            className={cn(
              paginationQuickLinkClassName,
              "cursor-default opacity-50",
            )}
          >
            <span className="hidden sm:inline">Next</span>
          </span>
        )}
      </div>
    </nav>
  );
}

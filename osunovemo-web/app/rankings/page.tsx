import { redirect } from "next/navigation";
import { buildRankingsHref, DEFAULT_FILTER, DEFAULT_MODE, DEFAULT_SORT, DEFAULT_TYPE } from "@/lib/rankings";

export default function RankingsIndexPage() {
  redirect(
    buildRankingsHref({
      country: null,
      filter: DEFAULT_FILTER,
      mode: DEFAULT_MODE,
      page: 1,
      sort: DEFAULT_SORT,
      type: DEFAULT_TYPE,
      variant: null,
    }),
  );
}

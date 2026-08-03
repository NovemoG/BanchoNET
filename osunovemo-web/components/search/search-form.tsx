"use client";

import { useRouter } from "next/navigation";
import { type FormEvent, useState } from "react";

/**
 * Navigates client side rather than submitting natively.
 *
 * A plain <form method="get"> triggers a full document load, which tears down and rebuilds the
 * whole app: the realtime bridge reconnects and /me, /notifications, /chat/channels and
 * /chat/updates are all fetched again on every search.
 */
export function SearchForm({ initialQuery }: { initialQuery: string }) {
  const router = useRouter();
  const [query, setQuery] = useState(initialQuery);

  const handleSubmit = (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();

    const trimmed = query.trim();

    router.push(trimmed.length > 0 ? `/home/search?query=${encodeURIComponent(trimmed)}` : "/home/search");
  };

  return (
    <form className="mb-6 flex gap-2" onSubmit={handleSubmit}>
      <input
        aria-label="search"
        autoFocus
        className="h-10 min-w-0 flex-1 rounded-md border border-white/10 bg-osu-b6 px-3 text-sm text-white outline-none transition focus:border-osu-h1"
        name="query"
        onChange={(event) => setQuery(event.currentTarget.value)}
        placeholder="username..."
        type="search"
        value={query}
      />
      <button
        className="h-10 rounded-md bg-osu-h2 px-5 text-sm font-bold text-white transition hover:bg-osu-h1"
        type="submit"
      >
        search
      </button>
    </form>
  );
}

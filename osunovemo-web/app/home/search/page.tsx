import type { Metadata } from "next";
import Link from "next/link";
import { Music, Search, UserRound } from "lucide-react";
import { CountryFlag } from "@/components/country-flag";
import { Header } from "@/components/header";
import { PageWrapper } from "@/components/page-wrapper";
import { resolveAssetUrl } from "@/lib/osu-api-common";
import { searchUsers } from "@/lib/search";
import { SearchForm } from "@/components/search/search-form";

export const dynamic = "force-dynamic";

export const metadata: Metadata = {
  title: "search",
};

function getQuery(value: string | string[] | undefined) {
  return (Array.isArray(value) ? value[0] : value)?.trim() ?? "";
}

export default async function SearchPage({
  searchParams,
}: {
  searchParams: Promise<Record<string, string | string[] | undefined>>;
}) {
  const params = await searchParams;
  const query = getQuery(params.query ?? params.q);
  const { total, users } = await searchUsers(query);

  return (
    <main className="flex-1 bg-background text-foreground">
      <Header
        background={<div className="size-full bg-osu-b6" />}
        icon={<Search aria-hidden className="size-6" />}
        title="search"
      />

      <PageWrapper className="py-6" modifiers="generic">
        <SearchForm initialQuery={query} />

        <section className="grid gap-3">
          <h2 className="flex items-center gap-2 text-lg font-semibold text-white">
            <UserRound aria-hidden className="size-5" />
            Players
            {total > 0 ? <span className="text-sm font-normal text-osu-f1">({total})</span> : null}
          </h2>

          {query.length === 0 ? (
            <p className="text-sm text-osu-f1">Enter a username to search.</p>
          ) : users.length === 0 ? (
            <p className="text-sm text-osu-f1">No players matched &ldquo;{query}&rdquo;.</p>
          ) : (
            <ul className="grid gap-2 sm:grid-cols-2">
              {users.map((user) => (
                <li key={user.id}>
                  <Link
                    className="flex items-center gap-3 rounded-lg bg-osu-b4 px-3 py-2 transition-colors hover:bg-osu-b3"
                    href={`/users/${user.id}`}
                  >
                    <span
                      className="size-10 shrink-0 rounded-md bg-osu-b5 bg-cover bg-center"
                      style={{ backgroundImage: `url(${JSON.stringify(resolveAssetUrl(user.avatar_url))})` }}
                    />
                    <span className="min-w-0">
                      <span className="block truncate font-semibold text-white">{user.username}</span>
                      <span className="flex items-center gap-1.5 text-xs text-osu-f1">
                        <CountryFlag code={user.country_code} name={user.country?.name} />
                        {user.country?.name ?? user.country_code}
                      </span>
                    </span>
                  </Link>
                </li>
              ))}
            </ul>
          )}
        </section>

        <section className="mt-8 grid gap-3">
          <h2 className="flex items-center gap-2 text-lg font-semibold text-white">
            <Music aria-hidden className="size-5" />
            Beatmaps
          </h2>
          <p className="text-sm text-osu-c1">
            {query.length === 0 ? (
              "Beatmaps have their own search with filters."
            ) : (
              <Link
                className="font-semibold text-osu-h1 hover:underline"
                href={`/beatmapsets?q=${encodeURIComponent(query)}`}
              >
                Search beatmaps for &ldquo;{query}&rdquo;
              </Link>
            )}
          </p>
        </section>
      </PageWrapper>
    </main>
  );
}

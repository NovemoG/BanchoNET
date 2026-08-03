import type { Metadata } from "next";
import { Download, Monitor, Terminal } from "lucide-react";
import { Header } from "@/components/header";
import { PageWrapper } from "@/components/page-wrapper";
import { getPublicDomain } from "@/lib/osu-api-common";

export const metadata: Metadata = {
  title: "download",
};

type Build = {
  description: string;
  label: string;
  platform: "Win" | "Linux";
  tachyon: boolean;
};

const builds: Build[] = [
  {
    description: "Portable build for Windows.",
    label: "Windows",
    platform: "Win",
    tachyon: false,
  },
  {
    description: "Portable build for Linux.",
    label: "Linux",
    platform: "Linux",
    tachyon: false,
  },
  {
    description: "Experimental client for Windows.",
    label: "Windows (tachyon)",
    platform: "Win",
    tachyon: true,
  },
  {
    description: "Experimental client for Linux.",
    label: "Linux (tachyon)",
    platform: "Linux",
    tachyon: true,
  },
];

function buildDownloadHref(domain: string, build: Build) {
  // Served by the updater controller on the api subdomain. The rest of that controller imitates
  // the GitHub API for in-game updates and is not involved here.
  return `https://api.${domain}/download?tachyon=${build.tachyon}&platform=${build.platform}`;
}

export default function DownloadPage() {
  const domain = getPublicDomain();

  return (
    <main className="flex-1 bg-background text-foreground">
      <Header
        background={<div className="size-full bg-osu-b6" />}
        icon={<Download aria-hidden className="size-6" />}
        title="download"
      />

      <PageWrapper className="py-6" modifiers="generic">
        <section className="grid gap-3">
          <h2 className="text-lg font-semibold text-white">Client</h2>
          <p className="max-w-3xl text-sm text-osu-c1">
            Already configured to connect here. Extract it anywhere and run it.
          </p>

          <ul className="grid gap-2 sm:grid-cols-2">
            {builds.map((build) => (
              <li key={build.label}>
                <a
                  className="flex items-center gap-3 rounded-lg bg-osu-b4 px-4 py-3 transition-colors hover:bg-osu-b3"
                  href={buildDownloadHref(domain, build)}
                >
                  <Monitor aria-hidden className="size-5 shrink-0 text-osu-l1" />
                  <span className="min-w-0">
                    <span className="block font-semibold text-white">{build.label}</span>
                    <span className="block text-xs text-osu-f1">{build.description}</span>
                  </span>
                  <Download aria-hidden className="ml-auto size-4 shrink-0 text-osu-l1" />
                </a>
              </li>
            ))}
          </ul>

          <p className="text-xs text-osu-f1">
            A build that has not been published yet will return a 404.
          </p>
        </section>

        <section className="mt-8 grid gap-3">
          <h2 className="text-lg font-semibold text-white">Stable</h2>
          <p className="max-w-3xl text-sm text-osu-c1">
            The stable client connects here through a launch argument. Make a shortcut to your
            existing installation and append it to the target:
          </p>

          <div className="flex items-start gap-3 overflow-x-auto rounded-lg bg-osu-b5 px-4 py-3">
            <Terminal aria-hidden className="mt-0.5 size-4 shrink-0 text-osu-l1" />
            <code className="whitespace-nowrap text-sm text-white">
              osu!.exe -devserver {domain}
            </code>
          </div>

          <p className="max-w-3xl text-xs text-osu-f1">
            Launching without the argument connects to the official server as usual, so the same
            installation can be used for both.
          </p>
        </section>
      </PageWrapper>
    </main>
  );
}

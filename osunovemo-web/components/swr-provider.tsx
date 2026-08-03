"use client";

import type {ReactNode} from "react";
import {SWRConfig, type SWRConfiguration} from "swr";

async function jsonFetcher(url: string) {
    const response = await fetch(url, {cache: "no-store"});

    if (!response.ok) {
        const body = await response.text();
        throw new Error(body.length > 0 ? body : `Request failed with ${response.status}.`);
    }

    if (response.status === 204) {
        return null;
    }

    return response.json();
}

const swrConfig = {
    fetcher: jsonFetcher,
    revalidateOnFocus: false,
    shouldRetryOnError: false,
} satisfies SWRConfiguration;

export function SwrProvider({children}: { children: ReactNode }) {
    return <SWRConfig value={swrConfig}>{children}</SWRConfig>;
}

"use client";

import { ProfileLineChart } from "@/components/profile/profile-line-chart";
import type { LandingGraphDatum } from "@/lib/home";

const timeFormatter = new Intl.DateTimeFormat("en-US", {
  hour: "2-digit",
  minute: "2-digit",
});

export function OnlineGraph({
  className,
  data,
}: {
  className?: string;
  data: LandingGraphDatum[];
}) {
  const points = data.map((point) => ({
    label: timeFormatter.format(new Date(point.x)),
    value: point.y,
  }));

  return (
    <ProfileLineChart
      className={className}
      data={points}
      emptyLabel="No data yet"
      strokeColor="hsl(var(--hsl-h1))"
      valueLabel="Online"
    />
  );
}

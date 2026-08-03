"use client";

import {
  Line,
  LineChart,
  ResponsiveContainer,
  Tooltip,
  XAxis,
  YAxis,
} from "recharts";
import {
  ChartContainer,
  ChartTooltipContent,
  type ChartConfig,
} from "@/components/ui/chart";
import { cn } from "@/lib/utils";

type ProfileRankChartPoint = {
  offsetDays: number;
  value: number;
};

type ProfileRankChartProps = {
  className?: string;
  data: ProfileRankChartPoint[];
  emptyLabel?: string;
  strokeColor: string;
};

const integerFormatter = new Intl.NumberFormat("en-US");
const chartInnerPadding = 8;

function formatDaysAgo(offsetDays: number) {
  if (offsetDays === 0) {
    return "Now";
  }

  const days = Math.abs(offsetDays);
  return `${days} day${days === 1 ? "" : "s"} ago`;
}

function getDomain(data: ProfileRankChartPoint[]) {
  const values = data.map((point) => point.value);
  const min = Math.min(...values);
  const max = Math.max(...values);

  if (min === max) {
    return [Math.max(1, min - 1), max + 1] as const;
  }

  return [min, max] as const;
}

export function ProfileRankChart({
  className,
  data,
  emptyLabel = "Unranked",
  strokeColor,
}: ProfileRankChartProps) {
  if (data.length === 0) {
    return <div className={cn("flex h-full w-full items-center justify-center text-white/55", className)}>{emptyLabel}</div>;
  }

  const config = {
    value: {
      color: strokeColor,
      label: "Global Rank",
    },
  } satisfies ChartConfig;

  const [domainMin, domainMax] = getDomain(data);

  return (
    <ChartContainer className={cn("h-full min-h-0 max-h-full w-full overflow-visible", className)} config={config}>
      <ResponsiveContainer width="100%" height="100%">
        <LineChart data={data} margin={{ bottom: 18, left: 0, right: 0, top: 18 }}>
          <XAxis
            allowDataOverflow
            dataKey="offsetDays"
            domain={["dataMin", "dataMax"]}
            hide
            padding={{ left: chartInnerPadding, right: chartInnerPadding }}
            type="number"
          />
          <YAxis
            allowDataOverflow
            domain={[domainMin, domainMax]}
            hide
            padding={{ bottom: 8, top: 8 }}
            reversed
            scale="log"
            type="number"
            width={0}
          />
          <Tooltip
            // See profile-line-chart: without this the tooltip slides in from the left.
            isAnimationActive={false}
            content={(
              <ChartTooltipContent
                formatter={(value) => `#${integerFormatter.format(Number(value ?? 0))}`}
                labelFormatter={(label) => formatDaysAgo(Number(label ?? 0))}
              />
            )}
            cursor={false}
          />
          <Line
            activeDot={{ fill: "var(--color-value)", r: 4, strokeWidth: 0 }}
            dataKey="value"
            dot={false}
            stroke="var(--color-value)"
            strokeWidth={2}
            type="monotone"
          />
        </LineChart>
      </ResponsiveContainer>
    </ChartContainer>
  );
}

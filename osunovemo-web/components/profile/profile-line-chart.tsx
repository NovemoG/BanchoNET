"use client";

import {
  CartesianGrid,
  Line,
  LineChart,
  ResponsiveContainer,
  XAxis,
  YAxis,
} from "recharts";
import {
  ChartContainer,
  ChartTooltip,
  ChartTooltipContent,
  type ChartConfig,
} from "@/components/ui/chart";
import { cn } from "@/lib/utils";

type ProfileLineChartPoint = {
  label: string;
  value: number;
};

type ProfileLineChartProps = {
  className?: string;
  data: ProfileLineChartPoint[];
  emptyLabel?: string;
  strokeColor: string;
  valueLabel: string;
};

const integerFormatter = new Intl.NumberFormat("en-US");

export function ProfileLineChart({
  className,
  data,
  emptyLabel = "No data",
  strokeColor,
  valueLabel,
}: ProfileLineChartProps) {
  if (data.length === 0) {
    return <div className={cn("flex h-full w-full items-center justify-center text-white/55", className)}>{emptyLabel}</div>;
  }

  const config = {
    value: {
      color: strokeColor,
      label: valueLabel,
    },
  } satisfies ChartConfig;

  return (
    <ChartContainer className={cn("h-full w-full", className)} config={config}>
      <ResponsiveContainer width="100%" height="100%">
        <LineChart data={data} margin={{ bottom: 18, left: 6, right: 8, top: 8 }}>
          <CartesianGrid stroke="rgba(255,255,255,0.08)" vertical={false} />
          <XAxis
            axisLine={{ stroke: "rgba(255,255,255,0.16)" }}
            dataKey="label"
            height={18}
            interval="preserveStartEnd"
            minTickGap={18}
            tick={{ fill: "rgba(255,255,255,0.55)", fontSize: 10 }}
            tickLine={false}
          />
          <YAxis
            axisLine={{ stroke: "rgba(255,255,255,0.16)" }}
            tick={{ fill: "rgba(255,255,255,0.55)", fontSize: 10 }}
            tickFormatter={(value) => integerFormatter.format(Number(value ?? 0))}
            tickLine={false}
            width={1}
          />
          <ChartTooltip
            // Recharts animates the tooltip's position, so it slid in from the chart's left edge
            // every time instead of appearing under the cursor.
            isAnimationActive={false}
            cursor={false}
            content={
              <ChartTooltipContent
                formatter={(value) => integerFormatter.format(Number(value ?? 0))}
              />
            }
          />
          <Line
            dataKey="value"
            dot={false}
            activeDot={{ fill: "var(--color-value)", r: 4, strokeWidth: 0 }}
            stroke="var(--color-value)"
            strokeWidth={2}
            type="linear"
          />
        </LineChart>
      </ResponsiveContainer>
    </ChartContainer>
  );
}

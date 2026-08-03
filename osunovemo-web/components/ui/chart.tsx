"use client";

import * as React from "react";
import * as RechartsPrimitive from "recharts";
import { cn } from "@/lib/utils";

export type ChartConfig = Record<
  string,
  {
    color?: string;
    label?: React.ReactNode;
  }
>;

type ChartContextProps = {
  config: ChartConfig;
};

const ChartContext = React.createContext<ChartContextProps | null>(null);

function useChart() {
  const context = React.useContext(ChartContext);

  if (context == null) {
    throw new Error("Chart components must be used inside <ChartContainer />");
  }

  return context;
}

export function ChartContainer({
  children,
  className,
  config,
}: React.ComponentProps<"div"> & {
  config: ChartConfig;
  children: React.ReactElement<typeof RechartsPrimitive.ResponsiveContainer>;
}) {
  const style = React.useMemo(
    () =>
      Object.fromEntries(
        Object.entries(config).flatMap(([key, value]) =>
          value.color ? [[`--color-${key}`, value.color]] : [],
        ),
      ) as React.CSSProperties,
    [config],
  );

  return (
    <ChartContext.Provider value={{ config }}>
      <div className={cn("chart-surface flex justify-center text-xs", className)} style={style}>
        {children}
      </div>
    </ChartContext.Provider>
  );
}

export const ChartTooltip = RechartsPrimitive.Tooltip;

export function ChartTooltipContent({
  active,
  formatter,
  hideLabel = false,
  label,
  labelFormatter,
  payload,
}: {
  active?: boolean;
  formatter?: (
    value: number | string | Array<number | string> | undefined,
    name: string | undefined,
    item: {
      color?: string;
      dataKey?: string | number;
      name?: string;
      value?: number | string | Array<number | string>;
    },
    payload: Array<{
      color?: string;
      dataKey?: string | number;
      name?: string;
      value?: number | string | Array<number | string>;
    }>,
  ) => React.ReactNode;
  hideLabel?: boolean;
  label?: React.ReactNode;
  labelFormatter?: (label: React.ReactNode) => React.ReactNode;
  payload?: Array<{
    color?: string;
    dataKey?: string | number;
    name?: string;
    value?: number | string | Array<number | string>;
  }>;
}) {
  const { config } = useChart();

  if (!active || payload == null || payload.length === 0) {
    return null;
  }

  return (
    <div className="grid min-w-[8rem] gap-1.5 rounded-md border border-white/10 bg-osu-b5/95 px-3 py-2 text-xs text-white shadow-xl backdrop-blur-sm">
      {!hideLabel && label != null ? (
        <div className="font-medium text-white/70">{labelFormatter ? labelFormatter(label) : label}</div>
      ) : null}
      <div className="grid gap-1">
        {payload.map((item) => {
          const key = item.dataKey?.toString() ?? "value";
          const itemConfig = config[key];
          const indicatorColor = item.color ?? itemConfig?.color ?? "currentColor";

          return (
            <div key={key} className="flex items-center justify-between gap-2">
              <div className="flex items-center gap-2">
                <span className="size-2 rounded-full" style={{ backgroundColor: indicatorColor }} />
                <span className="text-white/70">{itemConfig?.label ?? item.name ?? key}</span>
              </div>
              <span className="font-medium text-white">
                {formatter
                  ? formatter(item.value, item.name, item, payload)
                  : item.value?.toLocaleString?.() ?? item.value}
              </span>
            </div>
          );
        })}
      </div>
    </div>
  );
}

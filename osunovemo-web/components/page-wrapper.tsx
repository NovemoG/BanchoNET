import type { ElementType, ReactNode } from "react";
import { cn } from "@/lib/utils";

type PageWrapperProps<T extends ElementType = "div"> = {
  as?: T;
  children: ReactNode;
  className?: string;
  modifiers?: string | string[];
};

const wrapperBaseClassName =
  "mx-auto w-[calc(100%-20px)] max-w-[1200px] flex-none self-center lg:w-[calc(100%-100px)]";

const modifierClassNames: Record<string, string> = {
  generic:
    "bg-osu-b5 px-4 py-5 text-osu-c1 shadow-[0_12px_32px_rgba(0,0,0,0.24)] sm:px-6 lg:px-8",
  "generic-compact": "bg-osu-b5 text-osu-c1",
  "ranking-info":
    "grid gap-5 bg-osu-d3 px-4 py-5 shadow-[0_12px_32px_rgba(0,0,0,0.24)] sm:px-6 lg:px-8",
};

function getModifierClasses(modifiers?: string | string[]) {
  if (modifiers == null) {
    return [];
  }

  const values = Array.isArray(modifiers) ? modifiers : [modifiers];
  return values
    .map((modifier) => modifierClassNames[modifier])
    .filter((value): value is string => value != null);
}

export function PageWrapper<T extends ElementType = "div">({
  as,
  children,
  className,
  modifiers,
}: PageWrapperProps<T>) {
  const Component = as ?? "div";

  return (
    <Component className={cn(wrapperBaseClassName, getModifierClasses(modifiers), className)}>
      {children}
    </Component>
  );
}

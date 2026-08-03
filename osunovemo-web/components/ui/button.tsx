import * as React from "react"
import { cva, type VariantProps } from "class-variance-authority"
import { Slot } from "radix-ui"

import { cn } from "@/lib/utils"

const buttonVariants = cva(
  "group/button inline-flex shrink-0 items-center justify-center rounded-md border-0 bg-clip-padding align-middle text-sm font-semibold leading-none whitespace-nowrap no-underline transition-colors duration-120 outline-none select-none shadow-[0_0_0_2px_rgba(0,0,0,0.25)] focus-visible:ring-2 focus-visible:ring-osu-c2/40 disabled:opacity-100 disabled:shadow-[0_0_0_2px_rgba(0,0,0,0.18)] [&_svg]:pointer-events-none [&_svg]:shrink-0 [&_svg:not([class*='size-'])]:size-4",
  {
    variants: {
      variant: {
        default:
          "bg-osu-h2 text-osu-c1 hover:bg-osu-h1 focus-visible:bg-osu-h1 active:bg-osu-h1 disabled:bg-osu-b3 disabled:text-osu-f1",
        outline:
          "bg-osu-b4 text-osu-c1 hover:bg-osu-b3 focus-visible:bg-osu-b3 active:bg-osu-b3 disabled:bg-osu-b3 disabled:text-osu-f1",
        secondary:
          "bg-osu-b3 text-osu-c1 hover:bg-osu-b2 focus-visible:bg-osu-b2 active:bg-osu-b2 disabled:bg-osu-b3 disabled:text-osu-f1",
        ghost:
          "bg-transparent text-osu-l1 shadow-none hover:bg-osu-b4 hover:text-osu-c1 focus-visible:bg-osu-b4 focus-visible:text-osu-c1 active:bg-osu-b4",
        destructive:
          "bg-osu-red-3 text-osu-c1 hover:bg-osu-red-2 focus-visible:bg-osu-red-2 active:bg-osu-red-2 disabled:bg-osu-b3 disabled:text-osu-f1",
        link: "rounded-none bg-transparent px-0 py-0 text-osu-c1 shadow-none hover:underline",
      },
      size: {
        default:
          "min-h-8 gap-1.5 px-2.5 py-1.5 has-data-[icon=inline-end]:pr-2 has-data-[icon=inline-start]:pl-2",
        xs: "min-h-6 gap-1 px-2 py-1 text-xs has-data-[icon=inline-end]:pr-1.5 has-data-[icon=inline-start]:pl-1.5 [&_svg:not([class*='size-'])]:size-3",
        sm: "min-h-7 gap-1 px-2.5 py-1 text-[13px] has-data-[icon=inline-end]:pr-1.5 has-data-[icon=inline-start]:pl-1.5 [&_svg:not([class*='size-'])]:size-3.5",
        lg: "min-h-9 gap-1.5 px-3 py-2 has-data-[icon=inline-end]:pr-2.5 has-data-[icon=inline-start]:pl-2.5",
        icon: "size-8",
        "icon-xs": "size-6 [&_svg:not([class*='size-'])]:size-3",
        "icon-sm": "size-7",
        "icon-lg": "size-9",
      },
    },
    defaultVariants: {
      variant: "default",
      size: "default",
    },
  }
)

function Button({
  className,
  variant = "default",
  size = "default",
  asChild = false,
  ...props
}: React.ComponentProps<"button"> &
  VariantProps<typeof buttonVariants> & {
    asChild?: boolean
  }) {
  const Comp = asChild ? Slot.Root : "button"

  return (
    <Comp
      data-slot="button"
      data-variant={variant}
      data-size={size}
      className={cn(buttonVariants({ variant, size, className }))}
      {...props}
    />
  )
}

export { Button, buttonVariants }

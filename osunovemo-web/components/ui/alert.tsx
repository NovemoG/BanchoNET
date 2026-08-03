import * as React from "react"
import { cva, type VariantProps } from "class-variance-authority"

import { cn } from "@/lib/utils"

const alertVariants = cva(
  "group/alert relative grid w-full overflow-hidden rounded-lg border-0 text-left text-sm shadow-[0_12px_32px_rgba(0,0,0,0.24)] has-data-[slot=alert-action]:relative has-data-[slot=alert-action]:pr-18 has-[>svg]:grid-cols-[auto_1fr] has-[>svg]:gap-x-0 has-[>svg]:items-stretch *:[svg]:row-span-2 *:[svg]:h-full *:[svg]:bg-osu-orange-3 *:[svg]:p-3 *:[svg]:text-osu-b6 *:[svg]:[&:not([class*='size-'])]:w-12",
  {
    variants: {
      variant: {
        default: "bg-osu-b4 text-osu-c1",
        destructive:
          "bg-osu-b4 text-osu-c1 *:[svg]:bg-osu-red-3",
      },
      },
    defaultVariants: {
      variant: "default",
    },
  }
)

function Alert({
  className,
  variant,
  ...props
}: React.ComponentProps<"div"> & VariantProps<typeof alertVariants>) {
  return (
    <div
      data-slot="alert"
      role="alert"
      className={cn(alertVariants({ variant }), className)}
      {...props}
    />
  )
}

function AlertTitle({ className, ...props }: React.ComponentProps<"div">) {
  return (
    <div
      data-slot="alert-title"
      className={cn(
        "px-3 pt-3 font-semibold text-white group-has-[>svg]/alert:col-start-2 [&_a]:underline [&_a]:underline-offset-3 [&_a]:hover:text-white",
        className
      )}
      {...props}
    />
  )
}

function AlertDescription({
  className,
  ...props
}: React.ComponentProps<"div">) {
  return (
    <div
      data-slot="alert-description"
      className={cn(
        "px-3 pb-3 text-sm text-osu-f1 md:text-pretty [&_a]:underline [&_a]:underline-offset-3 [&_a]:hover:text-white [&_p:not(:last-child)]:mb-2",
        className
      )}
      {...props}
    />
  )
}

function AlertAction({ className, ...props }: React.ComponentProps<"div">) {
  return (
    <div
      data-slot="alert-action"
      className={cn("absolute top-2 right-2", className)}
      {...props}
    />
  )
}

export { Alert, AlertTitle, AlertDescription, AlertAction }

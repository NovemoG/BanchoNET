import { cn } from "@/lib/utils"

function Skeleton({ className, ...props }: React.ComponentProps<"div">) {
  return (
    <div
      data-slot="skeleton"
      className={cn(
        "animate-pulse rounded-md bg-osu-b4/90 shadow-[inset_0_0_0_1px_rgba(255,255,255,0.04)]",
        className
      )}
      {...props}
    />
  )
}

export { Skeleton }

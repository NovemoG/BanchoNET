import type {ReactNode} from "react";
import {PageWrapper} from "@/components/page-wrapper";
import {cn} from "@/lib/utils";

type HeaderProps = {
    background: ReactNode;
    icon: ReactNode;
    title: ReactNode;
    mobileSubtitle?: ReactNode;
    topRight?: ReactNode;
    bottom?: ReactNode;
    backgroundWrapperClassName?: string;
    className?: string;
    wrapperClassName?: string;
    topClassName?: string;
    bottomClassName?: string;
    titleClassName?: string;
};

export function Header({
                           background,
                           icon,
                           title,
                           mobileSubtitle,
                           topRight,
                           bottom,
                           backgroundWrapperClassName,
                           className,
                           wrapperClassName,
                           topClassName,
                           bottomClassName,
                           titleClassName,
                       }: HeaderProps) {
    return (
        <header className={cn("print:hidden", className)}>
            <div className="relative flex min-h-42 flex-col justify-end bg-osu-d5 md:min-h-50 lg:min-h-55">
                <div className="absolute inset-0 overflow-hidden">
                    <PageWrapper className={cn("relative h-full overflow-hidden", backgroundWrapperClassName)}>
                        {background}
                    </PageWrapper>
                </div>

                <PageWrapper className={cn("relative flex w-full flex-col", wrapperClassName)}>
                    <div
                        className={cn(
                            "relative flex min-h-14 items-center justify-between gap-4 px-4 py-3 sm:px-6 md:min-h-13.75 lg:px-8",
                            topClassName,
                        )}
                    >
                        <div className="flex items-center gap-3">
                            {icon}
                            <div className="flex flex-col">
                                <h1 className={cn("text-[14px] font-semibold leading-none text-white md:text-[20px]", titleClassName)}>
                                    {title}
                                </h1>
                                {mobileSubtitle ? (
                                    <p className="text-[13px] text-osu-l1 md:hidden">{mobileSubtitle}</p>
                                ) : null}
                            </div>
                        </div>
                        {topRight}
                    </div>

                    {bottom ? (
                        <div
                            className={cn("flex min-h-10 flex-wrap items-end gap-3 px-4 sm:px-6 lg:px-8", bottomClassName)}>
                            {bottom}
                        </div>
                    ) : null}
                </PageWrapper>
            </div>
        </header>
    );
}

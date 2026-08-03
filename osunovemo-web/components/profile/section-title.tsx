import type {ReactNode} from "react";

const integerFormatter = new Intl.NumberFormat("en-US");

function formatInteger(value: number | null | undefined) {
    return integerFormatter.format(Math.round(value ?? 0));
}

type SectionTitleProps = {
    actions?: ReactNode;
    count?: number;
    title: string;
};

export function SectionTitle({actions, count, title}: SectionTitleProps) {
    return (
        <div className="flex flex-wrap items-center justify-between gap-3">
            <h3 className="m-0 inline-flex max-w-full items-center gap-2 px-0 py-2.5 text-base font-bold">
                <span aria-hidden className="flex shrink-0 self-stretch items-center">
                    <span className="h-[0.85em] w-0.75 rounded-full bg-osu-h1"/>
                </span>
                <span>{title}</span>
                {count != null ? (
                    <span className="inline-flex items-center rounded-md bg-osu-b6 px-2 py-0.75 text-[0.75em] leading-none text-osu-f1">
                        {formatInteger(count)}
                    </span>
                ) : null}
            </h3>
            {actions ? <div className="ml-auto">{actions}</div> : null}
        </div>
    );
}

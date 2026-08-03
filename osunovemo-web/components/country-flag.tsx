import { cn } from "@/lib/utils";

function getCountryFlagFileName(code: string) {
  return code
    .toUpperCase()
    .split("")
    .map((character) => (character.charCodeAt(0) + 127397).toString(16))
    .join("-");
}

export function getCountryFlagUrl(code: string) {
  return `/assets/images/flags/${getCountryFlagFileName(code)}.svg`;
}

export function CountryFlag({
  className,
  code,
  name,
}: {
  className?: string;
  code?: string | null;
  name?: string | null;
}) {
  if (code == null || code.length !== 2) {
    return null;
  }

  const normalizedCode = code.toUpperCase();
  const flagUrl = getCountryFlagUrl(normalizedCode);

  return (
    <span
      aria-label={name ?? normalizedCode}
      className={cn(
        "relative block h-[1em] w-[1.3889em] rounded-[3px] bg-cover bg-center bg-no-repeat saturate-[1.1]",
        "after:absolute after:inset-0 after:rounded-[inherit] after:bg-[inherit] after:contrast-0 after:brightness-200 after:opacity-25 after:content-['']",
        className,
      )}
      role="img"
      style={{
        backgroundImage: `url("${flagUrl}"), url("/assets/images/flags/fallback.png")`,
      }}
      title={name ?? normalizedCode}
    />
  );
}

import Link from "next/link";

// Links that would go to osu!'s wiki, forums, store or legal pages are deliberately absent: those
// are their pages, not ours. Add our own equivalents here as they exist.
const footerSections = [
  {
    links: [
      { href: "/", label: "Home" },
      { href: "/beatmapsets", label: "Beatmap Listing" },
      { href: "/rankings", label: "Rankings" },
      { href: "/download", label: "Download" },
    ],
    title: "General",
  },
  {
    links: [
      { href: "/community/chat", label: "Chat" },
      { href: "/friends", label: "Friends" },
      { href: "/home/search", label: "Search" },
    ],
    title: "Community",
  },
];

export function SiteFooter() {
  return (
    <footer className="mt-auto bg-osu-b5 py-8">
      <div className="mx-auto grid w-[calc(100%-20px)] max-w-[800px] gap-2.5 text-center md:grid-cols-2 md:text-left">
        {footerSections.map((section) => (
          <ul className="m-0 list-none p-0" key={section.title}>
            <li className="text-[13px] font-bold text-white">{section.title}</li>
            {section.links.map((link) => (
              <li key={link.label}>
                <Link className="text-xs text-osu-h1 transition hover:text-white" href={link.href}>
                  {link.label}
                </Link>
              </li>
            ))}
          </ul>
        ))}
      </div>
    </footer>
  );
}

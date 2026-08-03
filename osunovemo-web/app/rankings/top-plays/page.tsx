import { redirect } from "next/navigation";
import { DEFAULT_MODE } from "@/lib/rankings";

export default function TopPlaysIndexPage() {
  redirect(`/rankings/top-plays/${DEFAULT_MODE}`);
}

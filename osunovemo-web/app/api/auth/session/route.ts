import { refreshAuthSession } from "@/lib/auth";

export async function GET() {
  return Response.json(await refreshAuthSession());
}

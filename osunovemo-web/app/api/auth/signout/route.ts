import { destroySession } from "@/lib/auth";

export async function POST() {
  await destroySession();

  return Response.json({ ok: true });
}

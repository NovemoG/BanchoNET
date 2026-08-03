export const dynamic = "force-dynamic";

const upstreamFlagBaseUrl = "https://osu.ppy.sh/assets/images/flags/";

export async function GET(
  _request: Request,
  context: {
    params: Promise<{
      flag: string;
    }>;
  },
) {
  const { flag } = await context.params;

  if (!/^[0-9a-f-]+\.svg$/i.test(flag)) {
    return new Response(null, { status: 404 });
  }

  const response = await fetch(new URL(flag, upstreamFlagBaseUrl), {
    cache: "force-cache",
  });

  if (!response.ok) {
    return new Response(null, { status: response.status });
  }

  return new Response(response.body, {
    headers: {
      "Cache-Control": "public, max-age=345600",
      "Content-Type": "image/svg+xml",
    },
  });
}

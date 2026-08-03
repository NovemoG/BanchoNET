import {getCurrentAuthUser} from "@/lib/auth";

export async function GET() {
    const user = await getCurrentAuthUser();

    return Response.json(user);
}

import type {Metadata} from "next";
import {redirect} from "next/navigation";
import {FriendsPage, parseFriendsPageState} from "@/components/friends/friends-page";
import {auth, getCurrentAuthUser} from "@/lib/auth";
import {getFriendCoverUrl, fetchFriends} from "@/lib/friends";
import {OsuApiError} from "@/lib/osu-api";

export const metadata: Metadata = {
    title: "friends | cossu!",
};

function getFriendsErrorMessage(error: unknown) {
    if (error instanceof OsuApiError && (error.status === 401 || error.status === 403)) {
        return "Your current session cannot read the friends list. Sign out and sign in again to grant friends.read.";
    }

    return "Could not load your friends list. Try refreshing in a moment.";
}

export default async function FriendsRoute({
                                               searchParams,
                                           }: {
    searchParams: Promise<Record<string, string | string[] | undefined>>;
}) {
    const session = await auth();

    if (session == null) {
        redirect("/");
    }

    const state = parseFriendsPageState(await searchParams);
    const [currentUserResult, friendsResult] = await Promise.allSettled([
        getCurrentAuthUser(),
        fetchFriends(),
    ]);
    const currentUser = currentUserResult.status === "fulfilled" ? currentUserResult.value : null;
    const friends = friendsResult.status === "fulfilled" ? friendsResult.value : [];
    const errorMessage = friendsResult.status === "rejected" ? getFriendsErrorMessage(friendsResult.reason) : null;

    return (
        <FriendsPage
            currentUserCoverUrl={getFriendCoverUrl(currentUser)}
            errorMessage={errorMessage}
            friends={friends}
            state={state}
        />
    );
}

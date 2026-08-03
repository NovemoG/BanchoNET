import {notFound} from "next/navigation";
import {ProfilePage} from "@/components/profile/profile-page";
import {fetchProfilePageData, isProfileNotFound} from "@/lib/profile";
import {getFriendRelation} from "@/lib/friend-relation";
import {rankingModes, type Ruleset} from "@/lib/rankings";

type RenderProfilePageProps = {
    mode?: string;
    user: string;
};

function isRuleset(value: string | undefined): value is Ruleset {
    return value != null && rankingModes.includes(value as Ruleset);
}

export async function renderProfilePage({mode, user}: RenderProfilePageProps) {
    if (mode != null && !isRuleset(mode)) {
        notFound();
    }

    const ruleset = isRuleset(mode) ? mode : undefined;

    try {
        const data = await fetchProfilePageData(user, ruleset);
        const friendRelation = await getFriendRelation(data.user.id);

        return <ProfilePage data={data} friendRelation={friendRelation} ruleset={ruleset}/>;
    } catch (error) {
        if (isProfileNotFound(error)) {
            notFound();
        }

        throw error;
    }
}

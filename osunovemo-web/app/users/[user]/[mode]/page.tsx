import {renderProfilePage} from "../profile-route";

export default async function UserProfileModePage({
                                                      params,
                                                  }: {
    params: Promise<{ mode: string; user: string }>;
}) {
    const {mode, user} = await params;
    return renderProfilePage({mode, user});
}

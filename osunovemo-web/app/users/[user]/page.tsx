import { renderProfilePage } from "./profile-route";

export default async function UserProfilePage({
  params,
}: {
  params: Promise<{ user: string }>;
}) {
  const { user } = await params;
  return renderProfilePage({ user });
}

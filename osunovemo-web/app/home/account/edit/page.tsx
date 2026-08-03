import type {Metadata} from "next";
import {redirect} from "next/navigation";
import {AccountSettingsPage} from "@/components/account-settings/account-settings-page";
import {auth, getCurrentAuthUser} from "@/lib/auth";
import {buildAccountSettingsData} from "@/lib/account-settings";
import type {AuthUser} from "@/lib/auth/types";

export const metadata: Metadata = {
  title: "account settings | cossu!",
};

export default async function AccountSettingsRoute() {
  const session = await auth();

  if (session == null) {
    redirect("/");
  }

  let user: AuthUser = session.user;

  try {
    user = (await getCurrentAuthUser()) ?? session.user;
  } catch {
    user = session.user;
  }

  return (
    <AccountSettingsPage
      initialData={buildAccountSettingsData(user)}
      sessionExpires={session.expires}
    />
  );
}

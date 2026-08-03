import type { Metadata } from "next";
import { notFound } from "next/navigation";
import { ChatPage } from "@/components/chat/chat-page";

type CommunityChatChannelPageProps = {
  params: Promise<{
    channel_id: string;
  }>;
};

export const metadata: Metadata = {
  description: "Realtime chat channel.",
  title: "chat",
};

function parseChannelId(value: string) {
  const channelId = Number.parseInt(value, 10);

  return Number.isFinite(channelId) && channelId > 0 ? channelId : null;
}

export default async function CommunityChatChannelPage({
  params,
}: CommunityChatChannelPageProps) {
  const { channel_id } = await params;
  const channelId = parseChannelId(channel_id);

  if (channelId == null) {
    notFound();
  }

  return <ChatPage initialChannelId={channelId} />;
}

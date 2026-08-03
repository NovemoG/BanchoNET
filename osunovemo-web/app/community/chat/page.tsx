import type { Metadata } from "next";
import { ChatPage } from "@/components/chat/chat-page";

export const metadata: Metadata = {
  description: "Realtime chat channels.",
  title: "chat",
};

export default function CommunityChatPage() {
  return <ChatPage redirectToFirstChannel />;
}

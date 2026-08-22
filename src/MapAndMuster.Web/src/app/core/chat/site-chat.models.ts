import type { ChatLanguage } from './chat-languages';

export interface SiteChatMember {
  userId: string;
  username: string;
  displayName: string;
}

export interface SiteChatMessage {
  id: string;
  postedUtc: string;
  authorUserId: string;
  authorUsername: string;
  authorDisplayName: string;
  body: string;
  language: string;
  kind: 'Player' | 'Admin';
  targetUserId: string | null;
  targetUsername: string | null;
  targetDisplayName: string | null;
}

export interface SiteChatBoard {
  messages: SiteChatMessage[];
  mentionableUsers: SiteChatMember[];
  blockedUsers: SiteChatMember[];
  languages: string[];
  preferredLanguage: string;
  canChat: boolean;
  canSendAdminMessages: boolean;
}

export interface PostSiteChatPayload {
  message: string;
  language: ChatLanguage;
  sendAsAdministrator: boolean;
  targetUserId: string | null;
}

export interface SiteChatSend {
  message: string;
  language: ChatLanguage;
  sendAsAdministrator: boolean;
  targetUserId: string | null;
}

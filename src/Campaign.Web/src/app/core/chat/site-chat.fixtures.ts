import { CHAT_LANGUAGES } from '../../core/chat/chat-languages';
import type { SiteChatBoard } from '../../core/chat/site-chat.models';

export function emptySiteChatBoard(overrides: Partial<SiteChatBoard> = {}): SiteChatBoard {
  return {
    messages: [],
    mentionableUsers: [],
    blockedUsers: [],
    languages: [...CHAT_LANGUAGES],
    preferredLanguage: 'English',
    canChat: true,
    canSendAdminMessages: false,
    ...overrides,
  };
}

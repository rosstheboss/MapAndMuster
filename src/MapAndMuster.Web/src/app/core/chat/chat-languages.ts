export const CHAT_LANGUAGES = [
  'English',
  'Spanish',
  'French',
  'German',
  'Dutch',
  'Italian',
  'Russian',
  'Korean',
  'Chinese',
  'Japanese',
  'Danish',
  'Swedish',
  'Norwegian',
  'Finnish',
  'Hindi',
  'Arabic',
] as const;

export type ChatLanguage = (typeof CHAT_LANGUAGES)[number];

export const DEFAULT_CHAT_LANGUAGE: ChatLanguage = 'English';

export function isChatLanguage(value: unknown): value is ChatLanguage {
  return typeof value === 'string' && (CHAT_LANGUAGES as readonly string[]).includes(value);
}

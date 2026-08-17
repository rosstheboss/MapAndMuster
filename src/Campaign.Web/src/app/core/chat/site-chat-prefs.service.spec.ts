import { DEFAULT_CHAT_LANGUAGE } from './chat-languages';
import {
  defaultSiteChatPrefs,
  SITE_CHAT_COOKIE_NAME,
  readStoredSiteChatPrefs,
  writeStoredSiteChatPrefs,
} from './site-chat-prefs.service';

describe('site chat prefs cookie', () => {
  afterEach(() => {
    document.cookie = `${SITE_CHAT_COOKIE_NAME}=; Path=/; Max-Age=0; SameSite=Lax`;
  });

  it('defaults to English compose language and every language visible', () => {
    expect(readStoredSiteChatPrefs()).toBeNull();
    writeStoredSiteChatPrefs({
      composeLanguage: 'Spanish',
      visibleLanguages: ['English', 'Spanish'],
    });
    expect(document.cookie).toContain(SITE_CHAT_COOKIE_NAME);
    const stored = readStoredSiteChatPrefs();
    expect(stored?.composeLanguage).toBe('Spanish');
    expect(stored?.visibleLanguages).toEqual(['English', 'Spanish']);
  });

  it('uses the profile default when no cookie is stored', () => {
    expect(DEFAULT_CHAT_LANGUAGE).toBe('English');
    expect(defaultSiteChatPrefs().composeLanguage).toBe('English');
    expect(defaultSiteChatPrefs('Spanish').composeLanguage).toBe('Spanish');
    expect(defaultSiteChatPrefs('Spanish').visibleLanguages).toContain('Arabic');
  });
});

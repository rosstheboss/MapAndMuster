import { campaignNeedsJoinPrompt, campaignShareUrl, copyText } from './campaign-share';

describe('campaign share', () => {
  it('prompts only for a private campaign the viewer cannot open', () => {
    expect(campaignNeedsJoinPrompt({ isPrivate: true, canView: false, isParticipant: false })).toBe(true);
    expect(campaignNeedsJoinPrompt({ isPrivate: true, canView: true, isParticipant: false })).toBe(false);
    expect(campaignNeedsJoinPrompt({ isPrivate: true, canView: false, isParticipant: true })).toBe(false);
    expect(campaignNeedsJoinPrompt({ isPrivate: false, canView: false, isParticipant: false })).toBe(false);
  });

  it('builds a campaign page link on this site', () => {
    expect(campaignShareUrl('abc')).toBe(`${location.origin}/campaigns/abc`);
  });

  it('reports when the clipboard is unavailable', async () => {
    const clipboard = navigator.clipboard;
    Object.defineProperty(navigator, 'clipboard', { configurable: true, value: undefined });
    await expect(copyText('https://example.test')).resolves.toBe(false);
    Object.defineProperty(navigator, 'clipboard', { configurable: true, value: clipboard });
  });
});

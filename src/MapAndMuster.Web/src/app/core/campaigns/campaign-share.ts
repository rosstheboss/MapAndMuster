/** A private campaign the viewer cannot open and has not joined. */
export function campaignNeedsJoinPrompt(campaign: {
  isPrivate: boolean;
  canView: boolean;
  isParticipant: boolean;
}): boolean {
  return campaign.isPrivate && !campaign.canView && !campaign.isParticipant;
}

export function campaignShareUrl(campaignId: string): string {
  return new URL(`/campaigns/${encodeURIComponent(campaignId)}`, globalThis.location.origin).href;
}

export async function copyText(value: string): Promise<boolean> {
  try {
    await globalThis.navigator.clipboard.writeText(value);
    return true;
  } catch {
    return false;
  }
}

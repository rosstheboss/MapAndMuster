import { provideZonelessChangeDetection } from '@angular/core';
import { TestBed } from '@angular/core/testing';

import { CampaignShareButtonComponent } from './campaign-share-button.component';

describe('CampaignShareButtonComponent', () => {
  const campaignId = 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa';

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [CampaignShareButtonComponent],
      providers: [provideZonelessChangeDetection()],
    }).compileComponents();
  });

  async function render(): Promise<{
    fixture: ReturnType<typeof TestBed.createComponent<CampaignShareButtonComponent>>;
  }> {
    const fixture = TestBed.createComponent(CampaignShareButtonComponent);
    fixture.componentRef.setInput('campaignId', campaignId);
    fixture.componentRef.setInput('name', 'Border War');
    await fixture.whenStable();
    fixture.detectChanges();
    return { fixture };
  }

  it('copies the campaign link', async () => {
    const writeText = vi.fn().mockResolvedValue(undefined);
    Object.defineProperty(navigator, 'clipboard', { configurable: true, value: { writeText } });
    const { fixture } = await render();
    const compiled = fixture.nativeElement as HTMLElement;
    compiled.querySelector<HTMLButtonElement>('button')?.click();
    await fixture.whenStable();
    fixture.detectChanges();

    expect(writeText).toHaveBeenCalledWith(`${location.origin}/campaigns/${campaignId}`);
    expect(compiled.textContent).toContain('Link copied.');
  });

  it('shows the link when the clipboard is unavailable', async () => {
    Object.defineProperty(navigator, 'clipboard', { configurable: true, value: undefined });
    const { fixture } = await render();
    const compiled = fixture.nativeElement as HTMLElement;
    compiled.querySelector<HTMLButtonElement>('button')?.click();
    await fixture.whenStable();
    fixture.detectChanges();

    const dialog = document.querySelector('[role="dialog"]');
    expect(dialog?.textContent).toContain('Share Border War');
    expect(dialog?.querySelector('input')?.value).toBe(`${location.origin}/campaigns/${campaignId}`);
    expect(dialog?.textContent).toContain('Close');
  });
});

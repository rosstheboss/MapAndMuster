import { provideZonelessChangeDetection } from '@angular/core';
import { TestBed } from '@angular/core/testing';

import { CampaignLogComponent } from './campaign-log.component';

describe('CampaignLogComponent', () => {
  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [CampaignLogComponent],
      providers: [provideZonelessChangeDetection()],
    }).compileComponents();
  });

  it('renders campaign events and member chat in log format', () => {
    const fixture = TestBed.createComponent(CampaignLogComponent);
    fixture.componentRef.setInput('timeZoneId', 'America/New_York');
    fixture.componentRef.setInput('canChat', true);
    fixture.componentRef.setInput('members', [{ userId: '1', username: 'northplayer', displayName: 'northplayer' }]);
    fixture.componentRef.setInput('entries', [
      {
        id: 'log-1',
        occurredUtc: '2026-08-15T20:45:23-04:00',
        kind: 'PlayerChat',
        originator: 'northplayer',
        summary: 'Hey, everybody! This is a message to all of you.',
        territoryId: null,
        forceId: null,
        battleId: null,
        isSystemAdjustment: false,
      },
    ]);
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.textContent).toContain('(2026-08-15 08:45:23 PM EDT)');
    expect(compiled.textContent).toContain('northplayer:');
    expect(compiled.textContent).toContain('Hey, everybody! This is a message to all of you.');
    expect(compiled.querySelector('textarea')).toBeTruthy();
    expect(compiled.querySelector('textarea')?.getAttribute('rows')).toBe('1');
  });

  it('autocompletes a campaign member after @', () => {
    const fixture = TestBed.createComponent(CampaignLogComponent);
    fixture.componentRef.setInput('canChat', true);
    fixture.componentRef.setInput('members', [{ userId: '1', username: 'northplayer', displayName: 'northplayer' }]);
    fixture.detectChanges();

    const page = fixture.componentInstance as unknown as {
      onDraftInput(value: string): void;
      complete(member: { userId: string; username: string; displayName: string }): void;
      draft: () => string;
    };
    page.onDraftInput('Hi @nor');
    fixture.detectChanges();
    expect((fixture.nativeElement as HTMLElement).textContent).toContain('@northplayer');
    page.complete({ userId: '1', username: 'northplayer', displayName: 'northplayer' });
    expect(page.draft()).toBe('Hi @northplayer ');
  });

  it('hides compose controls until the viewer has joined', () => {
    const fixture = TestBed.createComponent(CampaignLogComponent);
    fixture.componentRef.setInput('canChat', false);
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.querySelector('textarea')).toBeNull();
    expect(compiled.textContent).toContain('Join this campaign to chat in the log.');
  });

  it('restores the draft and shows an error when sending fails', async () => {
    const fixture = TestBed.createComponent(CampaignLogComponent);
    fixture.componentRef.setInput('canChat', true);
    fixture.detectChanges();

    const page = fixture.componentInstance as unknown as {
      onDraftInput(value: string): void;
      submit(): void;
      draft: () => string;
    };
    page.onDraftInput('Hello from the frontier');
    page.submit();
    expect(page.draft()).toBe('');

    fixture.componentRef.setInput('sendError', 'Unable to send that chat message.');
    fixture.detectChanges();
    await fixture.whenStable();
    expect(page.draft()).toBe('Hello from the frontier');
    expect((fixture.nativeElement as HTMLElement).textContent).toContain('Unable to send that chat message.');
  });
});

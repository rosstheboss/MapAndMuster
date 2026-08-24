import { provideZonelessChangeDetection, signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';

import { AuthService } from '../../core/auth/auth.service';
import { CampaignLogComponent } from './campaign-log.component';

describe('CampaignLogComponent', () => {
  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [CampaignLogComponent],
      providers: [
        provideZonelessChangeDetection(),
        provideRouter([]),
        { provide: AuthService, useValue: { currentUser: signal(null) } },
      ],
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
        originatorUsername: 'northplayer',
        summary: 'Hey, everybody! This is a message to all of you.',
        territoryId: null,
        forceId: null,
        battleId: null,
        isSystemAdjustment: false,
      },
    ]);
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.textContent).toContain('(August 15, 2026, 8:45:23 PM EDT)');
    expect(compiled.textContent).toContain('northplayer:');
    expect(compiled.querySelector('a[href^="/users/northplayer"]')?.textContent.trim()).toBe('northplayer:');
    expect(compiled.textContent).toContain('Hey, everybody! This is a message to all of you.');
    expect(compiled.querySelector('textarea')).toBeTruthy();
    expect(compiled.querySelector('textarea')?.getAttribute('rows')).toBe('1');
    expect(compiled.textContent).toContain('Public chat');
    expect(compiled.textContent).toContain('Private chats');
    expect(compiled.textContent).toContain('Game log');
    expect(compiled.querySelector<HTMLInputElement>('#chat-recipient')?.value).toBe('Everyone');
  });

  it('links chat originators and mentions to public profiles', () => {
    const fixture = TestBed.createComponent(CampaignLogComponent);
    fixture.componentRef.setInput('members', [
      { userId: '1', username: 'northplayer', displayName: 'northplayer' },
      { userId: '2', username: 'southplayer', displayName: 'Ada Lovelace' },
    ]);
    fixture.componentRef.setInput('entries', [
      {
        id: 'log-chat',
        occurredUtc: '2026-08-15T20:45:23-04:00',
        kind: 'PlayerChat',
        originator: 'northplayer',
        originatorUsername: 'northplayer',
        summary: 'Hello @southplayer',
        territoryId: null,
        forceId: null,
        battleId: null,
        isSystemAdjustment: false,
      },
      {
        id: 'log-game',
        occurredUtc: '2026-08-15T20:46:23-04:00',
        kind: 'CampaignStarted',
        originator: 'Campaign',
        originatorUsername: null,
        summary: 'The campaign has started.',
        territoryId: null,
        forceId: null,
        battleId: null,
        isSystemAdjustment: false,
      },
    ]);
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.querySelector('a[href^="/users/northplayer"]')?.textContent.trim()).toBe('northplayer:');
    expect(compiled.querySelector('a[href^="/users/southplayer"]')?.textContent.trim()).toBe('@southplayer');
    expect(compiled.textContent).toContain('Campaign:');
    expect([...compiled.querySelectorAll('a')].some((link) => link.textContent.trim() === 'Campaign')).toBe(false);
  });

  it('autocompletes a recipient username from the Send to field', () => {
    const fixture = TestBed.createComponent(CampaignLogComponent);
    fixture.componentRef.setInput('canChat', true);
    fixture.componentRef.setInput('members', [{ userId: '2', username: 'bobisthebest', displayName: 'Bob' }]);
    fixture.componentRef.setInput('channels', [
      { kind: 'Public', targetId: null, label: 'Everyone' },
      { kind: 'Direct', targetId: '2', label: 'Bob' },
    ]);
    fixture.detectChanges();

    const page = fixture.componentInstance as unknown as {
      onRecipientInput(value: string): void;
      onRecipientKeydown(event: KeyboardEvent): void;
      recipientQuery: () => string;
      selectedChannel: () => { kind: string; targetId: string | null };
    };
    page.onRecipientInput('bobis');
    fixture.detectChanges();
    expect((fixture.nativeElement as HTMLElement).textContent).toContain('Bob (bobisthebest)');
    page.onRecipientKeydown(new KeyboardEvent('keydown', { key: 'Enter' }));
    expect(page.recipientQuery()).toBe('bobisthebest');
    expect(page.selectedChannel()).toEqual({ kind: 'Direct', targetId: '2', label: 'Bob' });
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

  it('hides private chat until the viewer enables it', () => {
    const fixture = TestBed.createComponent(CampaignLogComponent);
    fixture.componentRef.setInput('entries', [
      {
        id: 'log-public',
        occurredUtc: '2026-08-15T20:45:23-04:00',
        kind: 'PlayerChat',
        originator: 'northplayer',
        summary: 'Hello everyone',
        territoryId: null,
        forceId: null,
        battleId: null,
        isSystemAdjustment: false,
        isPrivate: false,
      },
      {
        id: 'log-private',
        occurredUtc: '2026-08-15T20:46:23-04:00',
        kind: 'PlayerChat',
        originator: 'northplayer',
        summary: 'Secret to south',
        territoryId: null,
        forceId: null,
        battleId: null,
        isSystemAdjustment: false,
        isPrivate: true,
        channelLabel: 'southplayer',
      },
    ]);
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.textContent).toContain('Hello everyone');
    expect(compiled.textContent).not.toContain('Secret to south');
  });

  it('lets staff choose a public and game log download', () => {
    const fixture = TestBed.createComponent(CampaignLogComponent);
    fixture.componentRef.setInput('canExport', true);
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.textContent).toContain('Download log');
    const download = [...compiled.querySelectorAll('button')].find(
      (element) => element.textContent.trim() === 'Download log',
    );
    expect(download).toBeTruthy();
    download!.click();
    fixture.detectChanges();
    expect(compiled.textContent).toContain('Download campaign log');
    expect(compiled.textContent).toContain('Text (.txt)');
    expect(compiled.textContent).toContain('CSV (.csv)');

    const page = fixture.componentInstance as unknown as {
      exportPublicChat: { set(value: boolean): void };
      confirmExport(): void;
    };
    let emitted: { includePublicChat: boolean; includeGameLog: boolean; format: string } | null = null;
    fixture.componentInstance.downloadLog.subscribe((request) => {
      emitted = request;
    });
    page.exportPublicChat.set(false);
    fixture.detectChanges();
    page.confirmExport();
    expect(emitted).toEqual({ includePublicChat: false, includeGameLog: true, format: 'txt' });
  });

  it('hides log download unless the viewer may export', () => {
    const fixture = TestBed.createComponent(CampaignLogComponent);
    fixture.detectChanges();
    expect((fixture.nativeElement as HTMLElement).textContent).not.toContain('Download log');
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

  it('restores the last chat recipient from the initial channel key', () => {
    const fixture = TestBed.createComponent(CampaignLogComponent);
    fixture.componentRef.setInput('canChat', true);
    fixture.componentRef.setInput('members', [{ userId: '2', username: 'bobisthebest', displayName: 'Bob' }]);
    fixture.componentRef.setInput('channels', [
      { kind: 'Public', targetId: null, label: 'Everyone' },
      { kind: 'Direct', targetId: '2', label: 'Bob' },
    ]);
    fixture.componentRef.setInput('initialChannelKey', 'Direct:2');
    fixture.detectChanges();

    const page = fixture.componentInstance as unknown as {
      recipientQuery: () => string;
      selectedChannel: () => { kind: string; targetId: string | null };
    };
    expect(page.selectedChannel()).toEqual({ kind: 'Direct', targetId: '2', label: 'Bob' });
    expect(page.recipientQuery()).toBe('bobisthebest');
    expect((fixture.nativeElement as HTMLElement).querySelector<HTMLInputElement>('#chat-recipient')?.value).toBe(
      'bobisthebest',
    );
  });
});

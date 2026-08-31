import { provideZonelessChangeDetection, signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';

import { AuthService } from '../../core/auth/auth.service';
import { SiteChatComponent } from './site-chat.component';

describe('SiteChatComponent', () => {
  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [SiteChatComponent],
      providers: [
        provideZonelessChangeDetection(),
        provideRouter([]),
        { provide: AuthService, useValue: { currentUser: signal(null) } },
      ],
    }).compileComponents();
  });

  it('renders public site chat above compose controls', () => {
    const fixture = TestBed.createComponent(SiteChatComponent);
    fixture.componentRef.setInput('timeZoneId', 'America/New_York');
    fixture.componentRef.setInput('canChat', true);
    fixture.componentRef.setInput('viewerUserId', '1');
    fixture.componentRef.setInput('members', [{ userId: '1', username: 'northplayer', displayName: 'northplayer' }]);
    fixture.componentRef.setInput('messages', [
      {
        id: 'msg-1',
        postedUtc: '2026-08-15T20:45:23-04:00',
        authorUserId: '1',
        authorUsername: 'northplayer',
        authorDisplayName: 'northplayer',
        body: 'Hello from the public board.',
        language: 'English',
        kind: 'Player',
        targetUserId: null,
        targetUsername: null,
        targetDisplayName: null,
      },
    ]);
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.textContent).toContain('Site chat');
    expect(compiled.textContent).toContain('Hello from the public board.');
    expect(compiled.textContent).toContain('English');
    expect(compiled.querySelector('textarea')).toBeTruthy();
    expect(compiled.textContent).not.toContain('Private chats');
    const options = compiled.querySelector<HTMLDetailsElement>('details.chat-options');
    expect(options).toBeTruthy();
    expect(options?.open).toBe(false);
    const send = [...compiled.querySelectorAll('button')].find((item) => item.textContent.trim() === 'Send');
    expect(send).toBeTruthy();
    expect(Boolean(send && options && send.compareDocumentPosition(options) & Node.DOCUMENT_POSITION_FOLLOWING)).toBe(
      true,
    );
  });

  it('exposes mention suggestions as a combobox', () => {
    const fixture = TestBed.createComponent(SiteChatComponent);
    fixture.componentRef.setInput('canChat', true);
    fixture.componentRef.setInput('members', [{ userId: '1', username: 'northplayer', displayName: 'northplayer' }]);
    fixture.detectChanges();

    const page = fixture.componentInstance as unknown as {
      onDraftInput(value: string): void;
    };
    page.onDraftInput('Hi @nor');
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    const combobox = compiled.querySelector('.mention-combobox');
    expect(combobox?.getAttribute('role')).toBe('combobox');
    expect(combobox?.getAttribute('aria-labelledby')).toBe('site-chat-message-label');
    expect(combobox?.getAttribute('aria-expanded')).toBe('true');
    expect(combobox?.getAttribute('aria-controls')).toBe('site-chat-suggest');
    expect(compiled.querySelector('textarea')?.getAttribute('aria-activedescendant')).toBe(
      'site-chat-suggest-option-0',
    );
    expect(compiled.querySelector('#site-chat-suggest-option-0')?.getAttribute('role')).toBe('option');
  });

  it('hides messages whose language filter is off', () => {
    const fixture = TestBed.createComponent(SiteChatComponent);
    fixture.componentRef.setInput('visibleLanguages', ['English']);
    fixture.componentRef.setInput('messages', [
      {
        id: 'en',
        postedUtc: '2026-08-15T20:45:23-04:00',
        authorUserId: '1',
        authorUsername: 'ada',
        authorDisplayName: 'ada',
        body: 'Hello',
        language: 'English',
        kind: 'Player',
        targetUserId: null,
        targetUsername: null,
        targetDisplayName: null,
      },
      {
        id: 'es',
        postedUtc: '2026-08-15T20:46:23-04:00',
        authorUserId: '2',
        authorUsername: 'bob',
        authorDisplayName: 'bob',
        body: 'Hola',
        language: 'Spanish',
        kind: 'Player',
        targetUserId: null,
        targetUsername: null,
        targetDisplayName: null,
      },
    ]);
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.textContent).toContain('Hello');
    expect(compiled.textContent).not.toContain('Hola');
  });

  it('lets a viewer block another author', () => {
    const fixture = TestBed.createComponent(SiteChatComponent);
    const blocked: { userId: string; blocked: boolean }[] = [];
    fixture.componentRef.setInput('viewerUserId', '1');
    fixture.componentRef.setInput('canChat', true);
    fixture.componentRef.setInput('messages', [
      {
        id: 'msg-2',
        postedUtc: '2026-08-15T20:45:23-04:00',
        authorUserId: '2',
        authorUsername: 'bob',
        authorDisplayName: 'bob',
        body: 'Hi there',
        language: 'English',
        kind: 'Player',
        targetUserId: null,
        targetUsername: null,
        targetDisplayName: null,
      },
    ]);
    fixture.detectChanges();
    fixture.componentInstance.blockChange.subscribe((value) => blocked.push(value));
    const button = [...(fixture.nativeElement as HTMLElement).querySelectorAll('button')].find(
      (item) => item.textContent.trim() === 'Block',
    );
    expect(button).toBeTruthy();
    button!.click();
    expect(blocked).toEqual([{ userId: '2', blocked: true }]);
  });

  it('shows administrator compose controls and an Admin badge', () => {
    const fixture = TestBed.createComponent(SiteChatComponent);
    fixture.componentRef.setInput('canChat', true);
    fixture.componentRef.setInput('canSendAdminMessages', true);
    fixture.componentRef.setInput('viewerUserId', '1');
    fixture.componentRef.setInput('members', [
      { userId: '1', username: 'ada', displayName: 'ada' },
      { userId: '2', username: 'bob', displayName: 'bob' },
    ]);
    fixture.componentRef.setInput('messages', [
      {
        id: 'admin-1',
        postedUtc: '2026-08-15T20:45:23-04:00',
        authorUserId: '1',
        authorUsername: 'ada',
        authorDisplayName: 'ada',
        body: 'Please read the news.',
        language: 'English',
        kind: 'Admin',
        targetUserId: null,
        targetUsername: null,
        targetDisplayName: null,
      },
    ]);
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.textContent).toContain('Send as administrator');
    expect(compiled.textContent).toContain('Admin');
    expect(compiled.textContent).toContain('Please read the news.');
  });

  it('keeps language filters and the block list in a collapsed subpanel below Send', () => {
    const fixture = TestBed.createComponent(SiteChatComponent);
    fixture.componentRef.setInput('canChat', true);
    fixture.componentRef.setInput('blockedUsers', [{ userId: '2', username: 'bob', displayName: 'bob' }]);
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    const options = compiled.querySelector<HTMLDetailsElement>('details.chat-options');
    expect(options?.open).toBe(false);
    expect(options?.querySelector('legend')?.textContent).toContain('Languages');
    expect(options?.querySelectorAll('input[type="checkbox"]').length).toBeGreaterThan(0);
    expect(options?.querySelector('#site-chat-blocked-heading')?.textContent).toContain('Blocked people (1)');
    options?.querySelector('summary')?.click();
    fixture.detectChanges();
    expect(compiled.querySelector<HTMLDetailsElement>('details.chat-options')?.open).toBe(true);
  });
});

import { Component, computed, effect, inject, input, output, signal, viewChild } from '@angular/core';
import type { ElementRef } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';

import { AuthService } from '../../core/auth/auth.service';
import { CHAT_LANGUAGES, type ChatLanguage } from '../../core/chat/chat-languages';
import type { SiteChatMember, SiteChatMessage, SiteChatSend } from '../../core/chat/site-chat.models';
import {
  campaignLogComposerSize,
  formatLogTimestamp,
  mentionQuery,
  splitLogMessage,
} from '../../core/campaigns/campaign-log';

@Component({
  selector: 'app-site-chat',
  imports: [FormsModule, RouterLink],
  templateUrl: './site-chat.component.html',
  styleUrl: './site-chat.component.css',
})
export class SiteChatComponent {
  private readonly router = inject(Router);
  private readonly auth = inject(AuthService);
  readonly messages = input<readonly SiteChatMessage[]>([]);
  readonly members = input<readonly SiteChatMember[]>([]);
  readonly blockedUsers = input<readonly SiteChatMember[]>([]);
  readonly viewerUserId = input<string | null>(null);
  readonly timeZoneId = input<string | null>(null);
  readonly canChat = input(false);
  readonly canSendAdminMessages = input(false);
  readonly sending = input(false);
  readonly sendError = input<string | null>(null);
  readonly expanded = input(true);
  readonly composeLanguage = input<ChatLanguage>('English');
  readonly visibleLanguages = input<readonly ChatLanguage[]>(CHAT_LANGUAGES);

  readonly send = output<SiteChatSend>();
  readonly expandedChange = output<boolean>();
  readonly composeLanguageChange = output<ChatLanguage>();
  readonly visibleLanguagesChange = output<ChatLanguage[]>();
  readonly blockChange = output<{ userId: string; blocked: boolean }>();

  protected readonly languages = CHAT_LANGUAGES;
  protected readonly draft = signal('');
  protected readonly highlight = signal(0);
  protected readonly sendAsAdmin = signal(false);
  protected readonly recipientQuery = signal('Everyone');
  protected readonly recipientOpen = signal(false);
  protected readonly recipientHighlight = signal(0);
  protected readonly targetUserId = signal<string | null>(null);
  protected readonly optionsOpen = signal(false);
  private readonly scroller = viewChild<ElementRef<HTMLElement>>('scroller');
  private readonly composer = viewChild<ElementRef<HTMLTextAreaElement>>('composer');
  private readonly recipientInput = viewChild<ElementRef<HTMLInputElement>>('recipient');
  private heldMessage = '';
  private sawSending = false;
  private restoredScroll = false;

  protected readonly visibleMessages = computed(() => {
    const allowed = new Set(this.visibleLanguages());
    return this.messages().filter((message) => allowed.has(message.language as ChatLanguage));
  });
  protected readonly blockedIds = computed(() => new Set(this.blockedUsers().map((user) => user.userId)));
  protected readonly recipientOptions = computed(() =>
    this.members().filter((member) => member.userId !== this.viewerUserId()),
  );
  protected readonly recipientSuggestions = computed(() => {
    const query = this.recipientQuery().trim().toLowerCase();
    const options = this.recipientOptions();
    if (query.length === 0 || query === 'everyone') {
      return options;
    }

    return options.filter((member) => {
      return member.username.toLowerCase().includes(query) || member.displayName.toLowerCase().includes(query);
    });
  });
  protected readonly suggestions = computed(() => {
    const current = mentionQuery(this.draft(), this.draft().length);
    if (!current) {
      return [];
    }

    const needle = current.query.trim().toLowerCase();
    return this.members().filter((member) => {
      const username = member.username.toLowerCase();
      const display = member.displayName.toLowerCase();
      return needle.length === 0
        ? true
        : username.startsWith(needle) || display.startsWith(needle) || display.includes(needle);
    });
  });

  constructor() {
    effect(() => {
      this.visibleMessages();
      queueMicrotask(() => {
        const element = this.scroller()?.nativeElement;
        if (!element) {
          return;
        }

        if (!this.restoredScroll) {
          this.restoredScroll = true;
        }

        element.scrollTop = element.scrollHeight;
      });
    });
    effect(() => {
      this.draft();
      this.canChat();
      queueMicrotask(() => this.resizeComposer());
    });
    effect(() => {
      const sending = this.sending();
      const error = this.sendError();
      queueMicrotask(() => {
        if (sending) {
          this.sawSending = true;
          return;
        }

        if (error && this.heldMessage) {
          this.draft.set(this.heldMessage);
          this.heldMessage = '';
          this.sawSending = false;
          return;
        }

        if (this.sawSending && !error) {
          this.heldMessage = '';
          this.sawSending = false;
        }
      });
    });
  }

  protected onToggle(event: Event): void {
    const details = event.currentTarget as HTMLDetailsElement;
    this.expandedChange.emit(details.open);
  }

  protected originatorText(message: SiteChatMessage): string {
    return `${message.authorDisplayName}:`;
  }

  protected formatTimestamp(value: string): string {
    return formatLogTimestamp(value, this.timeZoneId(), this.auth.currentUser()?.dateTimeDisplayFormat);
  }

  protected parts(summary: string): { text: string; mention: boolean; username?: string | null }[] {
    return splitLogMessage(summary, this.members());
  }

  protected profileQuery(): { from: string } {
    return { from: this.router.url };
  }

  protected languageOn(language: ChatLanguage): boolean {
    return this.visibleLanguages().includes(language);
  }

  protected toggleLanguage(language: ChatLanguage, enabled: boolean): void {
    const current = new Set(this.visibleLanguages());
    if (enabled) {
      current.add(language);
    } else {
      current.delete(language);
    }

    this.visibleLanguagesChange.emit(CHAT_LANGUAGES.filter((item) => current.has(item)));
  }

  protected onLanguageToggle(language: ChatLanguage, event: Event): void {
    this.toggleLanguage(language, (event.target as HTMLInputElement).checked);
  }

  protected onOptionsToggle(event: Event): void {
    event.stopPropagation();
    this.optionsOpen.set((event.currentTarget as HTMLDetailsElement).open);
  }

  protected onComposeLanguage(value: string): void {
    if (CHAT_LANGUAGES.includes(value as ChatLanguage)) {
      this.composeLanguageChange.emit(value as ChatLanguage);
    }
  }

  protected onDraftInput(value: string): void {
    this.draft.set(value);
    this.highlight.set(0);
  }

  protected channelBadge(message: SiteChatMessage): string | null {
    if (message.kind !== 'Admin') {
      return null;
    }

    if (message.targetDisplayName) {
      return `Admin to ${message.targetDisplayName}`;
    }

    return 'Admin';
  }

  protected canBlock(message: SiteChatMessage): boolean {
    const viewer = this.viewerUserId();
    return !!viewer && message.authorUserId !== viewer;
  }

  protected isBlocked(userId: string): boolean {
    return this.blockedIds().has(userId);
  }

  protected toggleBlock(userId: string): void {
    this.blockChange.emit({ userId, blocked: !this.isBlocked(userId) });
  }

  protected onRecipientInput(value: string): void {
    this.recipientQuery.set(value);
    this.recipientOpen.set(true);
    this.recipientHighlight.set(0);
  }

  protected onRecipientDomInput(event: Event): void {
    this.onRecipientInput((event.target as HTMLInputElement).value);
  }

  protected onRecipientFocus(): void {
    this.recipientOpen.set(true);
    this.recipientHighlight.set(0);
    queueMicrotask(() => this.recipientInput()?.nativeElement.select());
  }

  protected onRecipientFocusOut(event: FocusEvent): void {
    const next = event.relatedTarget as Node | null;
    if (next && (event.currentTarget as HTMLElement).contains(next)) {
      return;
    }

    this.snapRecipient();
    this.recipientOpen.set(false);
  }

  protected onRecipientKeydown(event: KeyboardEvent): void {
    const options = this.recipientSuggestions();
    if (event.key === 'ArrowDown') {
      event.preventDefault();
      this.recipientOpen.set(true);
      if (options.length === 0) {
        return;
      }

      this.recipientHighlight.update((index) => (index + 1) % options.length);
      return;
    }

    if (event.key === 'ArrowUp') {
      event.preventDefault();
      this.recipientOpen.set(true);
      if (options.length === 0) {
        return;
      }

      this.recipientHighlight.update((index) => (index - 1 + options.length) % options.length);
      return;
    }

    if (event.key === 'Enter') {
      event.preventDefault();
      const selected = options.at(this.recipientHighlight());
      if (this.recipientQuery().trim().toLowerCase() === 'everyone') {
        this.selectEveryone();
        return;
      }

      if (selected) {
        this.selectRecipient(selected);
      }

      return;
    }

    if (event.key === 'Escape') {
      this.snapRecipient();
      this.recipientOpen.set(false);
    }
  }

  protected selectEveryone(): void {
    this.targetUserId.set(null);
    this.recipientQuery.set('Everyone');
    this.recipientOpen.set(false);
  }

  protected selectRecipient(member: SiteChatMember): void {
    this.targetUserId.set(member.userId);
    this.recipientQuery.set(member.username);
    this.recipientOpen.set(false);
    this.recipientHighlight.set(0);
  }

  protected recipientOptionLabel(member: SiteChatMember): string {
    if (member.displayName.toLowerCase() === member.username.toLowerCase()) {
      return member.username;
    }

    return `${member.displayName} (${member.username})`;
  }

  protected onKeydown(event: KeyboardEvent): void {
    const options = this.suggestions();
    if (event.key === 'ArrowDown' && options.length > 0) {
      event.preventDefault();
      this.highlight.update((index) => (index + 1) % options.length);
      return;
    }

    if (event.key === 'ArrowUp' && options.length > 0) {
      event.preventDefault();
      this.highlight.update((index) => (index - 1 + options.length) % options.length);
      return;
    }

    if (event.key === 'Enter' && !event.shiftKey) {
      event.preventDefault();
      if (options.length > 0) {
        this.complete(options[this.highlight()] ?? options[0]);
        return;
      }

      this.submit();
    }
  }

  protected complete(member: SiteChatMember): void {
    const current = mentionQuery(this.draft(), this.draft().length);
    if (!current) {
      return;
    }

    this.draft.set(`${this.draft().slice(0, current.start)}@${member.username} `);
    this.highlight.set(0);
  }

  protected submit(): void {
    const message = this.draft().trim();
    if (!message || this.sending() || !this.canChat()) {
      return;
    }

    this.heldMessage = message;
    this.send.emit({
      message,
      language: this.composeLanguage(),
      sendAsAdministrator: this.canSendAdminMessages() && this.sendAsAdmin(),
      targetUserId: this.canSendAdminMessages() && this.sendAsAdmin() ? this.targetUserId() : null,
    });
    this.draft.set('');
  }

  protected suggestionLabel(member: SiteChatMember): string {
    if (member.displayName.toLowerCase() === member.username.toLowerCase()) {
      return `@${member.username}`;
    }

    return `${member.displayName} (@${member.username})`;
  }

  private snapRecipient(): void {
    const query = this.recipientQuery().trim().toLowerCase();
    if (query.length === 0 || query === 'everyone') {
      this.selectEveryone();
      return;
    }

    const match = this.recipientOptions().find(
      (member) => member.username.toLowerCase() === query || member.displayName.toLowerCase() === query,
    );
    if (match) {
      this.selectRecipient(match);
      return;
    }

    if (this.targetUserId()) {
      const current = this.recipientOptions().find((member) => member.userId === this.targetUserId());
      if (current) {
        this.recipientQuery.set(current.username);
        return;
      }
    }

    this.selectEveryone();
  }

  private resizeComposer(): void {
    const element = this.composer()?.nativeElement;
    if (!element) {
      return;
    }

    const styles = globalThis.getComputedStyle(element);
    const parsedLineHeight = Number.parseFloat(styles.lineHeight);
    const lineHeight = Number.isFinite(parsedLineHeight) && parsedLineHeight > 0 ? parsedLineHeight : 20;
    const padding = Number.parseFloat(styles.paddingTop) + Number.parseFloat(styles.paddingBottom);
    const border = Number.parseFloat(styles.borderTopWidth) + Number.parseFloat(styles.borderBottomWidth);
    const chrome = (Number.isFinite(padding) ? padding : 0) + (Number.isFinite(border) ? border : 0);
    element.style.overflowY = 'hidden';
    element.style.height = 'auto';
    const size = campaignLogComposerSize(element.scrollHeight, lineHeight, chrome);
    element.style.height = `${size.height}px`;
    element.style.overflowY = size.overflowY;
  }
}

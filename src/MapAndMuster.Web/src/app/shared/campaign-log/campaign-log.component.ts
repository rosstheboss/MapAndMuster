import { Component, computed, effect, inject, input, output, signal, viewChild } from '@angular/core';
import type { ElementRef } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';

import { AuthService } from '../../core/auth/auth.service';
import type { CampaignChatSend, ChatChannel, PlayLogEntry } from '../../core/campaigns/campaign.models';
import {
  campaignLogComposerSize,
  filterCampaignLog,
  filterChatRecipients,
  formatLogTimestamp,
  matchChatRecipient,
  mentionQuery,
  recipientFieldLabel,
  recipientSuggestionLabel,
  splitLogMessage,
  type CampaignLogExportFormat,
  type CampaignLogExportRequest,
  type CampaignLogMember,
} from '../../core/campaigns/campaign-log';

@Component({
  selector: 'app-campaign-log',
  imports: [FormsModule, RouterLink],
  templateUrl: './campaign-log.component.html',
  styleUrl: './campaign-log.component.css',
})
export class CampaignLogComponent {
  private readonly router = inject(Router);
  private readonly auth = inject(AuthService);
  readonly entries = input<readonly PlayLogEntry[]>([]);
  readonly members = input<readonly CampaignLogMember[]>([]);
  readonly channels = input<readonly ChatChannel[]>([]);
  readonly canChat = input(false);
  readonly timeZoneId = input<string | null>(null);
  readonly sending = input(false);
  readonly sendError = input<string | null>(null);
  readonly expanded = input(true);
  readonly canExport = input(false);
  readonly exporting = input(false);
  readonly initialChannelKey = input('Public:');
  readonly initialScrollTop = input<number | null>(null);

  readonly send = output<CampaignChatSend>();
  readonly downloadLog = output<CampaignLogExportRequest>();
  readonly expandedChange = output<boolean>();
  readonly channelChange = output<string>();
  readonly scrollChange = output<number>();

  protected readonly draft = signal('');
  protected readonly highlight = signal(0);
  protected readonly channelKey = signal('Public:');
  protected readonly recipientQuery = signal('Everyone');
  protected readonly recipientOpen = signal(false);
  protected readonly recipientHighlight = signal(0);
  protected readonly showPublicChat = signal(true);
  protected readonly showPrivateChat = signal(false);
  protected readonly showGameLog = signal(true);
  protected readonly exportOpen = signal(false);
  protected readonly exportPublicChat = signal(true);
  protected readonly exportGameLog = signal(true);
  protected readonly exportFormat = signal<CampaignLogExportFormat>('txt');
  private readonly scroller = viewChild<ElementRef<HTMLElement>>('scroller');
  private readonly composer = viewChild<ElementRef<HTMLTextAreaElement>>('composer');
  private readonly recipientInput = viewChild<ElementRef<HTMLInputElement>>('recipient');
  private heldMessage = '';
  private sawSending = false;
  private appliedInitialChannel = false;
  private restoredScroll = false;
  protected readonly availableChannels = computed(() => {
    const listed = this.channels();
    return listed.length > 0 ? listed : [{ kind: 'Public', targetId: null, label: 'Everyone' }];
  });
  protected readonly visibleEntries = computed(() =>
    filterCampaignLog(this.entries(), this.showPublicChat(), this.showPrivateChat(), this.showGameLog()),
  );
  protected readonly recipientSuggestions = computed(() => {
    const query = this.recipientQuery();
    const committed = recipientFieldLabel(this.selectedChannel(), this.members());
    if (query.trim().toLowerCase() === committed.toLowerCase()) {
      return this.availableChannels();
    }

    return filterChatRecipients(this.availableChannels(), this.members(), query);
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
      const key = this.initialChannelKey();
      const channels = this.availableChannels();
      const members = this.members();
      if (this.appliedInitialChannel || channels.length === 0) {
        return;
      }

      this.appliedInitialChannel = true;
      const match = channels.find((channel) => this.channelOptionValue(channel) === key) ?? channels[0];
      this.channelKey.set(this.channelOptionValue(match));
      this.recipientQuery.set(recipientFieldLabel(match, members));
      if (match.kind !== 'Public') {
        this.showPrivateChat.set(true);
      }
    });
    effect(() => {
      this.visibleEntries();
      queueMicrotask(() => {
        const element = this.scroller()?.nativeElement;
        if (!element) {
          return;
        }

        if (!this.restoredScroll) {
          this.restoredScroll = true;
          const initial = this.initialScrollTop();
          if (initial !== null) {
            element.scrollTop = initial;
            return;
          }
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

  protected onScroll(event: Event): void {
    this.scrollChange.emit((event.currentTarget as HTMLElement).scrollTop);
  }

  protected originatorText(entry: PlayLogEntry): string {
    return `${entry.originator}:`;
  }

  protected canConfirmExport(): boolean {
    return this.exportPublicChat() || this.exportGameLog();
  }

  protected openExportDialog(): void {
    this.exportPublicChat.set(true);
    this.exportGameLog.set(true);
    this.exportFormat.set('txt');
    this.exportOpen.set(true);
  }

  protected closeExportDialog(): void {
    this.exportOpen.set(false);
  }

  protected setExportFormat(value: string): void {
    if (value === 'txt' || value === 'csv') {
      this.exportFormat.set(value);
    }
  }

  protected confirmExport(): void {
    if (!this.canConfirmExport() || this.exporting()) {
      return;
    }

    this.downloadLog.emit({
      includePublicChat: this.exportPublicChat(),
      includeGameLog: this.exportGameLog(),
      format: this.exportFormat(),
    });
    this.exportOpen.set(false);
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

  protected onDraftInput(value: string): void {
    this.draft.set(value);
    this.highlight.set(0);
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
      const selected =
        options.at(this.recipientHighlight()) ??
        matchChatRecipient(this.availableChannels(), this.members(), this.recipientQuery());
      if (selected) {
        this.selectRecipient(selected);
      }

      return;
    }

    if (event.key === 'Escape') {
      this.recipientQuery.set(recipientFieldLabel(this.selectedChannel(), this.members()));
      this.recipientOpen.set(false);
    }
  }

  protected selectRecipient(channel: ChatChannel): void {
    this.channelKey.set(this.channelOptionValue(channel));
    this.recipientQuery.set(recipientFieldLabel(channel, this.members()));
    this.recipientOpen.set(false);
    this.recipientHighlight.set(0);
    this.channelChange.emit(this.channelKey());
    if (channel.kind !== 'Public') {
      this.showPrivateChat.set(true);
    }
  }

  protected recipientOptionLabel(channel: ChatChannel): string {
    return recipientSuggestionLabel(channel, this.members());
  }

  protected channelOptionValue(channel: ChatChannel): string {
    return `${channel.kind}:${channel.targetId ?? ''}`;
  }

  protected selectedChannel(): ChatChannel {
    const key = this.channelKey();
    for (const channel of this.availableChannels()) {
      if (this.channelOptionValue(channel) === key) {
        return channel;
      }
    }

    return this.availableChannels()[0];
  }

  protected channelBadge(entry: PlayLogEntry): string | null {
    if (entry.kind !== 'PlayerChat' || !entry.isPrivate) {
      return null;
    }

    return entry.channelLabel ? `Private: ${entry.channelLabel}` : 'Private';
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

  protected complete(member: CampaignLogMember): void {
    const current = mentionQuery(this.draft(), this.draft().length);
    if (!current) {
      return;
    }

    const next = `${this.draft().slice(0, current.start)}@${member.username} `;
    this.draft.set(next);
    this.highlight.set(0);
  }

  protected submit(): void {
    const message = this.draft().trim();
    const channel =
      matchChatRecipient(this.availableChannels(), this.members(), this.recipientQuery()) ??
      (this.recipientQuery().trim().toLowerCase() ===
      recipientFieldLabel(this.selectedChannel(), this.members()).toLowerCase()
        ? this.selectedChannel()
        : null);
    if (!message || !channel || this.sending() || !this.canChat()) {
      return;
    }

    this.selectRecipient(channel);
    this.heldMessage = message;
    this.send.emit({
      message,
      channelKind: channel.kind,
      targetId: channel.targetId,
    });
    this.draft.set('');
  }

  protected suggestionLabel(member: CampaignLogMember): string {
    if (member.displayName.toLowerCase() === member.username.toLowerCase()) {
      return `@${member.username}`;
    }

    return `${member.displayName} (@${member.username})`;
  }

  private snapRecipient(): ChatChannel | null {
    const match = matchChatRecipient(this.availableChannels(), this.members(), this.recipientQuery());
    if (match) {
      this.selectRecipient(match);
      return match;
    }

    const committed = this.selectedChannel();
    this.recipientQuery.set(recipientFieldLabel(committed, this.members()));
    return committed;
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

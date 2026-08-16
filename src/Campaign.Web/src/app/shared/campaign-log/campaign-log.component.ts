import { Component, computed, effect, input, output, signal, viewChild } from '@angular/core';
import type { ElementRef } from '@angular/core';
import { FormsModule } from '@angular/forms';

import type { PlayLogEntry } from '../../core/campaigns/campaign.models';
import {
  campaignLogComposerSize,
  formatLogTimestamp,
  mentionQuery,
  splitLogMessage,
  type CampaignLogMember,
} from '../../core/campaigns/campaign-log';

@Component({
  selector: 'app-campaign-log',
  imports: [FormsModule],
  templateUrl: './campaign-log.component.html',
  styleUrl: './campaign-log.component.css',
})
export class CampaignLogComponent {
  readonly entries = input<readonly PlayLogEntry[]>([]);
  readonly members = input<readonly CampaignLogMember[]>([]);
  readonly canChat = input(false);
  readonly timeZoneId = input<string | null>(null);
  readonly sending = input(false);
  readonly sendError = input<string | null>(null);
  readonly expanded = input(true);

  readonly send = output<string>();
  readonly expandedChange = output<boolean>();

  protected readonly draft = signal('');
  protected readonly highlight = signal(0);
  private readonly scroller = viewChild<ElementRef<HTMLElement>>('scroller');
  private readonly composer = viewChild<ElementRef<HTMLTextAreaElement>>('composer');
  private heldMessage = '';
  private sawSending = false;
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
      this.entries();
      queueMicrotask(() => {
        const element = this.scroller()?.nativeElement;
        if (element) {
          element.scrollTop = element.scrollHeight;
        }
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

  protected formatTimestamp(value: string): string {
    return formatLogTimestamp(value, this.timeZoneId());
  }

  protected parts(summary: string): { text: string; mention: boolean }[] {
    return splitLogMessage(summary, this.members());
  }

  protected onDraftInput(value: string): void {
    this.draft.set(value);
    this.highlight.set(0);
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
    if (!message || this.sending() || !this.canChat()) {
      return;
    }

    this.heldMessage = message;
    this.send.emit(message);
    this.draft.set('');
  }

  protected suggestionLabel(member: CampaignLogMember): string {
    if (member.displayName.toLowerCase() === member.username.toLowerCase()) {
      return `@${member.username}`;
    }

    return `${member.displayName} (@${member.username})`;
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

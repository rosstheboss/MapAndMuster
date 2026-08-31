import {
  afterNextRender,
  Component,
  ElementRef,
  Injector,
  effect,
  inject,
  input,
  output,
  viewChild,
  type OnDestroy,
} from '@angular/core';

import { AppDialogService } from './dialog.service';

const FOCUSABLE =
  'a[href], button:not([disabled]), input:not([disabled]):not([type="hidden"]), select:not([disabled]), textarea:not([disabled]), [tabindex]:not([tabindex="-1"])';

@Component({
  selector: 'app-dialog',
  templateUrl: './dialog.component.html',
  styleUrl: './dialog.component.css',
})
export class AppDialogComponent implements OnDestroy {
  readonly open = input(false);
  readonly dialogRole = input<'dialog' | 'alertdialog'>('dialog');
  readonly labelledBy = input.required<string>();
  readonly describedBy = input<string | null>(null);
  readonly cancelled = output<void>();

  private readonly host = inject<ElementRef<HTMLElement>>(ElementRef);
  private readonly dialogs = inject(AppDialogService);
  private readonly injector = inject(Injector);
  private readonly panel = viewChild<ElementRef<HTMLElement>>('panel');
  private previousFocus: HTMLElement | null = null;
  private registered = false;
  private homeParent: Node | null = null;
  private homeNextSibling: ChildNode | null = null;

  constructor() {
    effect((onCleanup) => {
      if (!this.open()) {
        return;
      }

      this.previousFocus = document.activeElement instanceof HTMLElement ? document.activeElement : null;
      this.dialogs.register();
      this.registered = true;
      this.attachToBody();
      afterNextRender(() => this.focusInitial(), { injector: this.injector });

      onCleanup(() => {
        this.release();
        this.restoreFocus();
      });
    });
  }

  ngOnDestroy(): void {
    this.detachFromBody();
  }

  protected onBackdropClick(event: Event): void {
    if (event.target === event.currentTarget) {
      this.cancelled.emit();
    }
  }

  protected onKeydown(event: KeyboardEvent): void {
    if (event.key === 'Escape') {
      event.preventDefault();
      this.cancelled.emit();
      return;
    }

    if (event.key !== 'Tab') {
      return;
    }

    const focusable = this.focusableElements();
    if (focusable.length === 0) {
      event.preventDefault();
      this.panel()?.nativeElement.focus();
      return;
    }

    const first = focusable[0];
    const last = focusable[focusable.length - 1];
    if (event.shiftKey && document.activeElement === first) {
      event.preventDefault();
      last.focus();
    } else if (!event.shiftKey && document.activeElement === last) {
      event.preventDefault();
      first.focus();
    }
  }

  private attachToBody(): void {
    if (!document.querySelector('.app-shell')) {
      return;
    }

    const host = this.host.nativeElement;
    if (host.parentElement === document.body) {
      return;
    }

    this.homeParent = host.parentNode;
    this.homeNextSibling = host.nextSibling;
    document.body.append(host);
  }

  private restoreHost(): void {
    const host = this.host.nativeElement;
    if (host.parentElement !== document.body) {
      this.homeParent = null;
      this.homeNextSibling = null;
      return;
    }

    const parent = this.homeParent;
    const next = this.homeNextSibling;
    this.homeParent = null;
    this.homeNextSibling = null;
    if (parent?.isConnected && (next === null || next.parentNode === parent)) {
      parent.insertBefore(host, next);
      return;
    }

    host.remove();
  }

  private release(): void {
    if (this.registered) {
      this.dialogs.unregister();
      this.registered = false;
    }

    this.restoreHost();
  }

  private detachFromBody(): void {
    if (this.registered) {
      this.dialogs.unregister();
      this.registered = false;
    }

    const host = this.host.nativeElement;
    const panel = this.panel()?.nativeElement ?? document.querySelector(`[aria-labelledby="${this.labelledBy()}"]`);
    panel?.closest('.app-dialog-backdrop')?.remove();
    if (host.isConnected) {
      host.remove();
    }
  }

  private focusInitial(): void {
    const panel = this.panel()?.nativeElement;
    if (!panel) {
      return;
    }

    const target = this.initialFocusTarget(panel);
    target?.focus();
  }

  private initialFocusTarget(panel: HTMLElement): HTMLElement | null {
    if (this.dialogRole() === 'alertdialog') {
      return panel.querySelector<HTMLElement>('[data-dialog-safe]') ?? panel.querySelector('h2') ?? panel;
    }

    return (
      panel.querySelector<HTMLElement>('input:not([type="hidden"]), textarea, select') ??
      panel.querySelector<HTMLElement>('[data-dialog-safe]') ??
      panel.querySelector('h2') ??
      panel
    );
  }

  private focusableElements(): HTMLElement[] {
    const panel = this.panel()?.nativeElement;
    if (!panel) {
      return [];
    }

    return [...panel.querySelectorAll<HTMLElement>(FOCUSABLE)].filter(
      (element) => !element.hasAttribute('disabled') && element.tabIndex !== -1,
    );
  }

  private restoreFocus(): void {
    const previous = this.previousFocus;
    this.previousFocus = null;
    if (previous?.isConnected) {
      previous.focus();
    }
  }
}

import { Component, inject, signal } from '@angular/core';
import { provideZonelessChangeDetection } from '@angular/core';
import { TestBed } from '@angular/core/testing';

import { AppDialogComponent } from './dialog.component';
import { AppDialogService } from './dialog.service';

@Component({
  imports: [AppDialogComponent],
  template: `
    <div class="app-shell" [attr.inert]="dialogs.hasOpen() ? '' : null">
      <button type="button" (click)="open.set(true)">Open</button>
      <input id="behind" />
    </div>
    <app-dialog
      [open]="open()"
      dialogRole="alertdialog"
      labelledBy="dlg-title"
      describedBy="dlg-desc"
      (cancelled)="open.set(false)"
    >
      <h2 id="dlg-title">Delete this campaign?</h2>
      <p id="dlg-desc">This cannot be undone.</p>
      <button type="button" class="button-danger">Delete</button>
      <button type="button" class="button-secondary" data-dialog-safe (click)="open.set(false)">Cancel</button>
    </app-dialog>
  `,
})
class HostComponent {
  readonly dialogs = inject(AppDialogService);
  readonly open = signal(false);
}

describe('AppDialogComponent', () => {
  afterEach(() => {
    document.querySelector('.app-shell')?.removeAttribute('inert');
  });

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [HostComponent],
      providers: [provideZonelessChangeDetection()],
    }).compileComponents();
  });

  it('opens as a modal, focuses the safe action, traps tab, and restores the trigger on Escape', async () => {
    const fixture = TestBed.createComponent(HostComponent);
    fixture.detectChanges();
    const compiled = fixture.nativeElement as HTMLElement;
    const trigger = compiled.querySelector('button')!;
    trigger.focus();
    trigger.click();
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();

    const dialog = document.querySelector<HTMLElement>('[role="alertdialog"]');
    expect(dialog).toBeTruthy();
    expect(dialog?.getAttribute('aria-modal')).toBe('true');
    expect(document.activeElement?.textContent.trim()).toBe('Cancel');
    expect(TestBed.inject(AppDialogService).hasOpen()).toBe(true);
    expect(document.querySelector('.app-shell')?.hasAttribute('inert')).toBe(true);

    const deleteButton = [...dialog!.querySelectorAll('button')].find(
      (button) => button.textContent.trim() === 'Delete',
    );
    const cancel = [...dialog!.querySelectorAll('button')].find((button) => button.textContent.trim() === 'Cancel');
    expect(deleteButton).toBeTruthy();
    expect(cancel).toBeTruthy();

    cancel?.dispatchEvent(new KeyboardEvent('keydown', { key: 'Tab', bubbles: true }));
    fixture.detectChanges();
    expect(document.activeElement).toBe(deleteButton);

    deleteButton?.dispatchEvent(new KeyboardEvent('keydown', { key: 'Tab', shiftKey: true, bubbles: true }));
    fixture.detectChanges();
    expect(document.activeElement).toBe(cancel);

    dialog?.dispatchEvent(new KeyboardEvent('keydown', { key: 'Escape', bubbles: true }));
    fixture.detectChanges();
    expect(document.querySelector('[role="alertdialog"]')).toBeNull();
    expect(document.activeElement).toBe(trigger);
    expect(TestBed.inject(AppDialogService).hasOpen()).toBe(false);
  });

  it('closes when the backdrop is clicked', async () => {
    const fixture = TestBed.createComponent(HostComponent);
    fixture.detectChanges();
    const compiled = fixture.nativeElement as HTMLElement;
    compiled.querySelector('button')?.click();
    fixture.detectChanges();
    await fixture.whenStable();

    const backdrop = document.querySelector('.app-dialog-backdrop');
    backdrop?.dispatchEvent(new MouseEvent('pointerdown', { bubbles: true }));
    fixture.detectChanges();
    expect(document.querySelector('[role="alertdialog"]')).toBeNull();
  });

  it('does not leave a dialog on document.body when the host is destroyed while open', async () => {
    const fixture = TestBed.createComponent(HostComponent);
    fixture.detectChanges();
    const compiled = fixture.nativeElement as HTMLElement;
    compiled.querySelector('button')?.click();
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();

    expect(document.body.querySelector('.app-dialog-backdrop')).toBeTruthy();
    expect(document.body.querySelector('[role="alertdialog"]')?.textContent).toContain('Delete this campaign?');

    fixture.destroy();

    expect(document.body.querySelector('.app-dialog-backdrop')).toBeNull();
    expect(document.body.querySelector('[role="alertdialog"]')).toBeNull();
    expect(document.body.querySelector('app-dialog')).toBeNull();
    expect(TestBed.inject(AppDialogService).hasOpen()).toBe(false);
  });
});

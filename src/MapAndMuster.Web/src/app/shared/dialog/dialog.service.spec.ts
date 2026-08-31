import { TestBed } from '@angular/core/testing';
import { provideZonelessChangeDetection } from '@angular/core';

import { AppDialogService } from './dialog.service';

describe('AppDialogService', () => {
  it('tracks nested open dialogs', () => {
    TestBed.configureTestingModule({
      providers: [provideZonelessChangeDetection()],
    });
    const dialogs = TestBed.inject(AppDialogService);
    expect(dialogs.hasOpen()).toBe(false);
    dialogs.register();
    expect(dialogs.hasOpen()).toBe(true);
    dialogs.register();
    dialogs.unregister();
    expect(dialogs.hasOpen()).toBe(true);
    dialogs.unregister();
    expect(dialogs.hasOpen()).toBe(false);
  });
});

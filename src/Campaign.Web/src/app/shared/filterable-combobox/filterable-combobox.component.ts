import { Component, computed, forwardRef, input, signal } from '@angular/core';
import { type ControlValueAccessor, NG_VALUE_ACCESSOR } from '@angular/forms';

@Component({
  selector: 'app-filterable-combobox',
  templateUrl: './filterable-combobox.component.html',
  styleUrl: './filterable-combobox.component.css',
  providers: [
    {
      provide: NG_VALUE_ACCESSOR,
      useExisting: forwardRef(() => FilterableComboboxComponent),
      multi: true,
    },
  ],
})
export class FilterableComboboxComponent implements ControlValueAccessor {
  readonly inputId = input.required<string>();
  readonly options = input<readonly string[]>([]);
  readonly placeholder = input('');

  protected readonly query = signal('');
  protected readonly open = signal(false);
  protected readonly disabled = signal(false);
  protected readonly highlight = signal(0);
  private committed = '';

  protected readonly listId = computed(() => `${this.inputId()}-list`);
  protected readonly filtered = computed(() => {
    const needle = this.query().trim().toLowerCase();
    const options = this.options();
    if (needle.length === 0) {
      return options;
    }

    return options.filter((option) => option.toLowerCase().includes(needle));
  });

  private onChange: (value: string) => void = () => undefined;
  private onTouched: () => void = () => undefined;

  writeValue(value: string | null): void {
    const next = value ?? '';
    this.committed = next;
    this.query.set(next);
  }

  registerOnChange(fn: (value: string) => void): void {
    this.onChange = fn;
  }

  registerOnTouched(fn: () => void): void {
    this.onTouched = fn;
  }

  setDisabledState(isDisabled: boolean): void {
    this.disabled.set(isDisabled);
  }

  protected onInput(event: Event): void {
    const value = (event.target as HTMLInputElement).value;
    this.query.set(value);
    this.open.set(true);
    this.highlight.set(0);
    this.emit(value);
  }

  protected onFocus(): void {
    this.open.set(true);
  }

  protected onFocusOut(event: FocusEvent): void {
    const next = event.relatedTarget as Node | null;
    if (next && (event.currentTarget as HTMLElement).contains(next)) {
      return;
    }

    this.snapToOption();
    this.open.set(false);
    this.onTouched();
  }

  protected onKeyDown(event: KeyboardEvent): void {
    if (event.key === 'ArrowDown') {
      event.preventDefault();
      this.open.set(true);
      this.moveHighlight(1);
      return;
    }

    if (event.key === 'ArrowUp') {
      event.preventDefault();
      this.open.set(true);
      this.moveHighlight(-1);
      return;
    }

    if (event.key === 'Enter' && this.open()) {
      event.preventDefault();
      const option = this.filtered()[this.highlight()];
      if (option) {
        this.select(option);
      }

      return;
    }

    if (event.key === 'Escape') {
      this.query.set(this.committed);
      this.open.set(false);
    }
  }

  protected select(option: string): void {
    this.committed = option;
    this.query.set(option);
    this.open.set(false);
    this.emit(option);
  }

  protected optionId(index: number): string {
    return `${this.listId()}-option-${index}`;
  }

  private snapToOption(): void {
    const query = this.query().trim();
    const match = this.options().find((option) => option.toLowerCase() === query.toLowerCase());
    const next = match ?? query;
    this.committed = next;
    this.query.set(next);
    this.emit(next);
  }

  private moveHighlight(delta: number): void {
    const count = this.filtered().length;
    if (count === 0) {
      return;
    }

    this.highlight.set((this.highlight() + delta + count) % count);
  }

  private emit(value: string): void {
    this.onChange(value);
  }
}

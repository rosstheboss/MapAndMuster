import { Pipe, type PipeTransform } from '@angular/core';

@Pipe({
  name: 'instantDate',
})
export class InstantDatePipe implements PipeTransform {
  transform(value: string | Date | null | undefined, timeZone?: string | null): string {
    if (value === null || value === undefined || value === '') {
      return '';
    }

    const date = value instanceof Date ? value : new Date(value);
    if (Number.isNaN(date.getTime())) {
      return '';
    }

    const zone = timeZone?.trim() ? timeZone.trim() : 'UTC';
    try {
      return new Intl.DateTimeFormat('en-GB', {
        day: 'numeric',
        month: 'short',
        year: 'numeric',
        hour: '2-digit',
        minute: '2-digit',
        hour12: false,
        timeZone: zone,
        timeZoneName: 'short',
      }).format(date);
    } catch {
      return new Intl.DateTimeFormat('en-GB', {
        day: 'numeric',
        month: 'short',
        year: 'numeric',
        hour: '2-digit',
        minute: '2-digit',
        hour12: false,
        timeZone: 'UTC',
      }).format(date);
    }
  }
}

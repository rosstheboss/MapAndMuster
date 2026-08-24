import { inject, Pipe, type PipeTransform } from '@angular/core';

import { AuthService } from '../../core/auth/auth.service';
import { formatInstant } from '../../core/time/date-time-display';

@Pipe({
  name: 'instantDate',
  pure: false,
})
export class InstantDatePipe implements PipeTransform {
  private readonly auth = inject(AuthService);

  transform(value: string | Date | null | undefined, timeZone?: string | null, format?: string | null): string {
    const chosen = format ?? this.auth.currentUser()?.dateTimeDisplayFormat;
    return formatInstant(value, timeZone, chosen);
  }
}

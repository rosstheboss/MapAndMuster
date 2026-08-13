import { Component, inject } from '@angular/core';
import { RouterLink } from '@angular/router';

import { AuthService } from '../../core/auth/auth.service';
import { InstantDatePipe } from '../../shared/time/instant-date.pipe';

@Component({
  selector: 'app-home-page',
  imports: [RouterLink, InstantDatePipe],
  templateUrl: './home.page.html',
  styleUrl: './home.page.css',
})
export class HomePage {
  protected readonly auth = inject(AuthService);
}

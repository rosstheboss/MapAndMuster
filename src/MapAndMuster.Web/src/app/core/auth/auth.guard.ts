import { inject } from '@angular/core';
import { Router, type CanActivateFn } from '@angular/router';

import { AuthService } from './auth.service';

export const authGuard: CanActivateFn = async () => {
  const auth = inject(AuthService);
  const router = inject(Router);
  const user = auth.currentUser() ?? (await auth.loadSession());
  return user ? true : router.parseUrl('/login');
};

export const guestGuard: CanActivateFn = async () => {
  const auth = inject(AuthService);
  const router = inject(Router);
  const user = auth.currentUser() ?? (await auth.loadSession());
  return !user || user.isGuestAccount ? true : router.parseUrl('/');
};

export const adminGuard: CanActivateFn = async () => {
  const auth = inject(AuthService);
  const router = inject(Router);
  const user = auth.currentUser() ?? (await auth.loadSession());
  return user?.isAdministrator ? true : router.parseUrl('/');
};

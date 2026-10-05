import { inject } from '@angular/core';
import { Router, type CanActivateFn } from '@angular/router';

import { AuthService } from './auth.service';
import { returnUrlOrHome, safeReturnUrl } from './return-url';

export const authGuard: CanActivateFn = async (_route, state) => {
  const auth = inject(AuthService);
  const router = inject(Router);
  const user = auth.currentUser() ?? (await auth.loadSession());
  if (user) {
    return true;
  }

  const returnUrl = safeReturnUrl(state.url);
  if (!returnUrl) {
    return router.parseUrl('/login');
  }

  return router.createUrlTree(['/login'], { queryParams: { returnUrl } });
};

export const guestGuard: CanActivateFn = async (route) => {
  const auth = inject(AuthService);
  const router = inject(Router);
  const user = auth.currentUser() ?? (await auth.loadSession());
  if (!user || user.isGuestAccount) {
    return true;
  }

  return router.parseUrl(returnUrlOrHome(route.queryParamMap.get('returnUrl')));
};

export const adminGuard: CanActivateFn = async () => {
  const auth = inject(AuthService);
  const router = inject(Router);
  const user = auth.currentUser() ?? (await auth.loadSession());
  return user?.isAdministrator ? true : router.parseUrl('/');
};

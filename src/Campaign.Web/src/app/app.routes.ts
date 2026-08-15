import type { Routes } from '@angular/router';

import { authGuard, guestGuard } from './core/auth/auth.guard';

export const routes: Routes = [
  {
    path: 'login',
    canActivate: [guestGuard],
    loadComponent: () => import('./features/login/login.page').then((module) => module.LoginPage),
  },
  {
    path: 'register',
    canActivate: [guestGuard],
    loadComponent: () => import('./features/register/register.page').then((module) => module.RegisterPage),
  },
  {
    path: 'confirm-email',
    loadComponent: () =>
      import('./features/confirm-email/confirm-email.page').then((module) => module.ConfirmEmailPage),
  },
  {
    path: 'forgot-password',
    canActivate: [guestGuard],
    loadComponent: () =>
      import('./features/forgot-password/forgot-password.page').then((module) => module.ForgotPasswordPage),
  },
  {
    path: 'reset-password',
    loadComponent: () =>
      import('./features/reset-password/reset-password.page').then((module) => module.ResetPasswordPage),
  },
  {
    path: 'complete-external',
    loadComponent: () =>
      import('./features/complete-external/complete-external.page').then((module) => module.CompleteExternalPage),
  },
  {
    path: '',
    canActivate: [authGuard],
    loadComponent: () => import('./features/home/home.page').then((module) => module.HomePage),
  },
  {
    path: 'profile',
    canActivate: [authGuard],
    loadComponent: () => import('./features/profile/profile.page').then((module) => module.ProfilePage),
  },
  {
    path: 'campaigns',
    canActivate: [authGuard],
    loadComponent: () => import('./features/campaigns/campaigns.page').then((module) => module.CampaignsPage),
  },
  {
    path: 'campaigns/all',
    canActivate: [authGuard],
    loadComponent: () =>
      import('./features/all-campaigns/all-campaigns.page').then((module) => module.AllCampaignsPage),
  },
  {
    path: 'campaigns/new',
    canActivate: [authGuard],
    loadComponent: () =>
      import('./features/campaign-setup/campaign-setup.page').then((module) => module.CampaignSetupPage),
  },
  {
    path: 'campaigns/:id',
    canActivate: [authGuard],
    loadComponent: () =>
      import('./features/campaign-detail/campaign-detail.page').then((module) => module.CampaignDetailPage),
  },
  {
    path: 'campaigns/:id/play',
    canActivate: [authGuard],
    loadComponent: () =>
      import('./features/campaign-play/campaign-play.page').then((module) => module.CampaignPlayPage),
  },
  {
    path: 'campaigns/:id/edit',
    canActivate: [authGuard],
    loadComponent: () =>
      import('./features/campaign-setup/campaign-setup.page').then((module) => module.CampaignSetupPage),
  },
  {
    path: 'campaigns/:id/map',
    canActivate: [authGuard],
    loadComponent: () => import('./features/map-editor/map-editor.page').then((module) => module.MapEditorPage),
  },
  {
    path: 'users/:username',
    loadComponent: () =>
      import('./features/public-profile/public-profile.page').then((module) => module.PublicProfilePage),
  },
  {
    path: '**',
    redirectTo: '',
  },
];

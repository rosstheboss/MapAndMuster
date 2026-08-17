import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { inject, Injectable, signal } from '@angular/core';
import { firstValueFrom } from 'rxjs';

import type {
  ErrorResponse,
  ExternalProvider,
  OwnProfile,
  PendingExternalProfile,
  ProfileFormValue,
  PublicProfile,
  RegisterPayload,
} from './auth.models';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);

  readonly currentUser = signal<OwnProfile | null>(null);
  readonly sessionChecked = signal(false);

  async loadSession(): Promise<OwnProfile | null> {
    try {
      const profile = await firstValueFrom(this.http.get<OwnProfile>('/api/auth/me', { withCredentials: true }));
      this.currentUser.set(profile);
      return profile;
    } catch {
      this.currentUser.set(null);
      return null;
    } finally {
      this.sessionChecked.set(true);
    }
  }

  async login(email: string, password: string): Promise<OwnProfile> {
    const profile = await firstValueFrom(
      this.http.post<OwnProfile>('/api/auth/login', { email, password }, { withCredentials: true }),
    );
    this.currentUser.set(profile);
    this.sessionChecked.set(true);
    return profile;
  }

  async register(payload: RegisterPayload): Promise<void> {
    if (payload.avatar) {
      const form = new FormData();
      form.set('email', payload.email);
      form.set('username', payload.username);
      form.set('password', payload.password);
      form.set('firstName', payload.firstName);
      form.set('middleInitial', payload.middleInitial);
      form.set('lastName', payload.lastName);
      form.set('suffix', payload.suffix);
      form.set('city', payload.city);
      form.set('region', payload.region);
      form.set('country', payload.country);
      form.set('timeZoneId', payload.timeZoneId);
      form.set('displayNameMode', payload.displayNameMode);
      form.set('avatar', payload.avatar);
      await firstValueFrom(this.http.post('/api/auth/register', form, { withCredentials: true }));
      return;
    }

    await firstValueFrom(
      this.http.post(
        '/api/auth/register',
        {
          email: payload.email,
          username: payload.username,
          password: payload.password,
          firstName: payload.firstName,
          middleInitial: payload.middleInitial || null,
          lastName: payload.lastName,
          suffix: payload.suffix || null,
          city: payload.city,
          region: payload.region || null,
          country: payload.country,
          timeZoneId: payload.timeZoneId || null,
          displayNameMode: payload.displayNameMode,
        },
        { withCredentials: true },
      ),
    );
  }

  async logout(): Promise<void> {
    await firstValueFrom(this.http.post('/api/auth/logout', {}, { withCredentials: true }));
    this.currentUser.set(null);
  }

  async confirmEmail(userId: string, token: string): Promise<void> {
    await firstValueFrom(this.http.post('/api/auth/confirm-email', { userId, token }));
  }

  async forgotPassword(email: string): Promise<void> {
    await firstValueFrom(this.http.post('/api/auth/forgot-password', { email }));
  }

  async resetPassword(userId: string, token: string, password: string): Promise<void> {
    await firstValueFrom(this.http.post('/api/auth/reset-password', { userId, token, password }));
  }

  async changePassword(currentPassword: string, newPassword: string): Promise<void> {
    await firstValueFrom(
      this.http.post('/api/auth/change-password', { currentPassword, newPassword }, { withCredentials: true }),
    );
  }

  async getOwnProfile(): Promise<OwnProfile> {
    const profile = await firstValueFrom(this.http.get<OwnProfile>('/api/profiles/me', { withCredentials: true }));
    this.currentUser.set(profile);
    return profile;
  }

  async updateProfile(value: ProfileFormValue, profileRevision: number): Promise<OwnProfile> {
    const profile = await firstValueFrom(
      this.http.put<OwnProfile>(
        '/api/profiles/me',
        {
          username: value.username,
          firstName: value.firstName,
          middleInitial: value.middleInitial || null,
          lastName: value.lastName,
          suffix: value.suffix || null,
          city: value.city,
          region: value.region || null,
          country: value.country,
          timeZoneId: value.timeZoneId || null,
          displayNameMode: value.displayNameMode,
          inAppNotificationsEnabled: value.inAppNotificationsEnabled ?? true,
          emailNotificationsEnabled: value.emailNotificationsEnabled ?? true,
          profileRevision,
        },
        { withCredentials: true },
      ),
    );
    this.currentUser.set(profile);
    return profile;
  }

  async uploadAvatar(file: File): Promise<OwnProfile> {
    const form = new FormData();
    form.set('avatar', file);
    const profile = await firstValueFrom(
      this.http.post<OwnProfile>('/api/profiles/me/avatar', form, { withCredentials: true }),
    );
    this.currentUser.set(profile);
    return profile;
  }

  async getPublicProfile(username: string): Promise<PublicProfile> {
    return firstValueFrom(this.http.get<PublicProfile>(`/api/profiles/${encodeURIComponent(username)}`));
  }

  avatarUrl(username: string): string {
    return `/api/profiles/${encodeURIComponent(username)}/avatar`;
  }

  async getExternalProviders(): Promise<ExternalProvider[]> {
    return firstValueFrom(this.http.get<ExternalProvider[]>('/api/auth/external-providers'));
  }

  startExternalLogin(provider: string): void {
    window.location.assign(`/api/auth/external/${encodeURIComponent(provider)}/challenge`);
  }

  async getPendingExternalProfile(): Promise<PendingExternalProfile> {
    return firstValueFrom(
      this.http.get<PendingExternalProfile>('/api/auth/external/pending', { withCredentials: true }),
    );
  }

  async completeExternalRegistration(value: ProfileFormValue): Promise<OwnProfile> {
    const profile = await firstValueFrom(
      this.http.post<OwnProfile>(
        '/api/auth/external/complete',
        {
          username: value.username,
          firstName: value.firstName,
          middleInitial: value.middleInitial || null,
          lastName: value.lastName,
          suffix: value.suffix || null,
          city: value.city,
          region: value.region || null,
          country: value.country,
          timeZoneId: value.timeZoneId || null,
          displayNameMode: value.displayNameMode,
        },
        { withCredentials: true },
      ),
    );
    this.currentUser.set(profile);
    this.sessionChecked.set(true);
    return profile;
  }
}

export function readApiError(error: unknown, fallback: string): string {
  const messages = readApiErrorMessages(error, fallback);
  return messages.join('\n');
}

export function readApiErrorMessages(error: unknown, fallback: string): string[] {
  if (error instanceof HttpErrorResponse && error.error && typeof error.error === 'object') {
    const body = error.error as ErrorResponse;
    if (Array.isArray(body.errors) && body.errors.length > 0) {
      const messages = body.errors.map((item) => item.message).filter((message) => message.length > 0);
      if (messages.length > 0) {
        return messages;
      }
    }

    if (typeof body.message === 'string' && body.message.length > 0) {
      return body.message
        .split(/\n+/)
        .map((line) => line.trim())
        .filter((line) => line.length > 0);
    }
  }

  return [fallback];
}

export function readApiFieldErrors(error: unknown): string[] {
  if (error instanceof HttpErrorResponse && error.error && typeof error.error === 'object') {
    const body = error.error as ErrorResponse;
    if (Array.isArray(body.errors)) {
      return body.errors.map((item) => item.field).filter((field) => field.length > 0);
    }
  }

  return [];
}

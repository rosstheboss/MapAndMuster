export interface ErrorResponse {
  code: string;
  message: string;
}

export interface OwnProfile {
  id: string;
  email: string;
  username: string;
  firstName: string;
  middleInitial: string | null;
  lastName: string;
  city: string;
  region: string | null;
  country: string;
  displayNameMode: 'Username' | 'FullName';
  timeZoneId: string | null;
  hasAvatar: boolean;
  createdUtc: string;
  updatedUtc: string;
  profileRevision: number;
  emailConfirmed: boolean;
}

export interface PublicProfile {
  username: string;
  displayName: string;
  showsFullName: boolean;
  city: string;
  region: string | null;
  country: string;
  hasAvatar: boolean;
}

export interface ExternalProvider {
  name: string;
  displayName: string;
}

export interface PendingExternalProfile {
  provider: string;
  email: string | null;
  firstName: string | null;
  lastName: string | null;
  avatarUrl: string | null;
}

export interface RegisterPayload {
  email: string;
  username: string;
  password: string;
  firstName: string;
  middleInitial: string;
  lastName: string;
  city: string;
  region: string;
  country: string;
  displayNameMode: 'Username' | 'FullName';
  timeZoneId: string;
  avatar: File | null;
}

export interface ProfileFormValue {
  username: string;
  firstName: string;
  middleInitial: string;
  lastName: string;
  city: string;
  region: string;
  country: string;
  displayNameMode: 'Username' | 'FullName';
  timeZoneId: string;
}

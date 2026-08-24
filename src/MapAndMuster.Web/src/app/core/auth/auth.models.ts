export interface FieldError {
  field: string;
  code: string;
  message: string;
}

export interface ErrorResponse {
  code: string;
  message: string;
  errors?: FieldError[] | null;
}

export interface OwnProfile {
  id: string;
  email: string;
  username: string;
  firstName: string;
  middleInitial: string | null;
  lastName: string;
  suffix: string | null;
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
  isAdministrator: boolean;
  inAppNotificationsEnabled: boolean;
  emailNotificationsEnabled: boolean;
  preferredChatLanguage: string;
  dateTimeDisplayFormat?: string;
  isTestAccount?: boolean;
  testAccountNumber?: number | null;
  isImpersonating?: boolean;
}

export interface PublicProfile {
  username: string;
  displayName: string;
  showsFullName: boolean;
  city: string;
  region: string | null;
  country: string;
  hasAvatar: boolean;
  campaigns?: PublicProfileCampaign[];
}

export interface PublicProfileCampaign {
  id: string;
  name: string;
  status: string;
  isPrivate: boolean;
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
  suffix: string;
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
  suffix: string;
  city: string;
  region: string;
  country: string;
  displayNameMode: 'Username' | 'FullName';
  timeZoneId: string;
  inAppNotificationsEnabled?: boolean;
  emailNotificationsEnabled?: boolean;
  preferredChatLanguage?: string;
  dateTimeDisplayFormat?: string;
}

export interface ChangePasswordPayload {
  currentPassword: string;
  newPassword: string;
}

export interface TestAccount {
  id: string;
  number: number;
  username: string;
  displayName: string;
}

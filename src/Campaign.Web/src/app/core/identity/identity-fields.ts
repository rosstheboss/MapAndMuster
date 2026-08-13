export const NAME_SUFFIXES = ['Jr.', 'Sr.', 'I', 'II', 'III', 'IV', 'V', 'VI', 'VII', 'VIII', 'IX', 'X'] as const;

export const REGISTER_FIELD_LABELS: Readonly<Record<string, string>> = {
  email: 'Email',
  username: 'Username',
  password: 'Password',
  confirmPassword: 'Confirm password',
  firstName: 'First name',
  middleInitial: 'Middle initial',
  lastName: 'Last name',
  suffix: 'Suffix',
  country: 'Country',
  region: 'State or province',
  city: 'City',
  timeZoneId: 'Time zone',
};

export const PROFILE_FIELD_LABELS: Readonly<Record<string, string>> = {
  username: 'Username',
  firstName: 'First name',
  middleInitial: 'Middle initial',
  lastName: 'Last name',
  suffix: 'Suffix',
  country: 'Country',
  region: 'State or province',
  city: 'City',
  timeZoneId: 'Time zone',
  currentPassword: 'Current password',
  newPassword: 'New password',
  confirmPassword: 'Confirm password',
};

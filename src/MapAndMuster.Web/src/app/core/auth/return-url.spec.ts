import { clearReturnUrl, peekReturnUrl, rememberReturnUrl, returnUrlOrHome, safeReturnUrl } from './return-url';

describe('safeReturnUrl', () => {
  it('accepts an in-app path and query', () => {
    expect(safeReturnUrl('/campaigns/abc?join=1')).toBe('/campaigns/abc?join=1');
    expect(returnUrlOrHome('/profile')).toBe('/profile');
  });

  it('sends missing, external, and account-flow addresses home', () => {
    expect(safeReturnUrl(null)).toBeNull();
    expect(safeReturnUrl('')).toBeNull();
    expect(safeReturnUrl('/')).toBeNull();
    expect(safeReturnUrl('//evil.example')).toBeNull();
    expect(safeReturnUrl('https://evil.example/campaigns')).toBeNull();
    expect(safeReturnUrl('/login')).toBeNull();
    expect(safeReturnUrl('/register?next=/campaigns')).toBeNull();
    expect(safeReturnUrl('/\\evil')).toBeNull();
    expect(returnUrlOrHome('https://evil.example')).toBe('/');
  });
});

describe('rememberReturnUrl', () => {
  beforeEach(() => {
    sessionStorage.clear();
  });

  it('keeps a recent address and drops an expired one', () => {
    rememberReturnUrl('/campaigns/abc');
    expect(peekReturnUrl()).toBe('/campaigns/abc');

    sessionStorage.setItem(
      'mapandmuster.auth-return',
      JSON.stringify({ url: '/campaigns/abc', at: Date.now() - 31 * 60 * 1000 }),
    );
    expect(peekReturnUrl()).toBeNull();
    expect(sessionStorage.getItem('mapandmuster.auth-return')).toBeNull();
  });

  it('clears an unsafe address', () => {
    rememberReturnUrl('/campaigns/abc');
    rememberReturnUrl('https://evil.example');
    expect(peekReturnUrl()).toBeNull();
    clearReturnUrl();
  });
});

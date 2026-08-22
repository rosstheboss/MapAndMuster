import { internalReturnLink, safeInternalPath } from './internal-path';

describe('internal return paths', () => {
  it('accepts in-app paths and rejects open redirects', () => {
    expect(safeInternalPath('/campaigns/abc')).toBe('/campaigns/abc');
    expect(safeInternalPath('  /profile  ')).toBe('/profile');
    expect(safeInternalPath('//evil.example')).toBeNull();
    expect(safeInternalPath('/\\evil')).toBeNull();
    expect(safeInternalPath('https://evil.example/phish')).toBeNull();
    expect(safeInternalPath('campaigns/abc')).toBeNull();
  });

  it('splits query parameters for routerLink', () => {
    expect(internalReturnLink('/campaigns/abc?tab=log')).toEqual({
      path: '/campaigns/abc',
      queryParams: { tab: 'log' },
    });
    expect(internalReturnLink('//evil.example')).toBeNull();
  });
});

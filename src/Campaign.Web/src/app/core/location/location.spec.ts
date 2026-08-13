import { listCountries, regionsForCountry } from './location';

describe('location catalog', () => {
  it('lists countries and filters regions by selected country', () => {
    expect(listCountries()).toContain('Canada');
    expect(listCountries()).toContain('United States');
    expect(regionsForCountry('Canada')).toContain('Nova Scotia');
    expect(regionsForCountry('United States')).toContain('Texas');
    expect(regionsForCountry('')).toEqual([]);
  });
});

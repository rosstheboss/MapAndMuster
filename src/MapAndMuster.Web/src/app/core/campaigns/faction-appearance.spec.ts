import { resolveFactionAppearance } from './faction-appearance';

describe('faction appearance', () => {
  const faction = {
    id: 'daemons',
    name: 'Daemons of Chaos',
    color: '#AD1457',
    subfactions: ['Khorne'],
    allyGroupName: null,
    requiresSubfaction: true,
    hasFlagImage: true,
    tintFlagImage: true,
    subfactionAppearances: [
      {
        name: 'Khorne',
        color: '#B91C1C',
        flagSource: 'color' as const,
        hasFlagImage: false,
        tintFlagImage: false,
      },
    ],
  };

  it('uses a required subfaction color flag instead of the parent logo', () => {
    expect(resolveFactionAppearance(faction, 'Khorne')).toEqual({
      color: '#B91C1C',
      hasFlagImage: false,
      tint: false,
    });
  });

  it('inherits the parent logo and color when a subfaction has no appearance', () => {
    expect(resolveFactionAppearance({ ...faction, subfactionAppearances: [] }, 'Khorne')).toEqual({
      color: '#AD1457',
      hasFlagImage: true,
      tint: true,
    });
  });

  it('uses a subfaction uploaded logo when the flag source is image', () => {
    expect(
      resolveFactionAppearance(
        {
          ...faction,
          subfactionAppearances: [
            {
              name: 'Khorne',
              color: '#B91C1C',
              flagSource: 'image',
              hasFlagImage: true,
              tintFlagImage: true,
            },
          ],
        },
        'Khorne',
      ),
    ).toEqual({
      color: '#B91C1C',
      hasFlagImage: true,
      tint: true,
    });
  });
});

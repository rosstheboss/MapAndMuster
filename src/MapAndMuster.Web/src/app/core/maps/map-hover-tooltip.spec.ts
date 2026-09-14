import { placeMapHoverTooltip } from './map-hover-tooltip';

describe('placeMapHoverTooltip', () => {
  const tooltipWidth = 100;
  const tooltipHeight = 50;
  const viewportWidth = 400;
  const viewportHeight = 300;

  it('places the tooltip at the lower right of the cursor when it fits', () => {
    expect(placeMapHoverTooltip(20, 30, tooltipWidth, tooltipHeight, viewportWidth, viewportHeight)).toEqual({
      x: 32,
      y: 46,
    });
  });

  it('flips to the lower left when the right edge of the viewport would clip it', () => {
    expect(placeMapHoverTooltip(380, 20, tooltipWidth, tooltipHeight, viewportWidth, viewportHeight)).toEqual({
      x: 268,
      y: 36,
    });
  });

  it('flips to the upper right when the bottom edge of the viewport would clip it', () => {
    expect(placeMapHoverTooltip(20, 280, tooltipWidth, tooltipHeight, viewportWidth, viewportHeight)).toEqual({
      x: 32,
      y: 214,
    });
  });

  it('flips to the upper left when both the right and bottom edges would clip it', () => {
    expect(placeMapHoverTooltip(380, 280, tooltipWidth, tooltipHeight, viewportWidth, viewportHeight)).toEqual({
      x: 268,
      y: 214,
    });
  });

  it('clamps a tooltip that is larger than the remaining viewport', () => {
    expect(placeMapHoverTooltip(10, 10, 500, 400, 200, 120)).toEqual({
      x: 8,
      y: 8,
    });
  });
});

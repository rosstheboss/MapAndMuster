import { isAdditiveModifier } from './pointer';

describe('isAdditiveModifier', () => {
  it('treats Control, Command, and Shift as additive multi-select modifiers', () => {
    expect(isAdditiveModifier({ ctrlKey: true, metaKey: false, shiftKey: false })).toBe(true);
    expect(isAdditiveModifier({ ctrlKey: false, metaKey: true, shiftKey: false })).toBe(true);
    expect(isAdditiveModifier({ ctrlKey: false, metaKey: false, shiftKey: true })).toBe(true);
    expect(isAdditiveModifier({ ctrlKey: false, metaKey: false, shiftKey: false })).toBe(false);
  });
});

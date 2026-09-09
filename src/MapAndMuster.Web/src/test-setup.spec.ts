describe('test-setup localStorage', () => {
  it('exposes Storage methods under Node jsdom', () => {
    expect(typeof localStorage.getItem).toBe('function');
    expect(typeof localStorage.setItem).toBe('function');
    expect(typeof localStorage.removeItem).toBe('function');
  });
});

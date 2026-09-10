// Node 26 enables an experimental Web Storage global that is undefined unless
// --localstorage-file is set. That stub shadows jsdom's localStorage and breaks tests.
function isUsableStorage(value: unknown): value is Storage {
  if (typeof value !== 'object' || value === null) {
    return false;
  }

  const storage = value as Storage;
  return typeof storage.getItem === 'function' && typeof storage.removeItem === 'function';
}

function createMemoryStorage(): Storage {
  const store = new Map<string, string>();
  return {
    get length() {
      return store.size;
    },
    clear() {
      store.clear();
    },
    getItem(key: string) {
      return store.has(key) ? store.get(key)! : null;
    },
    key(index: number) {
      return [...store.keys()][index] ?? null;
    },
    removeItem(key: string) {
      store.delete(key);
    },
    setItem(key: string, value: string) {
      store.set(String(key), String(value));
    },
  };
}

if (!isUsableStorage(globalThis.localStorage)) {
  const memory = createMemoryStorage();
  Object.defineProperty(globalThis, 'localStorage', {
    configurable: true,
    enumerable: true,
    writable: true,
    value: memory,
  });
  if (typeof window !== 'undefined') {
    Object.defineProperty(window, 'localStorage', {
      configurable: true,
      enumerable: true,
      writable: true,
      value: memory,
    });
  }
}

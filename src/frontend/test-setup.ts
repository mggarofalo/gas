import "@testing-library/jest-dom/vitest";

/**
 * Node 24+ ships `localStorage`/`sessionStorage` globals that are undefined
 * unless the process was started with --localstorage-file, and they shadow the
 * ones jsdom installs. CI runs Node 22 and is unaffected; on newer Node the
 * app code would otherwise blow up on `localStorage.getItem`. Install a plain
 * in-memory Storage when the environment left us without one.
 */
function createMemoryStorage(): Storage {
  const store = new Map<string, string>();
  return {
    get length() {
      return store.size;
    },
    key: (index: number) => [...store.keys()][index] ?? null,
    getItem: (key: string) => store.get(key) ?? null,
    setItem: (key: string, value: string) => {
      store.set(key, String(value));
    },
    removeItem: (key: string) => {
      store.delete(key);
    },
    clear: () => {
      store.clear();
    },
  } as Storage;
}

for (const key of ["localStorage", "sessionStorage"] as const) {
  if (!globalThis[key]) {
    Object.defineProperty(globalThis, key, {
      value: createMemoryStorage(),
      configurable: true,
      writable: true,
    });
  }
}

/**
 * Vitest Test Setup for Vibe.UI Docs Tests
 */

import { vi } from 'vitest';

// Setup DOM environment
beforeEach(() => {
  document.body.innerHTML = '';
  document.head.innerHTML = '';
  vi.restoreAllMocks();
});

afterEach(() => {
  vi.clearAllMocks();
});

// Mock console methods
global.console = {
  ...console,
  log: vi.fn(console.log),
  error: vi.fn(console.error),
  warn: vi.fn(console.warn),
  info: vi.fn(console.info),
  debug: vi.fn(console.debug)
};

vi.useFakeTimers();

// Helper: Wait for async operations
export const waitForAsync = async (ms = 0): Promise<void> => {
  return new Promise((resolve) => {
    setTimeout(resolve, ms);
  });
};

// Helper: Setup mock clipboard
export const setupMockClipboard = (): void => {
  Object.assign(navigator, {
    clipboard: {
      writeText: vi.fn((_text: string) => Promise.resolve()),
      readText: vi.fn(() => Promise.resolve(''))
    }
  });
};

// Setup mock clipboard by default
setupMockClipboard();

export { vi };

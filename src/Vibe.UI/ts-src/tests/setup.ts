/**
 * Vitest Test Setup for Vibe.UI Chart Tests
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

// Helper: Create a mock canvas element
export const createMockCanvas = (id: string): HTMLCanvasElement => {
  const canvas = document.createElement('canvas');
  canvas.id = id;
  canvas.width = 400;
  canvas.height = 300;

  const mockContext = {
    fillStyle: '',
    strokeStyle: '',
    lineWidth: 1,
    fillRect: vi.fn(),
    strokeRect: vi.fn(),
    clearRect: vi.fn(),
    beginPath: vi.fn(),
    closePath: vi.fn(),
    moveTo: vi.fn(),
    lineTo: vi.fn(),
    arc: vi.fn(),
    fill: vi.fn(),
    stroke: vi.fn(),
    save: vi.fn(),
    restore: vi.fn(),
    scale: vi.fn(),
    rotate: vi.fn(),
    translate: vi.fn(),
    transform: vi.fn(),
    setTransform: vi.fn(),
    resetTransform: vi.fn(),
    drawImage: vi.fn(),
    createImageData: vi.fn(),
    getImageData: vi.fn(),
    putImageData: vi.fn(),
    measureText: vi.fn(() => ({ width: 100 })),
    canvas: canvas
  };

  canvas.getContext = vi.fn(() => mockContext as unknown as CanvasRenderingContext2D) as unknown as typeof canvas.getContext;

  return canvas;
};

export { vi };

/**
 * DOM-related mocks for testing
 */

/**
 * Mock Clipboard API
 */
export class MockClipboard {
  private clipboardText = '';
  public writeTextCalls: string[] = [];
  public readTextCalls: number = 0;
  public shouldFailWrite = false;
  public shouldFailRead = false;

  async writeText(text: string): Promise<void> {
    this.writeTextCalls.push(text);

    if (this.shouldFailWrite) {
      throw new Error('Clipboard write failed');
    }

    this.clipboardText = text;
    return Promise.resolve();
  }

  async readText(): Promise<string> {
    this.readTextCalls++;

    if (this.shouldFailRead) {
      throw new Error('Clipboard read failed');
    }

    return Promise.resolve(this.clipboardText);
  }

  // Test helpers
  getClipboardText(): string {
    return this.clipboardText;
  }

  reset(): void {
    this.clipboardText = '';
    this.writeTextCalls = [];
    this.readTextCalls = 0;
    this.shouldFailWrite = false;
    this.shouldFailRead = false;
  }
}

// Global mock clipboard instance
let mockClipboard: MockClipboard | null = null;

/**
 * Setup mock clipboard
 */
export function setupMockClipboard(): MockClipboard {
  mockClipboard = new MockClipboard();
  Object.defineProperty(navigator, 'clipboard', {
    value: mockClipboard,
    writable: true,
    configurable: true
  });
  return mockClipboard;
}

/**
 * Get current mock clipboard instance
 */
export function getMockClipboard(): MockClipboard | null {
  return mockClipboard;
}

/**
 * Teardown mock clipboard
 */
export function teardownMockClipboard(): void {
  mockClipboard = null;
}

/**
 * Create a mock button element for code copy tests
 */
export function createMockCopyButton(codeText: string): HTMLElement {
  const button = document.createElement('button');
  button.className = 'copy-button';

  const iconSpan = document.createElement('span');
  iconSpan.textContent = '📋';
  button.appendChild(iconSpan);

  const textSpan = document.createElement('span');
  textSpan.textContent = 'Copy';
  button.appendChild(textSpan);

  // Create code block structure
  const codeBlock = document.createElement('div');
  codeBlock.className = 'code-block';

  const pre = document.createElement('pre');
  const code = document.createElement('code');
  code.textContent = codeText;
  pre.appendChild(code);
  codeBlock.appendChild(pre);
  codeBlock.appendChild(button);

  document.body.appendChild(codeBlock);

  return button;
}

/**
 * Create mock sidebar and menu button for mobile menu tests
 */
export function createMockMobileMenu(): {
  sidebar: HTMLElement;
  menuButton: HTMLElement;
} {
  const sidebar = document.createElement('div');
  sidebar.className = 'docs-sidebar';

  const menuButton = document.createElement('button');
  menuButton.className = 'mobile-menu-btn';

  document.body.appendChild(sidebar);
  document.body.appendChild(menuButton);

  return { sidebar, menuButton };
}

/**
 * Create mock tab navigation structure
 */
export function createMockTabs(): {
  tabs: HTMLElement[];
  panels: HTMLElement[];
} {
  const tabNames = ['preview', 'code'];
  const tabs: HTMLElement[] = [];
  const panels: HTMLElement[] = [];

  tabNames.forEach((name) => {
    const tab = document.createElement('button');
    tab.className = 'preview-tab';
    tab.setAttribute('data-tab', name);
    document.body.appendChild(tab);
    tabs.push(tab);

    const panel = document.createElement('div');
    panel.className = `${name}-panel`;
    document.body.appendChild(panel);
    panels.push(panel);
  });

  return { tabs, panels };
}

/**
 * Create mock preview content for theme toggle
 */
export function createMockPreviewContent(): HTMLElement {
  const preview = document.createElement('div');
  preview.className = 'preview-content';
  document.body.appendChild(preview);
  return preview;
}

/**
 * Wait for toast to appear
 */
export async function waitForToast(timeout = 1000): Promise<HTMLElement | null> {
  const startTime = Date.now();

  while (Date.now() - startTime < timeout) {
    const toast = document.querySelector('.docs-toast') as HTMLElement;
    if (toast) {
      return toast;
    }
    await new Promise((resolve) => setTimeout(resolve, 10));
  }

  return null;
}

/**
 * Get all toasts currently in the document
 */
export function getToasts(): HTMLElement[] {
  return Array.from(document.querySelectorAll('.docs-toast'));
}

/**
 * Simulate a click event
 * Note: We use element.click() for jsdom compatibility instead of MouseEvent with view
 */
export function simulateClick(element: HTMLElement): void {
  element.click();
}

/**
 * Simulate a custom event
 */
export function simulateEvent(element: HTMLElement, eventType: string): void {
  const event = new Event(eventType, {
    bubbles: true,
    cancelable: true
  });
  element.dispatchEvent(event);
}

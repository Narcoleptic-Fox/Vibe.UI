/**
 * Vibe.UI DOM Utilities Module
 * CSP-safe DOM operations for Blazor components
 */

const initializedGridNavigationGuards = new WeakSet();
const initializedExpandedKeyGuards = new WeakSet();

/**
 * Gets the bounding client rect for an element by ID
 * @param {string} elementId - The element ID
 * @returns {DOMRect|null} The bounding rect or null if not found
 */
export function getBoundingRect(elementId) {
  const element = document.getElementById(elementId);
  if (!element) return null;
  return element.getBoundingClientRect();
}

/**
 * Gets the bounding client rect for an element's parent
 * @param {string} elementId - The element ID
 * @returns {DOMRect|null} The parent's bounding rect or null if not found
 */
export function getParentBoundingRect(elementId) {
  const element = document.getElementById(elementId);
  if (!element || !element.parentElement) return null;
  return element.parentElement.getBoundingClientRect();
}

/**
 * Focuses an element by ID
 * @param {string} elementId - The element ID
 */
export function focusElement(elementId) {
  return new Promise((resolve) => {
    requestAnimationFrame(() => {
      const element = document.getElementById(elementId);
      if (!element) {
        resolve(false);
        return;
      }

      // A grid cell that is no longer the roving tab stop was the target of an earlier move;
      // focusing it now would pull focus back from the day the keyboard has moved on to.
      if (isStaleGridCell(element)) {
        resolve(false);
        return;
      }

      element.focus({ preventScroll: true });
      resolve(document.activeElement === element);
    });
  });
}

function isStaleGridCell(element) {
  return (
    element.getAttribute("role") === "gridcell" &&
    element.getAttribute("tabindex") !== "0"
  );
}

/**
 * Prevents browser scrolling for arrow keys handled by an ARIA gridcell, and keeps focus on
 * the grid's roving tab stop while the component's own focus call is on its way.
 *
 * A key that moves to another month re-renders the grid, which removes the focused cell and
 * leaves focus on the body until the component's focus call arrives; on a slow device or
 * circuit, further keys in that window scroll the page and never reach the picker. A key
 * within the month leaves focus on the previous day for the same window. In both cases the
 * new tab stop (tabindex="0") is already in the DOM, so focus moves to it as soon as the
 * render lands.
 * @param {HTMLElement} gridElement - The grid container
 */
export function initializeGridNavigationGuard(gridElement) {
  if (!gridElement || initializedGridNavigationGuards.has(gridElement)) {
    return;
  }

  // The scope that holds the one tab stop: the popup, which can hold more than one grid.
  const scope = gridElement.closest('[role="dialog"]') ?? gridElement;
  let lastFocusedCell = null;

  gridElement.addEventListener("focusin", (event) => {
    const target = event.target;
    lastFocusedCell =
      target instanceof Element && target.getAttribute("role") === "gridcell"
        ? target
        : null;
  });

  new MutationObserver(() => {
    const active = document.activeElement;
    const focusWasRemoved =
      lastFocusedCell !== null &&
      !lastFocusedCell.isConnected &&
      (active === null || active === document.body);
    const focusIsOnStaleCell =
      active instanceof Element &&
      scope.contains(active) &&
      isStaleGridCell(active);

    if (!focusWasRemoved && !focusIsOnStaleCell) {
      return;
    }

    const tabStop = scope.querySelector('[role="gridcell"][tabindex="0"]');
    if (tabStop && tabStop !== active) {
      tabStop.focus({ preventScroll: true });
    }
  }).observe(gridElement, {
    subtree: true,
    childList: true,
    attributes: true,
    attributeFilter: ["tabindex"],
  });

  gridElement.addEventListener("keydown", (event) => {
    const target = event.target;
    const isGridCell =
      target instanceof Element && target.getAttribute("role") === "gridcell";
    const isNavigationKey =
      event.key === "ArrowLeft" ||
      event.key === "ArrowRight" ||
      event.key === "ArrowUp" ||
      event.key === "ArrowDown";

    if (isGridCell && isNavigationKey) {
      event.preventDefault();
    }
  });

  initializedGridNavigationGuards.add(gridElement);
}

/**
 * Prevents the browser default for Enter/ArrowUp/ArrowDown while a combobox-style
 * control is expanded, so those keys drive the popup instead of submitting a form
 * or moving the caret. CSP-safe replacement for an inline onkeydown attribute.
 * @param {HTMLElement} element - The input element whose aria-expanded is read
 */
export function initializeExpandedKeyGuard(element) {
  if (!element || initializedExpandedKeyGuards.has(element)) {
    return;
  }

  element.addEventListener("keydown", (event) => {
    const isNavigationKey =
      event.key === "Enter" ||
      event.key === "ArrowDown" ||
      event.key === "ArrowUp";

    if (isNavigationKey && element.getAttribute("aria-expanded") === "true") {
      event.preventDefault();
    }
  });

  initializedExpandedKeyGuards.add(element);
}

/**
 * Scrolls an element into view
 * @param {string} elementId - The element ID
 * @param {object} options - ScrollIntoView options
 */
export function scrollIntoView(
  elementId,
  options = { behavior: "smooth", block: "nearest" },
) {
  const element = document.getElementById(elementId);
  if (element) element.scrollIntoView(options);
}

/**
 * Gets the scroll position of an element
 * @param {string} elementId - The element ID
 * @returns {object|null} {scrollTop, scrollLeft} or null
 */
export function getScrollPosition(elementId) {
  const element = document.getElementById(elementId);
  if (!element) return null;
  return { scrollTop: element.scrollTop, scrollLeft: element.scrollLeft };
}

/**
 * Sets the scroll position of an element
 * @param {string} elementId - The element ID
 * @param {number} top - Scroll top position
 * @param {number} left - Scroll left position
 */
export function setScrollPosition(elementId, top, left) {
  const element = document.getElementById(elementId);
  if (element) {
    element.scrollTop = top;
    element.scrollLeft = left;
  }
}

// Export for global access (fallback)
window.VibeDom = {
  getBoundingRect,
  getParentBoundingRect,
  focusElement,
  initializeGridNavigationGuard,
  scrollIntoView,
  getScrollPosition,
  setScrollPosition,
};

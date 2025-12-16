(() => {
  const vibeDocs = (window.vibeDocs = window.vibeDocs || {});

  vibeDocs.registerHotkeys = (dotnetRef) => {
    if (vibeDocs._hotkeysRegistered) return;

    vibeDocs._hotkeysRegistered = true;
    vibeDocs._dotnetRef = dotnetRef;
    vibeDocs._handler = (e) => {
      const isK = e.key === "k" || e.key === "K" || e.code === "KeyK";
      if ((e.ctrlKey || e.metaKey) && isK) {
        e.preventDefault();
        vibeDocs._dotnetRef?.invokeMethodAsync("OpenCommandPalette");
      }
    };

    document.addEventListener("keydown", vibeDocs._handler);
    // Used by Playwright E2E tests as a "Blazor ready" signal.
    window._vibeKeyboardHandler = vibeDocs._handler;
  };

  vibeDocs.unregisterHotkeys = () => {
    if (!vibeDocs._hotkeysRegistered) return;
    document.removeEventListener("keydown", vibeDocs._handler);
    vibeDocs._handler = undefined;
    vibeDocs._dotnetRef = undefined;
    vibeDocs._hotkeysRegistered = false;
    window._vibeKeyboardHandler = undefined;
  };
})();

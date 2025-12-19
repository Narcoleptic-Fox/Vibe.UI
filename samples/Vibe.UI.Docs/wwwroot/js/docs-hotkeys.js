(() => {
  const vibeDocs = (window.vibeDocs = window.vibeDocs || {});

  vibeDocs.copyTextSync = (text) => {
    try {
      const textarea = document.createElement("textarea");
      textarea.value = text;
      textarea.setAttribute("readonly", "");
      textarea.style.position = "fixed";
      textarea.style.top = "-1000px";
      textarea.style.left = "-1000px";
      textarea.style.opacity = "0";

      document.body.appendChild(textarea);
      textarea.focus();
      textarea.select();

      const ok = document.execCommand("copy");
      document.body.removeChild(textarea);
      return ok === true;
    } catch {
      return false;
    }
  };

  vibeDocs.copyFromButton = (button) => {
    try {
      const b64 = button?.dataset?.copyTextB64;
      if (!b64) return false;

      const binary = atob(b64);
      const bytes = new Uint8Array(binary.length);
      for (let i = 0; i < binary.length; i++) {
        bytes[i] = binary.charCodeAt(i);
      }
      const text = new TextDecoder().decode(bytes);

      try {
        navigator.clipboard.writeText(text).catch(() => {
          vibeDocs.copyTextSync(text);
        });
        return true;
      } catch {
        return vibeDocs.copyTextSync(text);
      }
    } catch {
      return false;
    }
  };

  vibeDocs.copyText = async (text) => {
    try {
      await navigator.clipboard.writeText(text);
      return true;
    } catch {
      // Fall back for insecure contexts or denied permissions.
    }

    try {
      return vibeDocs.copyTextSync(text);
    } catch {
      return false;
    }
  };

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

  vibeDocs.registerCopyButtons = () => {
    if (vibeDocs._copyButtonsRegistered) return;
    vibeDocs._copyButtonsRegistered = true;

    vibeDocs._copyResetTimers = vibeDocs._copyResetTimers || new WeakMap();

    document.addEventListener(
      "click",
      (e) => {
      const button = e.target?.closest?.("button[data-copy-text-b64]");
      if (!button) return;

      vibeDocs.copyFromButton(button);

      const previousTimer = vibeDocs._copyResetTimers.get(button);
      if (previousTimer) {
        clearTimeout(previousTimer);
      }

      const originalTitle = button.getAttribute("title") || "Copy to clipboard";
      button.setAttribute("title", "Copied!");
      button.classList.add("copied");

      const timer = setTimeout(() => {
        button.setAttribute("title", originalTitle);
        button.classList.remove("copied");
        vibeDocs._copyResetTimers.delete(button);
      }, 2000);

      vibeDocs._copyResetTimers.set(button, timer);
      },
      true
    );
  };

  vibeDocs.registerCopyButtons();
})();

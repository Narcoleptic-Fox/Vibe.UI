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

        const originalTitle =
          button.getAttribute("title") || "Copy to clipboard";
        button.setAttribute("title", "Copied!");
        button.classList.add("copied");

        const timer = setTimeout(() => {
          button.setAttribute("title", originalTitle);
          button.classList.remove("copied");
          vibeDocs._copyResetTimers.delete(button);
        }, 2000);

        vibeDocs._copyResetTimers.set(button, timer);
      },
      true,
    );
  };

  vibeDocs.registerCopyButtons();

  // ---------------------------------------------------------------------------
  // Site shell: table of contents and navigation rail
  // ---------------------------------------------------------------------------

  // Headings count as "reached" once they pass the sticky header plus some air.
  const TOC_ACTIVATION_OFFSET = 96;

  const slugify = (text) =>
    text
      .toLowerCase()
      .replace(/[^a-z0-9]+/g, "-")
      .replace(/^-+|-+$/g, "") || "section";

  // Section headings only. Component demos render headings of their own (card titles,
  // dialog titles), and those are content, not page structure.
  const isPageHeading = (heading) => {
    if (
      heading.closest(".live-preview, [role='dialog'], [role='alertdialog']")
    ) {
      return false;
    }

    const parent = heading.parentElement;
    if (heading.tagName === "H2") {
      return parent?.tagName === "SECTION";
    }

    return (
      parent?.tagName === "SECTION" ||
      parent?.parentElement?.tagName === "SECTION"
    );
  };

  const updateActiveHeading = () => {
    const headings = (vibeDocs._tocHeadings || []).filter(
      (heading) => heading.isConnected,
    );
    let current = headings[0] || null;

    for (const heading of headings) {
      if (heading.getBoundingClientRect().top > TOC_ACTIVATION_OFFSET) break;
      current = heading;
    }

    // The last sections can be too short to ever reach the top of the viewport.
    const page = document.documentElement;
    if (
      headings.length > 0 &&
      window.innerHeight + window.scrollY >= page.scrollHeight - 2
    ) {
      current = headings[headings.length - 1];
    }

    document.querySelectorAll("[data-toc-target]").forEach((link) => {
      if (current && link.dataset.tocTarget === current.id) {
        link.setAttribute("aria-current", "location");
      } else {
        link.removeAttribute("aria-current");
      }
    });
  };

  const scheduleActiveHeadingUpdate = () => {
    if (vibeDocs._tocFrame) return;
    vibeDocs._tocFrame = requestAnimationFrame(() => {
      vibeDocs._tocFrame = 0;
      updateActiveHeading();
    });
  };

  const scrollBehavior = () =>
    window.matchMedia("(prefers-reduced-motion: reduce)").matches
      ? "auto"
      : "smooth";

  // Reads the page's section headings, gives each one an id, and returns them for the
  // "On this page" list. Called after every navigation.
  vibeDocs.collectHeadings = () => {
    const main = document.querySelector("main");
    if (!main) return [];

    // Blazor patches elements in place, so a heading can outlive the text its id was
    // generated from. Regenerate rather than trust an id made on an earlier pass.
    main.querySelectorAll("[data-toc-generated]").forEach((heading) => {
      heading.removeAttribute("id");
      heading.removeAttribute("data-toc-generated");
    });

    const headings = [...main.querySelectorAll("h2, h3")].filter(isPageHeading);
    const result = headings.map((heading) => {
      const text = heading.textContent.replace(/\s+/g, " ").trim();

      if (!heading.id) {
        const base = slugify(text);
        let id = base;
        for (let suffix = 2; document.getElementById(id); suffix++) {
          id = base + "-" + suffix;
        }
        heading.id = id;
        heading.setAttribute("data-toc-generated", "");
      }

      return { id: heading.id, text, level: heading.tagName === "H2" ? 2 : 3 };
    });

    vibeDocs._tocHeadings = headings;

    if (!vibeDocs._tocListening) {
      vibeDocs._tocListening = true;
      window.addEventListener("scroll", scheduleActiveHeadingUpdate, {
        passive: true,
      });
      window.addEventListener("resize", scheduleActiveHeadingUpdate, {
        passive: true,
      });
    }

    // The ids did not exist when the browser first looked for the fragment, so honour a
    // link that points at a section here.
    const fragment = decodeURIComponent(window.location.hash.slice(1));
    if (fragment && vibeDocs._tocFragmentHandledFor !== window.location.href) {
      vibeDocs._tocFragmentHandledFor = window.location.href;
      document.getElementById(fragment)?.scrollIntoView({ block: "start" });
    }

    return result;
  };

  // Called once the list has rendered, so the current section can be marked on it.
  vibeDocs.syncTableOfContents = () => updateActiveHeading();

  vibeDocs.scrollToHeading = (id) => {
    const heading = document.getElementById(id);
    if (!heading) return;

    heading.scrollIntoView({ behavior: scrollBehavior(), block: "start" });

    // Keep the address shareable. The existing state object belongs to Blazor's router.
    const url =
      window.location.pathname +
      window.location.search +
      "#" +
      encodeURIComponent(id);
    vibeDocs._tocFragmentHandledFor = new URL(url, window.location.href).href;
    history.replaceState(history.state, "", url);
  };

  // Scrolls the navigation rail (or the drawer) so the current page's link is visible.
  vibeDocs.revealCurrentNavLink = (nav) => {
    const link = nav?.querySelector?.("[aria-current='page']");
    const scroller = nav?.closest?.(".docs-rail, .sheet-body");
    if (!link || !scroller) return;

    const linkBox = link.getBoundingClientRect();
    const scrollerBox = scroller.getBoundingClientRect();
    if (linkBox.top < scrollerBox.top || linkBox.bottom > scrollerBox.bottom) {
      scroller.scrollTop +=
        linkBox.top -
        scrollerBox.top -
        (scrollerBox.height - linkBox.height) / 2;
    }
  };
})();

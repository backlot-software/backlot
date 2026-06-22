// wwwroot/js/studio.js

// === Phase 1: Sidebar toggle ===
// Use turbo:load, not DOMContentLoaded, so the handler runs after every Turbo Drive navigation.
// The sidebar is data-turbo-permanent, so its DOM node (including .collapsed class) is preserved
// across navigations — turbo:load only needs to restore the aria-label and re-wire the click handler.

document.addEventListener('turbo:load', function () {
    const toggle = document.getElementById('sidebar-toggle');
    const sidebar = document.getElementById('sidebar');
    if (!toggle || !sidebar) return;

    // Restore aria-label based on current collapse state
    const collapsed = sidebar.classList.contains('collapsed');
    toggle.setAttribute('aria-label', collapsed ? 'Expand sidebar' : 'Collapse sidebar');

    // Wire click handler (remove any previous listener by replacing the element or using a flag)
    toggle.onclick = function () {
        const isCollapsed = sidebar.classList.toggle('collapsed');
        this.setAttribute('aria-label', isCollapsed ? 'Expand sidebar' : 'Collapse sidebar');
    };
});

// === Phase 2: Scalar API Reference side panel ===
// Initializes Scalar once across Turbo Drive navigations using a sentinel on the permanent element.

document.addEventListener('turbo:load', function () {
    const panel = document.getElementById('scalar-panel');
    // Return early if panel is missing or already initialized (single-init sentinel)
    if (!panel || panel.dataset.scalarInitialized) return;

    // Guard: typeof check in case CDN loads slowly or is blocked.
    // Set scalarFailed so openScalarPanel() silently ignores clicks rather
    // than showing a blank slide-in panel.
    if (typeof Scalar === 'undefined') {
        panel.dataset.scalarFailed = 'true';
        return;
    }

    const mountEl = document.getElementById('scalar-mount');
    if (!mountEl) return;
    Scalar.createApiReference(mountEl, {
        url: '/openapidoc.json',
        darkMode: false,
        defaultOpenAllTags: false,
    });
    panel.dataset.scalarInitialized = 'true';
});

// Reset open state before Turbo Drive navigates away (does NOT destroy the Scalar instance)
document.addEventListener('turbo:before-visit', function () {
    const panel = document.getElementById('scalar-panel');
    if (panel) panel.classList.remove('is-open');
    const backdrop = document.getElementById('scalar-backdrop');
    if (backdrop) backdrop.style.display = 'none';
});

// Event delegation for "Open API Docs" buttons — reads endpoint from data-* attribute
// instead of inline onclick, preventing XSS via HTML-entity-decoded event handler context.
document.addEventListener('click', function (e) {
    const btn = e.target.closest('[data-action="open-scalar"]');
    if (!btn) return;
    openScalarPanel(btn.dataset.endpoint ?? '');
});

function openScalarPanel(_endpointPath) {
    const panel = document.getElementById('scalar-panel');
    const backdrop = document.getElementById('scalar-backdrop');
    if (!panel || panel.dataset.scalarFailed) return;
    panel.classList.add('is-open');
    if (backdrop) backdrop.style.display = 'block';
    panel.focus();
    // v1: open at top level; hash deep-linking deferred (hash format is Scalar-version-internal)
}

function closeScalarPanel() {
    const panel = document.getElementById('scalar-panel');
    const backdrop = document.getElementById('scalar-backdrop');
    if (panel) panel.classList.remove('is-open');
    if (backdrop) backdrop.style.display = 'none';
}

// Escape key closes the panel
document.addEventListener('keydown', function (e) {
    if (e.key === 'Escape') closeScalarPanel();
});

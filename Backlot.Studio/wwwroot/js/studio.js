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
// Scalar initializes via auto-init: the CDN script discovers <script id="api-reference" data-url="...">
// and renders into #scalar-mount. The panel is data-turbo-permanent, so content persists across
// Turbo Drive navigations. Studio only manages open/close state.

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
    openScalarPanel();
});

function openScalarPanel() {
    const panel = document.getElementById('scalar-panel');
    const backdrop = document.getElementById('scalar-backdrop');
    if (!panel) return;
    panel.classList.add('is-open');
    if (backdrop) backdrop.style.display = 'block';
    panel.focus();
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

// === Phase 3: Copy UID to clipboard ===
// Event delegation for copy-uid buttons on the role list page.
// Wired as a top-level document listener (not inside turbo:load) so it persists
// across Turbo Frame updates that replace the table without a full page navigation.
document.addEventListener('click', function (e) {
    const btn = e.target.closest('[data-action="copy-uid"]');
    if (!btn) return;

    const uid = btn.dataset.uid;
    if (!uid) return;

    navigator.clipboard.writeText(uid).then(function () {
        // Success: swap icon and add visually-hidden announcement
        const icon = btn.querySelector('i');
        if (icon) {
            icon.classList.remove('bi-clipboard');
            icon.classList.add('bi-clipboard-check');
        }
        const announcement = document.createElement('span');
        announcement.className = 'visually-hidden';
        announcement.textContent = 'Copied!';
        btn.appendChild(announcement);

        setTimeout(function () {
            if (icon) {
                icon.classList.remove('bi-clipboard-check');
                icon.classList.add('bi-clipboard');
            }
            if (announcement.parentNode === btn) {
                btn.removeChild(announcement);
            }
        }, 1500);
    }).catch(function () {
        // Silently ignore clipboard failures — the UID is visible and can be manually copied
    });
});

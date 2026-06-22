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

// === Phase 2 additions below ===

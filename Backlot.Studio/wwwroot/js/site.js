// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.


// Full-screen loading overlay wired to Turbo page (Drive) visits. Because every page is rendered
// from a live Backlot API call at request time, a click can look like it did nothing while the
// server round-trip is in flight (Turbo's own progress bar only appears after a delay). We show the
// overlay the instant a Drive visit starts and hide it once the new page has rendered, so menu
// items and the Play button always give immediate feedback.
//
// turbo:visit fires only for top-level Drive visits, NOT for <turbo-frame> loads, so framed updates
// (roles list search/pagination, lazy related-roles) are intentionally left un-blocked. Bound once
// at document level in <head>, which Turbo does not re-execute across visits.
if (!window.__loadingOverlayBound) {
    window.__loadingOverlayBound = true;

    const showOverlay = function () {
        const el = document.getElementById('loading-overlay');
        if (el) { el.classList.remove('hidden'); el.classList.add('flex'); }
    };
    const hideOverlay = function () {
        const el = document.getElementById('loading-overlay');
        if (el) { el.classList.remove('flex'); el.classList.add('hidden'); }
    };

    // A Drive visit began (link click, redirect, form navigation).
    document.addEventListener('turbo:visit', showOverlay);
    // The destination page finished rendering.
    document.addEventListener('turbo:load', hideOverlay);
    // Safety nets so the overlay can never get stuck if a visit aborts or errors, and so a visible
    // overlay is never captured in Turbo's page snapshot cache (which would show it on preview).
    document.addEventListener('turbo:before-cache', hideOverlay);
    document.addEventListener('turbo:fetch-request-error', hideOverlay);
    document.addEventListener('turbo:render', hideOverlay);
}


// Searchable scenario dropdown (combobox). Shared by the Client page and the role Detail page so
// both offer the identical search box. Wires the markup rendered by the _ScenarioSearchBox partial
// (ids namespaced by `id`: {id}-combo, {id}-search, {id}-list, {id}-empty) and calls
// opts.onSelect(name, endpoint) whenever an option is chosen. Returns a small controller exposing
// selectByEndpoint(endpoint) so callers can pre-select a default. Returns null if the box is absent.
window.initScenarioSearch = function (opts) {
    const combo = document.getElementById(opts.id + '-combo');
    if (!combo) return null;

    const search = document.getElementById(opts.id + '-search');
    const list = document.getElementById(opts.id + '-list');
    const emptyRow = document.getElementById(opts.id + '-empty');
    const options = Array.from(list.querySelectorAll('.scenario-option'));

    function openList() {
        list.classList.remove('hidden');
        search.setAttribute('aria-expanded', 'true');
    }
    function closeList() {
        list.classList.add('hidden');
        search.setAttribute('aria-expanded', 'false');
    }
    function filterList() {
        const q = search.value.trim().toLowerCase();
        let visible = 0;
        options.forEach(function (opt) {
            const hay = (opt.dataset.name + ' ' + opt.dataset.endpoint).toLowerCase();
            const match = hay.indexOf(q) !== -1;
            opt.classList.toggle('hidden', !match);
            if (match) visible++;
        });
        if (emptyRow) emptyRow.classList.toggle('hidden', visible !== 0);
    }
    function selectOption(opt) {
        search.value = opt.dataset.name;
        closeList();
        if (opts.onSelect) opts.onSelect(opt.dataset.name, opt.dataset.endpoint);
    }

    search.addEventListener('focus', function () { filterList(); openList(); });
    search.addEventListener('input', function () { filterList(); openList(); });

    options.forEach(function (opt) {
        opt.addEventListener('click', function () { selectOption(opt); });
    });

    document.addEventListener('click', function (e) {
        if (!combo.contains(e.target)) closeList();
    });

    return {
        selectByEndpoint: function (endpoint) {
            const match = options.find(function (o) { return o.dataset.endpoint === endpoint; });
            if (match) selectOption(match);
        }
    };
};

// JSON viewer toggle functionality
// JSON viewer click-to-expand functionality
if (!window.__jsonExpandBound) {
    window.__jsonExpandBound = true;
    document.addEventListener('click', function (e) {
        const viewer = e.target.closest('.json-viewer');
        if (!viewer) return;

        viewer.classList.toggle('expanded');
    });
}
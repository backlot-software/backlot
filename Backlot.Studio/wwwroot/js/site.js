// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.


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
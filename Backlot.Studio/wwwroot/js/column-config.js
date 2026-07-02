// wwwroot/js/column-config.js
// Implements D-06 and D-07: per-skill column configuration for the role list table.
//
// D-06: Gear icon near column headers opens an inline checkbox panel showing available
//       fields from the current result set; changes save immediately to localStorage
//       under studio_columns_{skillType}.
// D-07: Per-skill config only applies when all visible rows share the same primary skill
//       type. Mixed-type results always use default columns and disable the gear icon.

(function () {
    'use strict';

    var DEFAULT_COLUMNS = ['Uid', 'Name', 'LastModified', 'Type', 'Actions'];
    var ALWAYS_VISIBLE = ['Actions'];
    var NON_TOGGLEABLE = ['Uid', 'Actions'];

    /**
     * Initialize column configuration for the given table element.
     * @param {HTMLElement|null} tableEl - The <table> element to configure.
     */
    function initColumnConfig(tableEl) {
        if (!tableEl) return;

        // Step 1: Determine skill type and mode from tbody rows
        var rows = Array.from(tableEl.querySelectorAll('tbody tr'));
        var skillSet = new Set();
        rows.forEach(function (row) {
            var skill = row.dataset.primarySkill || '';
            if (skill) skillSet.add(skill);
        });

        var mode, skillType;
        if (skillSet.size === 1) {
            mode = 'single-type';
            skillType = Array.from(skillSet)[0];
        } else {
            mode = 'mixed';
            skillType = null;
        }

        // Step 2: Determine active columns
        var activeColumns;
        if (mode === 'mixed') {
            activeColumns = DEFAULT_COLUMNS.slice();
        } else {
            var stored = null;
            try {
                var raw = localStorage.getItem('studio_columns_' + skillType);
                if (raw) {
                    var parsed = JSON.parse(raw);
                    if (Array.isArray(parsed) && parsed.every(function (x) { return typeof x === 'string'; })) {
                        stored = parsed;
                    }
                }
            } catch (e) {
                // Invalid JSON in localStorage — ignore and use defaults
            }
            activeColumns = stored || DEFAULT_COLUMNS.slice();
        }

        // Step 3: Collect available field names from data-fields attributes
        var availableFields = new Set();
        rows.forEach(function (row) {
            try {
                var fields = JSON.parse(row.dataset.fields || '[]');
                fields.forEach(function (f) { availableFields.add(f); });
            } catch (e) {
                // skip malformed data-fields
            }
        });

        // Apply initial column visibility
        applyColumnVisibility(tableEl, activeColumns);

        // Step 4 & 5: Render gear icon and panel
        renderGearAndPanel(tableEl, mode, skillType, activeColumns, availableFields);
    }

    /**
     * Show/hide columns based on the active columns list.
     * The Actions column is always visible.
     */
    function applyColumnVisibility(tableEl, activeColumns) {
        var headers = Array.from(tableEl.querySelectorAll('thead th[data-col]'));
        headers.forEach(function (th) {
            var col = th.dataset.col;
            if (ALWAYS_VISIBLE.includes(col)) {
                th.style.display = '';
                return;
            }
            var visible = activeColumns.includes(col);
            th.style.display = visible ? '' : 'none';
        });

        var bodyRows = Array.from(tableEl.querySelectorAll('tbody tr'));
        bodyRows.forEach(function (row) {
            var cells = Array.from(row.querySelectorAll('td[data-col]'));
            cells.forEach(function (td) {
                var col = td.dataset.col;
                if (ALWAYS_VISIBLE.includes(col)) {
                    td.style.display = '';
                    return;
                }
                var visible = activeColumns.includes(col);
                td.style.display = visible ? '' : 'none';
            });
        });
    }

    /**
     * Render the gear button in the thead and the column config panel adjacent to the table.
     */
    function renderGearAndPanel(tableEl, mode, skillType, activeColumns, availableFields) {
        var theadRow = tableEl.querySelector('thead tr');
        if (!theadRow) return;

        // Remove any previously injected gear th to avoid duplicates on frame reload
        var existingGearTh = theadRow.querySelector('.col-gear');
        if (existingGearTh) existingGearTh.remove();

        // Remove any existing panel
        var existingPanel = document.getElementById('col-config-panel');
        if (existingPanel) existingPanel.remove();

        // Create gear <th>
        var gearTh = document.createElement('th');
        gearTh.className = 'col-gear';
        gearTh.style.width = '40px';

        var gearBtn = document.createElement('button');
        gearBtn.type = 'button';
        gearBtn.className = 'text-gray-600 hover:text-gray-800 bg-transparent border-none p-0 cursor-pointer';
        gearBtn.setAttribute('aria-label', 'Configure columns');
        gearBtn.setAttribute('aria-expanded', 'false');
        gearBtn.id = 'col-config-btn';

        var gearIcon = document.createElement('i');
        gearIcon.className = 'bi bi-gear';
        gearBtn.appendChild(gearIcon);

        if (mode === 'mixed') {
            gearBtn.disabled = true;
            gearBtn.title = 'Column configuration is available when filtering to a single role type';
        }

        gearTh.appendChild(gearBtn);
        theadRow.appendChild(gearTh);

        // Create panel
        var panel = document.createElement('div');
        panel.id = 'col-config-panel';
        panel.className = 'bg-white shadow-md p-3 absolute z-10 hidden';
        panel.style.cssText = 'min-width:220px;top:0;right:0;';

        var header = document.createElement('div');
        header.className = 'font-semibold mb-2';
        header.textContent = 'Visible columns';
        panel.appendChild(header);

        // Toggleable fields: available fields minus always-non-toggleable
        var toggleableFields = Array.from(availableFields).filter(function (f) {
            return !NON_TOGGLEABLE.includes(f);
        });

        toggleableFields.forEach(function (field) {
            var label = document.createElement('label');
            label.className = 'flex items-center gap-2 mb-1';

            var checkbox = document.createElement('input');
            checkbox.type = 'checkbox';
            checkbox.checked = activeColumns.includes(field);

            checkbox.addEventListener('change', function () {
                if (checkbox.checked) {
                    if (!activeColumns.includes(field)) activeColumns.push(field);
                } else {
                    var idx = activeColumns.indexOf(field);
                    if (idx !== -1) activeColumns.splice(idx, 1);
                }
                applyColumnVisibility(tableEl, activeColumns);
                if (skillType) {
                    try {
                        localStorage.setItem('studio_columns_' + skillType, JSON.stringify(activeColumns));
                    } catch (e) {
                        // localStorage may be unavailable (private browsing quota exceeded) — silently skip
                    }
                }
            });

            label.appendChild(checkbox);
            label.appendChild(document.createTextNode(field));
            panel.appendChild(label);
        });

        // Insert panel relative to the table container
        var tableContainer = tableEl.closest('.relative') || tableEl.parentElement;
        if (tableContainer) {
            tableContainer.style.position = 'relative';
            tableContainer.appendChild(panel);
        }

        // Panel open/close logic (only in single-type mode)
        if (mode !== 'mixed') {
            gearBtn.addEventListener('click', function (e) {
                e.stopPropagation();
                var isOpen = !panel.classList.contains('hidden');
                panel.classList.toggle('hidden');
                gearBtn.setAttribute('aria-expanded', String(!isOpen));
            });

            document.addEventListener('click', function closeOnOutside(e) {
                if (!panel.contains(e.target) && e.target !== gearBtn) {
                    panel.classList.add('hidden');
                    gearBtn.setAttribute('aria-expanded', 'false');
                }
            });
        }
    }

    /**
     * Find the role list table and initialize column config.
     */
    function onPageReady() {
        var tableEl = document.getElementById('roles-table');
        initColumnConfig(tableEl);
    }

    // Wire to turbo:load and turbo:frame-load for the role-list frame
    document.addEventListener('turbo:load', onPageReady);
    document.addEventListener('turbo:frame-load', function (e) {
        if (e.target && e.target.id === 'role-list') {
            onPageReady();
        }
    });

    // Also run immediately if DOM is already ready (e.g., on direct navigation)
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', onPageReady);
    } else {
        onPageReady();
    }

})();

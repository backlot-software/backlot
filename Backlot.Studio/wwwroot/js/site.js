// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.


// Copy http commands to clipboard
if (!window.__copyHttpRequestBound) {
    window.__copyHttpRequestBound = true;
    document.addEventListener('click', function (e) {
        const btn = e.target.closest('[data-action="copy-http-request"]');
        if (!btn) return;

        const source = document.getElementById('http-request-content');
        if (!source) return;

        const icon = btn.querySelector('i');

        navigator.clipboard.writeText(source.value).then(function () {
            if (icon) {
                icon.classList.remove('bi-clipboard');
                icon.classList.add('bi-clipboard-check');
                setTimeout(function () {
                    icon.classList.remove('bi-clipboard-check');
                    icon.classList.add('bi-clipboard');
                }, 1500);
            }
        });
    });
}

// JSON viewer toggle functionality
if (!window.__jsonToggleBound) {
    window.__jsonToggleBound = true;
    document.addEventListener('click', function (e) {
        const toggleBtn = e.target.closest('[data-action="toggle-json"]');
        if (!toggleBtn) return;

        const targetKey = toggleBtn.getAttribute('data-target');
        const container = document.querySelector('.json-viewer[data-key="' + targetKey + '"]');
        if (!container) return;

        const pre = container.querySelector('pre');
        const icon = toggleBtn.querySelector('.toggle-icon');
        const text = toggleBtn.querySelector('.toggle-text');

        if (container.classList.contains('raw')) {
            container.classList.remove('raw');
            text.textContent = 'Show Raw';
            icon.classList.remove('bi-chevron-up');
            icon.classList.add('bi-chevron-down');
        } else {
            container.classList.add('raw');
            text.textContent = 'Show Formatted';
            icon.classList.remove('bi-chevron-down');
            icon.classList.add('bi-chevron-up');
        }
    });
}
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
// JSON viewer click-to-expand functionality
if (!window.__jsonExpandBound) {
    window.__jsonExpandBound = true;
    document.addEventListener('click', function (e) {
        const viewer = e.target.closest('.json-viewer');
        if (!viewer) return;

        viewer.classList.toggle('expanded');
    });
}
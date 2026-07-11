// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.


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
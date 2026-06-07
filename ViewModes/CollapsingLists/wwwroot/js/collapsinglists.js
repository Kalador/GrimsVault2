// CollapsingLists ViewMode JS
// Handles expand/collapse behaviour for grouped sections.
// Purely layout logic — no feature/domain behaviour.

(function () {
    const groups = document.querySelectorAll('.collapsing-group');

    if (!groups || groups.length === 0) {
        return;
    }

    groups.forEach(group => {
        const header = group.querySelector('.collapsing-header');
        const body = group.querySelector('.collapsing-body');

        if (!header || !body) {
            return;
        }

        header.addEventListener('click', () => {
            const collapsed = body.classList.contains('collapsed');

            if (collapsed) {
                body.classList.remove('collapsed');
                header.classList.remove('collapsed');
            } else {
                body.classList.add('collapsed');
                header.classList.add('collapsed');
            }
        });
    });
})();
Views
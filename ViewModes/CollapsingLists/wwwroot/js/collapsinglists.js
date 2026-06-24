// CollapsingLists ViewMode JS
// 1.0 behaviour, updated for 2.0 class names

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
            const isCollapsed = body.classList.contains('collapsed');

            if (isCollapsed) {
                body.classList.remove('collapsed');
                header.classList.remove('collapsed');
            } else {
                body.classList.add('collapsed');
                header.classList.add('collapsed');
            }
        });
    });
})();

// ---------------------------------------------------------------------------
// Generic: Show/Hide all error rows inside a given group
// ---------------------------------------------------------------------------

function toggleErrorRows(groupName) {
    // Find the group section by matching the visible group title text
    const group = [...document.querySelectorAll('.collapsing-group')]
        .find(g => g.querySelector('.collapsing-title')?.textContent.trim() === groupName);

    if (!group) {
        return;
    }

    // Error rows are marked with .is-excluded (from HasError = true)
    const rows = group.querySelectorAll('.pc-row.is-excluded');

    rows.forEach(row => {
        row.classList.toggle('hidden');
    });
}

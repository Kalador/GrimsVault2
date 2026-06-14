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

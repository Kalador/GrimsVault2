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
// Track visibility state of excluded rows per group
// ---------------------------------------------------------------------------

const groupVisibilityState = {};

// ---------------------------------------------------------------------------
// Generic: Show/Hide all error rows inside a given group
// ---------------------------------------------------------------------------

function toggleErrorRows(groupName) {
    const group = [...document.querySelectorAll('.collapsing-group')]
        .find(g => g.querySelector('.collapsing-title')?.textContent.trim() === groupName);

    if (!group) return;

    const rows = group.querySelectorAll('.pc-row.is-excluded');

    rows.forEach(row => {
        row.classList.toggle('hidden');
    });

    const anyVisible = [...rows].some(r => !r.classList.contains('hidden'));
    groupVisibilityState[groupName] = anyVisible;
}

// ---------------------------------------------------------------------------
// Exclude selected rows and refresh only the table body
// ---------------------------------------------------------------------------

async function excludeSelected(button) {
    const form = button.closest("form");
    if (!form) return;

    const group = form.closest(".collapsing-group");
    const groupName = form.querySelector('input[name="GroupName"]').value;

    // --- LOADING STATE ON ---
    group.classList.add("loading");
    button.disabled = true;

    const formData = new FormData(form);
    const actionUrl = "/ebay/exclude";

    const response = await fetch(actionUrl, {
        method: "POST",
        body: formData
    });

    const html = await response.text();

    const temp = document.createElement("div");
    temp.innerHTML = html;

    const newTbody = temp.querySelector("tbody");
    const oldTbody = form.querySelector("tbody");

    oldTbody.innerHTML = newTbody.innerHTML;

    // Reapply excluded visibility state
    if (groupVisibilityState[groupName]) {
        oldTbody.querySelectorAll('.pc-row.is-excluded').forEach(r => r.classList.remove('hidden'));
    }

    // --- LOADING STATE OFF ---
    group.classList.remove("loading");
    button.disabled = false;
}

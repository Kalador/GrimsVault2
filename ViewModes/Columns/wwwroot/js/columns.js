async function retryCard(id) {
    // Find the current error card by its data-id
    const card = document.querySelector(`.column-card.error[data-id="${CSS.escape(id)}"]`);
    if (!card) return;

    // Prevent double-clicks
    if (card.dataset.loading === "1") return;
    card.dataset.loading = "1";

    // Show loading state inside the card, but keep the wrapper intact
    const inner = card.querySelector(".column-card-inner");
    if (inner) {
        inner.innerHTML = `
            <div class="retry-loading">
                <div class="spinner"></div>
            </div>
        `;
    }

    try {
        const response = await fetch(`/cex/retry?id=${encodeURIComponent(id)}`, {
            method: "POST"
        });

        const html = await response.text();

        // Parse returned HTML (full <a> wrapper + .column-card inside)
        const temp = document.createElement("div");
        temp.innerHTML = html.trim();

        const newRoot = temp.firstElementChild; // <a class="card-link ...">
        if (!newRoot) return;

        const newCard = newRoot.querySelector(".column-card") || newRoot;
        const targetGroup = newCard.dataset.group;

        // Old root is the <a> wrapper around the error card
        const oldRoot = card.closest("a") || card;
        const parentColumn = oldRoot.parentElement;

        // Remove old card
        oldRoot.remove();

        // Find target column by data-group
        const targetColumn = targetGroup
            ? document.querySelector(`.column[data-group="${targetGroup}"]`)
            : null;

        // Insert new card into correct column (or fallback to original column)
        const column = targetColumn ?? parentColumn;

        column.appendChild(newRoot);

        // Sort alphabetically after insertion
        sortColumnAlphabetically(column);

        console.log("Sorting column:", column.id);

    } catch (err) {
        if (inner) {
            inner.innerHTML = `<div class="retry-error">Error refreshing</div>`;
        }
        card.dataset.loading = "0";
    }
}

function sortColumnAlphabetically(column) {
    const cards = Array.from(column.children); // <a> wrappers

    cards.sort((a, b) => {
        const titleA = a.querySelector(".column-card")?.dataset.title?.toLowerCase() || "";
        const titleB = b.querySelector(".column-card")?.dataset.title?.toLowerCase() || "";
        return titleA.localeCompare(titleB);
    });

    cards.forEach(card => column.appendChild(card));
}


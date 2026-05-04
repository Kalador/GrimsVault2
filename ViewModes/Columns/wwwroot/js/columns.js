retryCard = function (id) {
    fetch(`/cex/retry?id=${id}`, { method: "POST" })
        .then(r => r.text())
        .then(html => {
            const oldCard = document.querySelector(`.column-card[data-id='${id}']`);
            if (oldCard) oldCard.outerHTML = html;
        });
};


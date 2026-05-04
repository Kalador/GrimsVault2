// Core ticker logic — works for ALL viewmodes
function applyTickers() {
    document.querySelectorAll('.ticker-content').forEach(content => {
        const container = content.parentElement;

        if (content.scrollWidth > container.clientWidth) {
            content.classList.add('scroll');
        } else {
            content.classList.remove('scroll');
        }
    });
}


document.addEventListener('DOMContentLoaded', applyTickers);
window.addEventListener('resize', applyTickers);
document.addEventListener('vault:content-updated', applyTickers);

// NEW: Auto-detect when ViewMode content appears
const app = document.getElementById('vault-app');

const observer = new MutationObserver(() => {
    requestAnimationFrame(() => {
        applyTickers();
    });
});

observer.observe(app, { childList: true, subtree: true });

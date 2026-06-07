function openThumbnailLightbox(img) {
    const overlay = document.createElement("div");
    overlay.className = "thumbnail-lightbox";

    const big = document.createElement("img");
    big.className = "thumbnail-lightbox-image";
    big.src = img.src;

    // Close on click
    overlay.addEventListener("click", () => {
        overlay.classList.add("closing");
        setTimeout(() => overlay.remove(), 150);
        document.body.style.overflow = "";
    });

    // Close on ESC
    const escHandler = (e) => {
        if (e.key === "Escape") {
            overlay.click();
            document.removeEventListener("keydown", escHandler);
        }
    };
    document.addEventListener("keydown", escHandler);

    // Prevent scroll behind lightbox
    document.body.style.overflow = "hidden";

    overlay.appendChild(big);
    document.body.appendChild(overlay);
}

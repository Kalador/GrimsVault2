function openThumbnailLightbox(img) {
    // Create overlay
    const overlay = document.createElement("div");
    overlay.className = "thumbnail-lightbox";

    // Create image
    const big = document.createElement("img");
    big.className = "thumbnail-lightbox-image";
    big.src = img.src;

    // Close on click
    overlay.onclick = () => overlay.remove();

    // Append
    overlay.appendChild(big);
    document.body.appendChild(overlay);
}

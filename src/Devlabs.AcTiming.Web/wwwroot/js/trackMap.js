/**
 * Initialises zoom + pan on a track map container.
 * - Mouse wheel: zoom toward cursor (min 1x, max 8x)
 * - Left-drag: pan when zoomed in
 * - Double-click: reset to fit
 */
export function init(container) {
    let zoom = 1, panX = 0, panY = 0;
    let dragging = false, dragStartX = 0, dragStartY = 0, panStartX = 0, panStartY = 0;

    const inner = container.querySelector('.track-map-inner');

    function applyTransform() {
        inner.style.transform = `translate(${panX}px, ${panY}px) scale(${zoom})`;
        container.style.cursor = dragging ? 'grabbing' : (zoom > 1 ? 'grab' : '');
    }

    container.addEventListener('wheel', e => {
        e.preventDefault();
        const rect = container.getBoundingClientRect();
        const mouseX = e.clientX - rect.left;
        const mouseY = e.clientY - rect.top;

        const factor = e.deltaY < 0 ? 1.3 : (1 / 1.3);
        const newZoom = Math.min(8, Math.max(1, zoom * factor));
        if (newZoom === zoom) return;

        // Zoom toward the cursor position
        panX = mouseX - (mouseX - panX) * (newZoom / zoom);
        panY = mouseY - (mouseY - panY) * (newZoom / zoom);
        zoom = newZoom;

        // Snap back to origin when fully zoomed out
        if (zoom <= 1) { zoom = 1; panX = 0; panY = 0; }

        applyTransform();
    }, { passive: false });

    container.addEventListener('pointerdown', e => {
        if (zoom <= 1 || e.button !== 0) return;
        dragging = true;
        dragStartX = e.clientX;
        dragStartY = e.clientY;
        panStartX = panX;
        panStartY = panY;
        container.setPointerCapture(e.pointerId);
        applyTransform();
    });

    container.addEventListener('pointermove', e => {
        if (!dragging) return;
        panX = panStartX + (e.clientX - dragStartX);
        panY = panStartY + (e.clientY - dragStartY);
        applyTransform();
    });

    container.addEventListener('pointerup', () => {
        dragging = false;
        applyTransform();
    });

    container.addEventListener('dblclick', () => {
        zoom = 1; panX = 0; panY = 0;
        applyTransform();
    });
}

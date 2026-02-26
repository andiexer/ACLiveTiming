/**
 * Zoom + pan for the track map.
 *
 * Strategy:
 *   - SVG: update viewBox so the browser re-renders vector content at full
 *     resolution (no compositing layer upscale = no blur).
 *   - img: CSS transform on a precisely-positioned wrapper that mirrors the
 *     SVG content area. Raster scaling is acceptable for the background image.
 *
 * Controls:
 *   - Mouse wheel      zoom toward cursor  (1× … 8×)
 *   - Left-drag        pan while zoomed in
 *   - Double-click     reset to fit
 */

const instances = new Map();

export function init(container) {
    const svg  = container.querySelector('svg');
    const imgWrap = container.querySelector('.track-map-img-wrap');

    // Read the natural image size from the initial viewBox
    const vbBase = svg.viewBox.baseVal;
    const pixelWidth  = vbBase.width;
    const pixelHeight = vbBase.height;
    const aspect = pixelWidth / pixelHeight;

    let zoom = 1, vx = 0, vy = 0;
    let dragging = false;
    let dragStartX = 0, dragStartY = 0;
    let vxStart = 0, vyStart = 0;
    let ctmScaleAtStart = 1;
    let cachedContentArea = null;
    // Tracks whether the last pointer interaction was a real drag (> 5px movement).
    // Read by getSvgPoint to suppress accidental POI placement after panning.
    let lastPointerWasDrag = false;

    // ── Content area (the letterboxed region where the map image is drawn) ──

    function computeContentArea() {
        const W = container.clientWidth;
        const H = container.clientHeight;
        const containerAspect = W / H;
        let x, y, w, h;
        if (containerAspect > aspect) {
            // Pillarbox — empty space left/right
            h = H; w = h * aspect;
            x = (W - w) / 2; y = 0;
        } else {
            // Letterbox — empty space top/bottom
            w = W; h = w / aspect;
            x = 0; y = (H - h) / 2;
        }
        return { x, y, w, h };
    }

    function getContentArea() {
        if (!cachedContentArea) cachedContentArea = computeContentArea();
        return cachedContentArea;
    }

    // ── Apply the current zoom / pan state ──────────────────────────────────

    function applyTransform() {
        const vw = pixelWidth  / zoom;
        const vh = pixelHeight / zoom;

        // Clamp so we never pan outside the image
        vx = Math.max(0, Math.min(pixelWidth  - vw, vx));
        vy = Math.max(0, Math.min(pixelHeight - vh, vy));

        // SVG: change viewBox — browser re-rasterises at full resolution
        svg.setAttribute('viewBox', `${vx} ${vy} ${vw} ${vh}`);

        // img wrapper: match the content area and apply zoom / pan
        if (imgWrap) {
            const ca = getContentArea();
            imgWrap.style.left   = ca.x + 'px';
            imgWrap.style.top    = ca.y + 'px';
            imgWrap.style.width  = ca.w + 'px';
            imgWrap.style.height = ca.h + 'px';
            // translate moves the point (vx,vy) to wrapper origin; scale zooms in.
            imgWrap.style.transform =
                `scale(${zoom}) translate(${-vx / pixelWidth * 100}%, ${-vy / pixelHeight * 100}%)`;
        }

        container.style.cursor =
            zoom > 1 ? (dragging ? 'grabbing' : 'grab') : '';
    }

    // ── Wheel: zoom toward cursor ────────────────────────────────────────────

    container.addEventListener('wheel', e => {
        e.preventDefault();

        const factor  = e.deltaY < 0 ? 1.3 : 1 / 1.3;
        const newZoom = Math.min(8, Math.max(1, zoom * factor));
        if (newZoom === zoom) return;

        // Map cursor to SVG coordinate space (accounts for preserveAspectRatio)
        const pt = svg.createSVGPoint();
        pt.x = e.clientX;
        pt.y = e.clientY;
        const svgPt = pt.matrixTransform(svg.getScreenCTM().inverse());

        const oldVw = pixelWidth  / zoom;
        const newVw = pixelWidth  / newZoom;
        const oldVh = pixelHeight / zoom;
        const newVh = pixelHeight / newZoom;

        // Keep the SVG coordinate under the cursor fixed after zoom
        vx = svgPt.x - (svgPt.x - vx) * newVw / oldVw;
        vy = svgPt.y - (svgPt.y - vy) * newVh / oldVh;
        zoom = newZoom;

        if (zoom <= 1) { zoom = 1; vx = 0; vy = 0; }

        applyTransform();
    }, { passive: false });

    // ── Drag: pan ────────────────────────────────────────────────────────────

    container.addEventListener('pointerdown', e => {
        lastPointerWasDrag = false;
        if (zoom <= 1 || e.button !== 0) return;
        dragging   = true;
        dragStartX = e.clientX;
        dragStartY = e.clientY;
        vxStart    = vx;
        vyStart    = vy;
        // CSS pixels per SVG unit at the moment dragging starts
        ctmScaleAtStart = svg.getScreenCTM().a;
        container.setPointerCapture(e.pointerId);
        applyTransform();
    });

    container.addEventListener('pointermove', e => {
        if (!dragging) return;
        // Mark as drag once the pointer moves more than 5 CSS pixels
        if (!lastPointerWasDrag) {
            const dx = e.clientX - dragStartX;
            const dy = e.clientY - dragStartY;
            if (dx * dx + dy * dy > 25) lastPointerWasDrag = true;
        }
        const svgUnitsPerPx = 1 / ctmScaleAtStart;
        vx = vxStart - (e.clientX - dragStartX) * svgUnitsPerPx;
        vy = vyStart - (e.clientY - dragStartY) * svgUnitsPerPx;
        applyTransform();
    });

    container.addEventListener('pointerup', () => {
        dragging = false;
        applyTransform();
    });

    // ── Double-click: reset ──────────────────────────────────────────────────

    container.addEventListener('dblclick', () => {
        zoom = 1; vx = 0; vy = 0;
        applyTransform();
    });

    // ── Resize: invalidate cached content area ───────────────────────────────

    const resizeObserver = new ResizeObserver(() => {
        cachedContentArea = null;
        applyTransform();
    });
    resizeObserver.observe(container);

    // Initial placement of img wrapper
    applyTransform();

    instances.set(container, {
        resizeObserver,
        getLastPointerWasDrag: () => lastPointerWasDrag,
    });
}

export function dispose(container) {
    const inst = instances.get(container);
    if (inst) {
        inst.resizeObserver.disconnect();
        instances.delete(container);
    }
}

/**
 * Converts viewport client coordinates to SVG viewBox coordinates.
 * Returns null when the click followed a pan gesture (drag guard).
 * The returned point can be used directly with the world-coordinate formula:
 *   worldX = svgX * scaleFactor - xOffset
 *   worldZ = svgY * scaleFactor - zOffset
 */
export function getSvgPoint(container, clientX, clientY) {
    const inst = instances.get(container);
    // Suppress placement if the user was panning
    if (inst?.getLastPointerWasDrag()) return null;

    const svg = container.querySelector('svg');
    if (!svg) return null;

    const pt = svg.createSVGPoint();
    pt.x = clientX;
    pt.y = clientY;
    const svgPt = pt.matrixTransform(svg.getScreenCTM().inverse());
    return { x: svgPt.x, y: svgPt.y };
}

// Tracks the chart-group hover position and calls back into Blazor
// so the track map can render a position marker.
//
// Key design: instead of passing a continuously interpolated mouse position,
// we snap to the nearest actual data-point index in the series. This means
// Blazor only re-renders when the crosshair moves to a NEW sample — keeping
// the map marker perfectly in sync with the chart crosshair (same index,
// same spline value, no drift or flicker).

let _controller = null;

/** Binary search: returns the index in `xs` whose value is closest to `val`. */
function nearestIndex(xs, val) {
    let lo = 0, hi = xs.length - 1;
    while (lo < hi) {
        const mid = (lo + hi) >>> 1;
        xs[mid] < val ? (lo = mid + 1) : (hi = mid);
    }
    // Check whether the left neighbour is actually closer
    return (lo > 0 && Math.abs(xs[lo - 1] - val) < Math.abs(xs[lo] - val))
        ? lo - 1 : lo;
}

export function init(dotNetRef, chartsEl) {
    dispose(); // clean up any previous listener

    _controller = new AbortController();
    const { signal } = _controller;
    let lastIdx = -1;

    chartsEl.addEventListener('mousemove', (e) => {
        try {
            const chart = ApexCharts.getChartByID('lc-speed');
            if (!chart) return;

            const g  = chart.w.globals;
            const xs = g.seriesX?.[0];
            if (!xs || xs.length === 0) return;

            // Map mouse pixel-X to data x-value using the speed chart's plot area.
            // All three charts share the same horizontal layout so this is valid
            // regardless of which chart the cursor is over.
            const svg = chart.el?.querySelector('svg');
            if (!svg) return;

            const rect      = svg.getBoundingClientRect();
            const mx        = e.clientX - rect.left;
            const plotLeft  = g.translateX;
            const plotRight = g.translateX + g.gridWidth;
            if (mx < plotLeft || mx > plotRight) return;

            const ratio = (mx - plotLeft) / g.gridWidth;
            const xVal  = g.minX + ratio * (g.maxX - g.minX);

            // Snap to the nearest real data point
            const idx = nearestIndex(xs, xVal);

            // Only notify Blazor when we cross into a new sample — eliminates flicker.
            // Send the index, not the spline value — C# uses it as a direct array lookup.
            if (idx === lastIdx) return;
            lastIdx = idx;

            dotNetRef.invokeMethodAsync('OnChartHover', idx);
        } catch {
            // Chart not yet ready — silently ignore
        }
    }, { signal });

    chartsEl.addEventListener('mouseleave', () => {
        lastIdx = -1;
        try { dotNetRef.invokeMethodAsync('OnChartMouseLeave'); } catch { }
    }, { signal });
}

export function dispose() {
    _controller?.abort();
    _controller = null;
}

// ============================================================
// AssetFlow – Mémoire Intelligente – Graph Engine v2
// Clean force-directed graph inspired by network visualization
// Small nodes, generous spacing, labels outside, dark canvas
// ============================================================

window.GraphEngine = (function () {
    let canvas, ctx, dotnetRef;
    let nodes = [], links = [];
    let animFrame, width, height;
    let intelligence = false;
    let hoveredNode = null;
    let selectedNode = null;
    let dragging = null;
    let dragOffsetX = 0, dragOffsetY = 0;
    let isDark = false;

    // ── Color palette (matches capture 2 style) ──────────────
    const PALETTE = {
        materiel:    { fill: '#3b82f6', stroke: '#60a5fa', label: '#93c5fd', glow: 'rgba(59,130,246,0.35)' },
        incident:    { fill: '#ef4444', stroke: '#f87171', label: '#fca5a5', glow: 'rgba(239,68,68,0.35)'  },
        utilisateur: { fill: '#8b5cf6', stroke: '#a78bfa', label: '#c4b5fd', glow: 'rgba(139,92,246,0.35)' },
        ia:          { fill: '#10b981', stroke: '#34d399', label: '#6ee7b7', glow: 'rgba(16,185,129,0.45)' },
    };

    // Node sizes — deliberately small and clean
    const NODE_RADIUS = {
        materiel:    14,
        incident:    13,
        utilisateur: 12,
        ia:          20,
    };

    // ── Init ─────────────────────────────────────────────────
    function init(canvasId, ref) {
        canvas    = document.getElementById(canvasId);
        dotnetRef = ref;
        if (!canvas) return;
        ctx = canvas.getContext('2d');

        // Detect dark mode from html element
        isDark = document.documentElement.classList.contains('dark');

        // Watch for theme changes
        const observer = new MutationObserver(() => {
            isDark = document.documentElement.classList.contains('dark');
        });
        observer.observe(document.documentElement, { attributes: true, attributeFilter: ['class'] });

        resize();
        window.addEventListener('resize', resize);
        canvas.addEventListener('mousemove', onMouseMove);
        canvas.addEventListener('click', onClick);
        canvas.addEventListener('mousedown', onMouseDown);
        canvas.addEventListener('mouseup', onMouseUp);
        canvas.addEventListener('mouseleave', onMouseLeave);
        loop();
    }

    function resize() {
        if (!canvas) return;
        width  = canvas.parentElement?.offsetWidth  || 800;
        height = canvas.parentElement?.offsetHeight || 600;
        canvas.width  = width;
        canvas.height = height;
        scatterNodes();
    }

    // ── Data ─────────────────────────────────────────────────
    function setData(graphNodes, graphLinks) {
        const cx = (width  || 800) / 2;
        const cy = (height || 600) / 2;
        const count = graphNodes.length;

        // Arrange nodes in a loose spiral/grid to start
        nodes = graphNodes.map((n, i) => {
            const angle = (i / count) * Math.PI * 2;
            const r     = 150 + Math.random() * 120;
            return {
                ...n,
                x:      cx + Math.cos(angle) * r + (Math.random() - 0.5) * 60,
                y:      cy + Math.sin(angle) * r + (Math.random() - 0.5) * 60,
                vx:     0,
                vy:     0,
                radius: getRadius(n),
                pinned: false,
                pulse:  Math.random() * Math.PI * 2,
            };
        });
        links = graphLinks;
    }

    function getRadius(n) {
        const base = NODE_RADIUS[n.type] || 13;
        const extra = Math.min((n.weight || 1) - 1, 4) * 1.5;
        return base + extra;
    }

    function scatterNodes() {
        if (!nodes.length) return;
        const cx = width / 2;
        const cy = height / 2;
        nodes.forEach((n, i) => {
            if (!n.pinned) {
                const angle = (i / nodes.length) * Math.PI * 2;
                const r     = Math.min(width, height) * 0.28;
                n.x = cx + Math.cos(angle) * r + (Math.random() - 0.5) * 50;
                n.y = cy + Math.sin(angle) * r + (Math.random() - 0.5) * 50;
            }
        });
    }

    // ── Physics (force-directed) ──────────────────────────────
    function tick() {
        if (!nodes.length) return;

        const repulsion  = intelligence ? 7000 : 5500;
        const linkDist   = intelligence ? 130  : 110;
        const damping    = 0.82;
        const centerPull = 0.002;
        const cx = width / 2, cy = height / 2;

        // Gravity to center
        nodes.forEach(n => {
            if (n === dragging || n.pinned) return;
            n.vx += (cx - n.x) * centerPull;
            n.vy += (cy - n.y) * centerPull;
        });

        // Node-node repulsion
        for (let i = 0; i < nodes.length; i++) {
            for (let j = i + 1; j < nodes.length; j++) {
                const a = nodes[i], b = nodes[j];
                let dx = a.x - b.x, dy = a.y - b.y;
                const dist = Math.sqrt(dx * dx + dy * dy) || 0.1;
                const minDist = (a.radius + b.radius) * 3.5; // generous spacing
                if (dist < minDist * 2) {
                    const force = repulsion / (dist * dist);
                    const fx = (dx / dist) * force;
                    const fy = (dy / dist) * force;
                    if (!a.pinned && a !== dragging) { a.vx += fx; a.vy += fy; }
                    if (!b.pinned && b !== dragging) { b.vx -= fx; b.vy -= fy; }
                }
            }
        }

        // Link spring force
        links.forEach(l => {
            const a = nodes.find(n => n.id === l.source);
            const b = nodes.find(n => n.id === l.target);
            if (!a || !b) return;
            const dx   = b.x - a.x, dy = b.y - a.y;
            const dist = Math.sqrt(dx * dx + dy * dy) || 0.1;
            const diff = dist - linkDist;
            const k    = (l.strength || 0.5) * 0.025;
            const fx   = (dx / dist) * diff * k;
            const fy   = (dy / dist) * diff * k;
            if (!a.pinned && a !== dragging) { a.vx += fx; a.vy += fy; }
            if (!b.pinned && b !== dragging) { b.vx -= fx; b.vy -= fy; }
        });

        // Integrate positions
        const margin = 60;
        nodes.forEach(n => {
            if (n === dragging) return;
            n.vx *= damping;
            n.vy *= damping;
            n.x   = Math.max(margin, Math.min(width  - margin, n.x + n.vx));
            n.y   = Math.max(margin, Math.min(height - margin, n.y + n.vy));
            n.pulse += intelligence ? 0.035 : 0.02;
        });
    }

    // ── Draw ─────────────────────────────────────────────────
    function draw() {
        ctx.clearRect(0, 0, width, height);

        // Background — slightly tinted in dark mode
        if (isDark) {
            ctx.fillStyle = '#0a0f1a';
        } else {
            ctx.fillStyle = '#f1f5f9';
        }
        ctx.fillRect(0, 0, width, height);

        // Subtle grid dots (like capture 2)
        drawGrid();
        drawLinks();
        drawNodes();
        if (hoveredNode && hoveredNode !== selectedNode) drawTooltip(hoveredNode);
    }

    function drawGrid() {
        const step = 40;
        const dotColor = isDark ? 'rgba(148,163,184,0.06)' : 'rgba(15,23,42,0.06)';
        ctx.fillStyle = dotColor;
        for (let x = step; x < width; x += step) {
            for (let y = step; y < height; y += step) {
                ctx.beginPath();
                ctx.arc(x, y, 1, 0, Math.PI * 2);
                ctx.fill();
            }
        }
    }

    function drawLinks() {
        links.forEach(l => {
            const a = nodes.find(n => n.id === l.source);
            const b = nodes.find(n => n.id === l.target);
            if (!a || !b) return;

            const isActive = selectedNode &&
                (selectedNode.id === a.id || selectedNode.id === b.id);

            // Line
            ctx.beginPath();
            ctx.moveTo(a.x, a.y);
            ctx.lineTo(b.x, b.y);

            if (isActive) {
                ctx.strokeStyle = isDark ? 'rgba(148,163,184,0.5)' : 'rgba(15,23,42,0.35)';
                ctx.lineWidth   = 1.5;
            } else {
                ctx.strokeStyle = isDark ? 'rgba(148,163,184,0.12)' : 'rgba(15,23,42,0.1)';
                ctx.lineWidth   = 0.8;
            }
            ctx.stroke();

            // Animated particle along active link (intelligence mode)
            if (intelligence && isActive) {
                const t  = (Date.now() % 1600) / 1600;
                const px = a.x + (b.x - a.x) * t;
                const py = a.y + (b.y - a.y) * t;
                ctx.beginPath();
                ctx.arc(px, py, 2.5, 0, Math.PI * 2);
                const col = PALETTE[selectedNode?.type] || PALETTE.materiel;
                ctx.fillStyle = col.stroke;
                ctx.fill();
            }

            // Link label (only on hover/select, tiny)
            if (isActive && l.label) {
                const mx = (a.x + b.x) / 2;
                const my = (a.y + b.y) / 2;
                ctx.font        = '9px Segoe UI';
                ctx.fillStyle   = isDark ? 'rgba(148,163,184,0.6)' : 'rgba(71,85,105,0.7)';
                ctx.textAlign   = 'center';
                ctx.textBaseline= 'middle';
                ctx.fillText(l.label, mx, my - 6);
            }
        });
    }

    function drawNodes() {
        // Draw all non-selected/hovered nodes first, then active ones on top
        const order = [...nodes].sort((a, b) => {
            const aActive = (a === selectedNode || a === hoveredNode) ? 1 : 0;
            const bActive = (b === selectedNode || b === hoveredNode) ? 1 : 0;
            return aActive - bActive;
        });

        order.forEach(n => {
            const col   = PALETTE[n.type] || PALETTE.materiel;
            const isHov = n === hoveredNode;
            const isSel = n === selectedNode;
            const r     = n.radius;
            const pulse = Math.sin(n.pulse) * (intelligence ? 3 : 2);

            // Outer glow ring (only for hovered / selected / critical)
            if (isHov || isSel || n.status === 'critical' || intelligence) {
                const glowSize = r + 10 + (isSel ? 4 : 0) + pulse;
                const grd = ctx.createRadialGradient(n.x, n.y, r * 0.6, n.x, n.y, glowSize);
                grd.addColorStop(0, col.glow);
                grd.addColorStop(1, 'transparent');
                ctx.beginPath();
                ctx.arc(n.x, n.y, glowSize, 0, Math.PI * 2);
                ctx.fillStyle = grd;
                ctx.fill();
            }

            // Selection ring (dashed)
            if (isSel) {
                ctx.beginPath();
                ctx.arc(n.x, n.y, r + 6, 0, Math.PI * 2);
                ctx.strokeStyle = col.stroke;
                ctx.lineWidth   = 1.5;
                ctx.setLineDash([4, 3]);
                ctx.stroke();
                ctx.setLineDash([]);
            }

            // Node circle — flat fill with subtle inner highlight
            ctx.beginPath();
            ctx.arc(n.x, n.y, r, 0, Math.PI * 2);
            ctx.fillStyle   = col.fill;
            ctx.shadowColor = col.fill;
            ctx.shadowBlur  = isHov || isSel ? 14 : (intelligence ? 8 : 5);
            ctx.fill();
            ctx.shadowBlur  = 0;

            // Inner highlight (top-left shine)
            const shine = ctx.createRadialGradient(
                n.x - r * 0.35, n.y - r * 0.35, r * 0.05,
                n.x, n.y, r
            );
            shine.addColorStop(0, 'rgba(255,255,255,0.28)');
            shine.addColorStop(0.5, 'rgba(255,255,255,0.06)');
            shine.addColorStop(1, 'rgba(0,0,0,0.1)');
            ctx.beginPath();
            ctx.arc(n.x, n.y, r, 0, Math.PI * 2);
            ctx.fillStyle = shine;
            ctx.fill();

            // Node border
            ctx.beginPath();
            ctx.arc(n.x, n.y, r, 0, Math.PI * 2);
            ctx.strokeStyle = col.stroke;
            ctx.lineWidth   = 1;
            ctx.globalAlpha = 0.6;
            ctx.stroke();
            ctx.globalAlpha = 1;

            // Label BELOW the node (outside, like capture 2)
            const labelY = n.y + r + 14;
            const label  = n.label.length > 14 ? n.label.slice(0, 14) + '…' : n.label;

            // Label background pill for readability
            ctx.font = `600 ${Math.max(9, Math.min(11, r * 0.75))}px 'Segoe UI', sans-serif`;
            const textW = ctx.measureText(label).width;
            const padX = 5, padY = 3;
            const pillX = n.x - textW / 2 - padX;
            const pillY = labelY - 9 - padY;
            const pillW = textW + padX * 2;
            const pillH = 14 + padY * 2;

            ctx.fillStyle = isDark ? 'rgba(10,15,26,0.75)' : 'rgba(241,245,249,0.85)';
            roundRect(ctx, pillX, pillY, pillW, pillH, 4);
            ctx.fill();

            // Label text
            ctx.fillStyle   = col.label;
            ctx.textAlign   = 'center';
            ctx.textBaseline= 'alphabetic';
            ctx.fillText(label, n.x, labelY);

            // Warning exclamation for critical nodes (small badge)
            if (n.status === 'critical') {
                const bx = n.x + r * 0.65;
                const by = n.y - r * 0.65;
                ctx.beginPath();
                ctx.arc(bx, by, 5, 0, Math.PI * 2);
                ctx.fillStyle = '#f59e0b';
                ctx.fill();
                ctx.fillStyle   = '#fff';
                ctx.font        = 'bold 7px Segoe UI';
                ctx.textAlign   = 'center';
                ctx.textBaseline= 'middle';
                ctx.fillText('!', bx, by + 0.5);
            }
        });
    }

    function drawTooltip(n) {
        const col   = PALETTE[n.type] || PALETTE.materiel;
        const lines = [n.label, n.detail].filter(Boolean);
        const pad   = 10, lineH = 17;
        const w     = Math.min(220, width * 0.35);
        const h     = lines.length * lineH + pad * 2;

        let tx = n.x + n.radius + 14;
        let ty = n.y - h / 2;
        if (tx + w > width - 10)  tx = n.x - n.radius - w - 14;
        if (ty < 10)              ty = 10;
        if (ty + h > height - 10) ty = height - h - 10;

        // Shadow
        ctx.shadowColor = 'rgba(0,0,0,0.25)';
        ctx.shadowBlur  = 12;

        ctx.fillStyle   = isDark ? 'rgba(15,23,42,0.96)' : 'rgba(255,255,255,0.97)';
        ctx.strokeStyle = col.fill;
        ctx.lineWidth   = 1;
        roundRect(ctx, tx, ty, w, h, 8);
        ctx.fill();
        ctx.stroke();
        ctx.shadowBlur  = 0;

        // Accent line on left
        ctx.fillStyle = col.fill;
        roundRect(ctx, tx, ty, 3, h, [8, 0, 0, 8]);
        ctx.fill();

        // Text
        lines.forEach((line, i) => {
            ctx.fillStyle    = i === 0
                ? (isDark ? '#e2e8f0' : '#0f172a')
                : (isDark ? '#94a3b8' : '#64748b');
            ctx.font         = i === 0
                ? 'bold 11px Segoe UI'
                : '10px Segoe UI';
            ctx.textAlign    = 'left';
            ctx.textBaseline = 'top';
            const maxLen = Math.floor((w - pad * 2 - 4) / 6.5);
            const txt    = line.length > maxLen ? line.slice(0, maxLen) + '…' : line;
            ctx.fillText(txt, tx + pad + 4, ty + pad + i * lineH);
        });
    }

    // ── Helpers ──────────────────────────────────────────────
    function roundRect(ctx, x, y, w, h, r) {
        if (typeof r === 'number') r = [r, r, r, r];
        const [tl, tr, br, bl] = r;
        ctx.beginPath();
        ctx.moveTo(x + tl, y);
        ctx.lineTo(x + w - tr, y);
        ctx.quadraticCurveTo(x + w, y,     x + w, y + tr);
        ctx.lineTo(x + w,     y + h - br);
        ctx.quadraticCurveTo(x + w, y + h, x + w - br, y + h);
        ctx.lineTo(x + bl,    y + h);
        ctx.quadraticCurveTo(x,     y + h, x, y + h - bl);
        ctx.lineTo(x,         y + tl);
        ctx.quadraticCurveTo(x, y, x + tl, y);
        ctx.closePath();
    }

    // ── Events ───────────────────────────────────────────────
    function getNodeAt(e) {
        const rect = canvas.getBoundingClientRect();
        const mx   = e.clientX - rect.left;
        const my   = e.clientY - rect.top;
        // Check from top (last drawn = highest) downward
        for (let i = nodes.length - 1; i >= 0; i--) {
            const n  = nodes[i];
            const dx = n.x - mx, dy = n.y - my;
            if (Math.sqrt(dx * dx + dy * dy) <= n.radius + 6) return n;
        }
        return null;
    }

    function onMouseMove(e) {
        const rect = canvas.getBoundingClientRect();
        const mx   = e.clientX - rect.left;
        const my   = e.clientY - rect.top;
        hoveredNode = getNodeAt(e);
        canvas.style.cursor = hoveredNode ? 'pointer' : 'default';

        if (dragging) {
            dragging.x = mx + dragOffsetX;
            dragging.y = my + dragOffsetY;
            dragging.vx = 0;
            dragging.vy = 0;
        }
    }

    function onClick(e) {
        if (dragging) return; // was dragged, not a click
        const n  = getNodeAt(e);
        selectedNode = (n === selectedNode) ? null : n;
        if (n && dotnetRef) {
            dotnetRef.invokeMethodAsync('OnNodeClicked', n.id);
        }
    }

    function onMouseDown(e) {
        const n = getNodeAt(e);
        if (n) {
            const rect  = canvas.getBoundingClientRect();
            dragging    = n;
            n.pinned    = true;
            dragOffsetX = n.x - (e.clientX - rect.left);
            dragOffsetY = n.y - (e.clientY - rect.top);
        }
    }

    function onMouseUp() {
        if (dragging) {
            // Unpin after short delay so it doesn't fly away
            const d = dragging;
            setTimeout(() => { if (!d.isIa) d.pinned = false; }, 800);
        }
        dragging = null;
    }

    function onMouseLeave() {
        hoveredNode = null;
        if (dragging) {
            const d = dragging;
            dragging = null;
            setTimeout(() => { d.pinned = false; }, 800);
        }
    }

    // ── Loop ─────────────────────────────────────────────────
    function loop() {
        tick();
        draw();
        animFrame = requestAnimationFrame(loop);
    }

    function setIntelligenceMode(enabled) {
        intelligence = enabled;
    }

    function highlight(nodeId) {
        const n = nodes.find(n => n.id === nodeId);
        selectedNode = n || null;
        if (n) {
            // Gently move toward center-ish so it's visible
            const cx = width / 2, cy = height / 2;
            n.vx += (cx - n.x) * 0.05;
            n.vy += (cy - n.y) * 0.05;
        }
    }

    function destroy() {
        cancelAnimationFrame(animFrame);
        window.removeEventListener('resize', resize);
    }

    return { init, setData, setIntelligenceMode, highlight, destroy, resize };
})();
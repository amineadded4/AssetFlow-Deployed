// ============================================================
// AssetFlow – Mémoire Intelligente – Graph Engine v3
// Contextual force-directed graph (no global graph, per-entity)
// ============================================================

window.GraphEngine = (function () {
    let canvas, ctx, dotnetRef;
    let nodes = [], links = [];
    let animFrame, w, h;
    let hoveredNode = null;
    let dragging = null;
    let dragOX = 0, dragOY = 0;

    const C = {
        materiel:    '#3b82f6',
        incident:    '#ef4444',
        utilisateur: '#8b5cf6',
        commande:    '#f59e0b',
        projet:      '#10b981',
        demande:     '#14b8a6',
        commentaire: '#ec4899',
        ia:          '#94a3b8'
    };

    const GLOW = {
        materiel:    'rgba(59,130,246,0.30)',
        incident:    'rgba(239,68,68,0.30)',
        utilisateur: 'rgba(139,92,246,0.30)',
        commande:    'rgba(245,158,11,0.30)',
        projet:      'rgba(16,185,129,0.30)',
        demande:     'rgba(20,184,166,0.30)',
        commentaire: 'rgba(236,72,153,0.20)',
        ia:          'rgba(148,163,184,0.20)'
    };

    // ── Init ─────────────────────────────────────────────────
    function init(canvasId, ref) {
        canvas    = document.getElementById(canvasId);
        dotnetRef = ref;
        if (!canvas) return;
        ctx = canvas.getContext('2d');
        resize();
        window.addEventListener('resize', resize);
        attachEvents();
        loop();
    }

    function resize() {
        if (!canvas) return;
        const area = canvas.parentElement;
        w = area ? area.offsetWidth  : 800;
        h = area ? area.offsetHeight : 600;
        canvas.width  = w;
        canvas.height = h;
    }

    // ── Data ─────────────────────────────────────────────────
    function setData(graphNodes, graphLinks) {
        const cx = (w || 800) / 2;
        const cy = (h || 600) / 2;
        const count = graphNodes.length;

        nodes = graphNodes.map((n, i) => {
            // Place center node in middle, others in orbit
            if (n.isCenter) {
                return { ...n, x: cx, y: cy, vx: 0, vy: 0, radius: getRadius(n), pinned: true, pulse: 0 };
            }
            const angle = (i / count) * Math.PI * 2;
            const r = 140 + Math.random() * 80;
            return {
                ...n,
                x:      cx + Math.cos(angle) * r + (Math.random() - 0.5) * 40,
                y:      cy + Math.sin(angle) * r + (Math.random() - 0.5) * 40,
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
        if (n.isCenter) return 26;
        const base = { materiel: 18, incident: 15, utilisateur: 17, commande: 14, projet: 17, demande: 16, commentaire: 12, ia: 20 };
        const b = base[n.type] || 14;
        const extra = Math.min((n.weight || 1) - 1, 4) * 1.2;
        return b + extra;
    }

    // ── Physics ───────────────────────────────────────────────
    function tick() {
        if (!nodes.length) return;
        const cx = w / 2, cy = h / 2;
        const REP = 4500, LINK_DIST = 130, DAMP = 0.82, CPULL = 0.003;

        nodes.forEach(n => {
            if (n === dragging || n.pinned) return;
            n.vx += (cx - n.x) * CPULL;
            n.vy += (cy - n.y) * CPULL;
        });

        for (let i = 0; i < nodes.length; i++) {
            for (let j = i + 1; j < nodes.length; j++) {
                const a = nodes[i], b = nodes[j];
                let dx = a.x - b.x, dy = a.y - b.y;
                const d = Math.sqrt(dx * dx + dy * dy) || 0.1;
                const minD = (a.radius + b.radius) * 3;
                if (d < minD * 2) {
                    const f = REP / (d * d);
                    const fx = (dx / d) * f, fy = (dy / d) * f;
                    if (!a.pinned && a !== dragging) { a.vx += fx; a.vy += fy; }
                    if (!b.pinned && b !== dragging) { b.vx -= fx; b.vy -= fy; }
                }
            }
        }

        links.forEach(l => {
            const a = nodes.find(n => n.id === l.source);
            const b = nodes.find(n => n.id === l.target);
            if (!a || !b) return;
            const dx = b.x - a.x, dy = b.y - a.y;
            const d = Math.sqrt(dx * dx + dy * dy) || 0.1;
            const diff = d - LINK_DIST;
            const k = (l.strength || 0.5) * 0.022;
            const fx = (dx / d) * diff * k, fy = (dy / d) * diff * k;
            if (!a.pinned && a !== dragging) { a.vx += fx; a.vy += fy; }
            if (!b.pinned && b !== dragging) { b.vx -= fx; b.vy -= fy; }
        });

        const M = 55;
        nodes.forEach(n => {
            if (n === dragging) return;
            n.vx *= DAMP; n.vy *= DAMP;
            n.x = Math.max(M, Math.min(w - M, n.x + n.vx));
            n.y = Math.max(M, Math.min(h - M, n.y + n.vy));
            n.pulse += 0.022;
        });
    }

    // ── Draw ─────────────────────────────────────────────────
    function draw() {
        if (!ctx) return;
        ctx.clearRect(0, 0, w, h);

        // Dark background
        ctx.fillStyle = '#0b0f1a';
        ctx.fillRect(0, 0, w, h);

        // Grid dots
        ctx.fillStyle = 'rgba(30,45,64,0.55)';
        for (let x = 32; x < w; x += 32) {
            for (let y = 32; y < h; y += 32) {
                ctx.beginPath();
                ctx.arc(x, y, 0.8, 0, Math.PI * 2);
                ctx.fill();
            }
        }

        drawLinks();
        drawNodes();
    }

    function drawLinks() {
        links.forEach(l => {
            const a = nodes.find(n => n.id === l.source);
            const b = nodes.find(n => n.id === l.target);
            if (!a || !b) return;

            const isHov = hoveredNode && (hoveredNode.id === a.id || hoveredNode.id === b.id);

            ctx.beginPath();
            ctx.moveTo(a.x, a.y);
            ctx.lineTo(b.x, b.y);
            ctx.strokeStyle = isHov ? 'rgba(148,163,184,0.45)' : 'rgba(30,45,64,0.9)';
            ctx.lineWidth   = isHov ? 1.5 : 0.8;
            ctx.stroke();

            if (isHov && l.label) {
                const mx = (a.x + b.x) / 2, my = (a.y + b.y) / 2;
                ctx.font        = '10px DM Mono, monospace';
                ctx.fillStyle   = 'rgba(100,116,139,0.8)';
                ctx.textAlign   = 'center';
                ctx.textBaseline = 'middle';
                ctx.fillText(l.label, mx, my - 8);
            }
        });
    }

    function drawNodes() {
        const order = [...nodes].sort((a, b) => (a === hoveredNode ? 1 : 0) - (b === hoveredNode ? 1 : 0));
        order.forEach(n => {
            const col  = C[n.type] || '#94a3b8';
            const glow = GLOW[n.type] || 'transparent';
            const isHov = n === hoveredNode;
            const r    = n.radius;
            const ps   = Math.sin(n.pulse) * 2;

            if (isHov || n.isCenter || n.status === 'critical') {
                const gs = r + (isHov ? 14 : 8) + ps;
                const g = ctx.createRadialGradient(n.x, n.y, r * 0.5, n.x, n.y, gs);
                g.addColorStop(0, glow);
                g.addColorStop(1, 'transparent');
                ctx.beginPath();
                ctx.arc(n.x, n.y, gs, 0, Math.PI * 2);
                ctx.fillStyle = g;
                ctx.fill();
            }

            // Dashed ring for center node
            if (n.isCenter) {
                ctx.beginPath();
                ctx.arc(n.x, n.y, r + 5 + ps, 0, Math.PI * 2);
                ctx.strokeStyle = col + '60';
                ctx.lineWidth   = 1;
                ctx.setLineDash([4, 3]);
                ctx.stroke();
                ctx.setLineDash([]);
            }

            ctx.beginPath();
            ctx.arc(n.x, n.y, r, 0, Math.PI * 2);
            ctx.fillStyle  = col;
            ctx.shadowColor = col;
            ctx.shadowBlur  = isHov ? 12 : 5;
            ctx.fill();
            ctx.shadowBlur  = 0;

            // Inner shine
            const sh = ctx.createRadialGradient(n.x - r * 0.3, n.y - r * 0.3, 1, n.x, n.y, r);
            sh.addColorStop(0, 'rgba(255,255,255,0.22)');
            sh.addColorStop(1, 'rgba(0,0,0,0.1)');
            ctx.beginPath();
            ctx.arc(n.x, n.y, r, 0, Math.PI * 2);
            ctx.fillStyle = sh;
            ctx.fill();

            ctx.beginPath();
            ctx.arc(n.x, n.y, r, 0, Math.PI * 2);
            ctx.strokeStyle = col + '80';
            ctx.lineWidth   = 1;
            ctx.stroke();

            // Warning badge
            if (n.status === 'critical') {
                const bx = n.x + r * 0.7, by = n.y - r * 0.7;
                ctx.beginPath();
                ctx.arc(bx, by, 5, 0, Math.PI * 2);
                ctx.fillStyle = '#f59e0b';
                ctx.fill();
                ctx.fillStyle    = '#fff';
                ctx.font         = 'bold 7px DM Sans, sans-serif';
                ctx.textAlign    = 'center';
                ctx.textBaseline = 'middle';
                ctx.fillText('!', bx, by + 0.5);
            }

            // Label below node
            const lbl = n.label.length > 14 ? n.label.slice(0, 14) + '…' : n.label;
            ctx.font = `500 ${Math.max(9, Math.min(11, r * 0.75))}px 'DM Sans', sans-serif`;
            const tw = ctx.measureText(lbl).width;
            const ly = n.y + r + 13;
            ctx.fillStyle = 'rgba(11,15,26,0.82)';
            roundRect(n.x - tw / 2 - 5, ly - 10, tw + 10, 14, 3);
            ctx.fill();
            ctx.fillStyle    = col + 'dd';
            ctx.textAlign    = 'center';
            ctx.textBaseline = 'alphabetic';
            ctx.fillText(lbl, n.x, ly);
        });
    }

    function roundRect(x, y, ww, hh, r) {
        ctx.beginPath();
        ctx.moveTo(x + r, y);
        ctx.lineTo(x + ww - r, y);
        ctx.quadraticCurveTo(x + ww, y, x + ww, y + r);
        ctx.lineTo(x + ww, y + hh - r);
        ctx.quadraticCurveTo(x + ww, y + hh, x + ww - r, y + hh);
        ctx.lineTo(x + r, y + hh);
        ctx.quadraticCurveTo(x, y + hh, x, y + hh - r);
        ctx.lineTo(x, y + r);
        ctx.quadraticCurveTo(x, y, x + r, y);
        ctx.closePath();
    }

    // ── Events ───────────────────────────────────────────────
    function nodeAt(e) {
        const rect = canvas.getBoundingClientRect();
        const mx = e.clientX - rect.left, my = e.clientY - rect.top;
        for (let i = nodes.length - 1; i >= 0; i--) {
            const n = nodes[i];
            const dx = n.x - mx, dy = n.y - my;
            if (Math.sqrt(dx * dx + dy * dy) <= n.radius + 6) return n;
        }
        return null;
    }

    function attachEvents() {
        canvas.addEventListener('mousemove', e => {
            const n = nodeAt(e);
            hoveredNode = n;
            canvas.style.cursor = n ? 'pointer' : 'default';
            if (n) showTip(e, n);
            else hideTip();
            if (dragging) {
                const rect = canvas.getBoundingClientRect();
                dragging.x = e.clientX - rect.left + dragOX;
                dragging.y = e.clientY - rect.top  + dragOY;
                dragging.vx = 0; dragging.vy = 0;
            }
        });
        canvas.addEventListener('mousedown', e => {
            const n = nodeAt(e);
            if (n) {
                const rect = canvas.getBoundingClientRect();
                dragging = n;
                dragOX = n.x - (e.clientX - rect.left);
                dragOY = n.y - (e.clientY - rect.top);
            }
        });
        canvas.addEventListener('mouseup', () => { dragging = null; });
        canvas.addEventListener('mouseleave', () => { hoveredNode = null; dragging = null; hideTip(); });
    }

    function showTip(e, n) {
        const tip = document.getElementById('node-tip');
        if (!tip) return;

        // Position basée sur le NŒUD → tooltip pile au-dessus
        const rect = canvas.getBoundingClientRect();
        const nodeScreenX = n.x;
        const nodeScreenY = n.y - n.radius - 10;

        tip.style.display = 'block';
        tip.style.left = (rect.left + nodeScreenX) + 'px';
        tip.style.top  = (rect.top  + nodeScreenY) + 'px';
        // Le CSS applique transform: translate(-50%, -100%) → centré + au-dessus

        const typeLabels = { materiel: 'Matériel', incident: 'Incident', utilisateur: 'Utilisateur', commande: 'Commande / Offre', projet: 'Projet', demande: 'Demande d\'achat', commentaire: 'Commentaire' };
        const typeCols   = { materiel: 'rgba(59,130,246,.15)', incident: 'rgba(239,68,68,.15)', utilisateur: 'rgba(139,92,246,.15)', commande: 'rgba(245,158,11,.15)', projet: 'rgba(16,185,129,.15)', demande: 'rgba(20,184,166,.15)', commentaire: 'rgba(236,72,153,.15)' };
        const textCols   = { materiel: '#3b82f6', incident: '#ef4444', utilisateur: '#8b5cf6', commande: '#f59e0b', projet: '#10b981', demande: '#14b8a6', commentaire: '#ec4899' };

        const tc = document.getElementById('tip-type');
        if (tc) { tc.textContent = typeLabels[n.type] || n.type; tc.style.background = typeCols[n.type] || 'rgba(148,163,184,.15)'; tc.style.color = textCols[n.type] || '#94a3b8'; }
        const tt = document.getElementById('tip-title');
        if (tt) tt.textContent = n.label;
        const tb = document.getElementById('tip-body');
        if (tb) tb.textContent = n.detail || '';
    }

    function hideTip() {
        const tip = document.getElementById('node-tip');
        if (tip) tip.style.display = 'none';
    }

    // ── Loop ─────────────────────────────────────────────────
    function loop() {
        tick();
        draw();
        animFrame = requestAnimationFrame(loop);
    }

    function setIntelligenceMode(enabled) { /* kept for compat */ }

    function highlight(nodeId) {
        const n = nodes.find(n => n.id === nodeId);
        if (n) { n.vx += (w / 2 - n.x) * 0.05; n.vy += (h / 2 - n.y) * 0.05; }
    }

    function destroy() {
        cancelAnimationFrame(animFrame);
        window.removeEventListener('resize', resize);
    }

    return { init, setData, setIntelligenceMode, highlight, destroy, resize };
})();
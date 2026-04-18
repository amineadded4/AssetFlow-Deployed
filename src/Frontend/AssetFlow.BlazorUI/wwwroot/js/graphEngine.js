// ═══════════════════════════════════════════════════════════════
// Mémoire Intelligente – Graph Engine v4
// Shapes:  materiel/incident → hexagone
//          projet             → diamant
//          commande/demande   → rectangle arrondi
//          utilisateur        → cercle + anneau + avatar
//          commentaire        → bulle
// ═══════════════════════════════════════════════════════════════

window.GraphEngine = (function () {

    let canvas, ctx, dotnetRef;
    let nodes = [], links = [];
    let animFrame, w, h;
    let hoveredNode = null;
    let dragging = null, dragOX = 0, dragOY = 0;
    let tickCount = 0;
    let initialized = false;

    // ── Palette ──────────────────────────────────────────────────
    const C = {
        materiel:    '#4F8EF7',
        incident:    '#EF4444',
        utilisateur: '#8B5CF6',
        commande:    '#D97706',
        projet:      '#10B981',
        demande:     '#14B8A6',
        commentaire: '#EC4899',
        ia:          '#94A3B8'
    };
    const C_BG = {
        materiel:    '#040C1E',
        incident:    '#1A0606',
        utilisateur: '#0C0714',
        commande:    '#0D0700',
        projet:      '#020F07',
        demande:     '#020D0D',
        commentaire: '#140610',
        ia:          '#0a0a0a'
    };
    const C_GLOW = {
        materiel:    'rgba(79,142,247,0.35)',
        incident:    'rgba(239,68,68,0.32)',
        utilisateur: 'rgba(139,92,246,0.32)',
        commande:    'rgba(217,119,6,0.32)',
        projet:      'rgba(16,185,129,0.32)',
        demande:     'rgba(20,184,166,0.32)',
        commentaire: 'rgba(236,72,153,0.28)',
        ia:          'rgba(148,163,184,0.18)'
    };

    // ── Tooltip labels ────────────────────────────────────────────
    const TYPE_LABELS = {
        materiel:    'Matériel',
        incident:    'Incident',
        utilisateur: 'Utilisateur',
        commande:    'Commande',
        projet:      'Projet',
        demande:     "Demande d'achat",
        commentaire: 'Commentaire'
    };
    const TIP_BG = {
        materiel:    'rgba(79,142,247,.15)',
        incident:    'rgba(239,68,68,.15)',
        utilisateur: 'rgba(139,92,246,.15)',
        commande:    'rgba(217,119,6,.15)',
        projet:      'rgba(16,185,129,.15)',
        demande:     'rgba(20,184,166,.15)',
        commentaire: 'rgba(236,72,153,.15)'
    };

    // ══════════════════════════════════════════════════════════════
    // PUBLIC API
    // ══════════════════════════════════════════════════════════════

    function init(canvasId, ref) {
        if (animFrame) { cancelAnimationFrame(animFrame); animFrame = null; }
        canvas    = document.getElementById(canvasId);
        dotnetRef = ref;
        if (!canvas) return;
        ctx = canvas.getContext('2d');
        resize();
        if (!initialized) {
            window.addEventListener('resize', resize);
            initialized = true;
        }
        attachEvents();
        loop();
    }

    function setData(graphNodes, graphLinks) {
        tickCount = 0;
        const cx = (w || 800) / 2;
        const cy = (h || 600) / 2;
        const total = graphNodes.length;

        nodes = graphNodes.map((n, i) => {
            const r = nodeRadius(n);
            if (n.isCenter) {
                return { ...n, x: cx, y: cy, vx: 0, vy: 0, r, pinned: true, pulse: 0 };
            }
            const angle = (i / Math.max(total - 1, 1)) * Math.PI * 2;
            const dist  = 155 + Math.random() * 55;
            return {
                ...n,
                x:     cx + Math.cos(angle) * dist + (Math.random() - .5) * 28,
                y:     cy + Math.sin(angle) * dist + (Math.random() - .5) * 28,
                vx: 0, vy: 0,
                r,
                pinned: false,
                pulse:  Math.random() * Math.PI * 2
            };
        });
        links = graphLinks || [];
    }

    function destroy() {
        if (animFrame) cancelAnimationFrame(animFrame);
        window.removeEventListener('resize', resize);
        initialized = false;
        nodes = []; links = [];
        hideTip();
    }

    function highlight(nodeId) {
        const n = nodes.find(x => x.id === nodeId);
        if (n) { n.vx += (w / 2 - n.x) * 0.05; n.vy += (h / 2 - n.y) * 0.05; }
    }

    function setIntelligenceMode() { /* compat */ }
    function resize() {
        if (!canvas) return;
        const area = canvas.parentElement;
        w = area ? area.offsetWidth  : 800;
        h = area ? area.offsetHeight : 600;
        canvas.width  = w;
        canvas.height = h;
    }

    // ══════════════════════════════════════════════════════════════
    // RADIUS per shape
    // ══════════════════════════════════════════════════════════════
    function nodeRadius(n) {
        if (n.isCenter) return 32;
        const base = {
            materiel:    20, incident:    18, utilisateur: 22,
            commande:    null, // → rect, radius = half-height
            projet:      21,   demande:    null,
            commentaire: 14,   ia:         18
        };
        const b     = base[n.type] ?? 16;
        const extra = Math.min((n.weight || 1) - 1, 4) * 1.1;
        return b + extra;
    }

    // ══════════════════════════════════════════════════════════════
    // PHYSICS
    // ══════════════════════════════════════════════════════════════
    function tick() {
        if (!nodes.length) return;
        tickCount++;

        const cx    = w / 2, cy = h / 2;
        const REP   = 5200;
        const LDIST = 140;
        const DAMP  = tickCount < 100 ? 0.76 : 0.85;
        const PULL  = tickCount < 80  ? 0.007 : 0.003;

        // Center pull
        nodes.forEach(n => {
            if (n.pinned || n === dragging) return;
            n.vx += (cx - n.x) * PULL;
            n.vy += (cy - n.y) * PULL;
        });

        // Repulsion
        for (let i = 0; i < nodes.length; i++) {
            for (let j = i + 1; j < nodes.length; j++) {
                const a = nodes[i], b = nodes[j];
                const dx = a.x - b.x, dy = a.y - b.y;
                const d  = Math.sqrt(dx * dx + dy * dy) || 0.1;
                const threshold = (a.r + b.r) * 3;
                if (d < threshold * 2.2) {
                    const f  = REP / (d * d);
                    const fx = (dx / d) * f, fy = (dy / d) * f;
                    if (!a.pinned && a !== dragging) { a.vx += fx; a.vy += fy; }
                    if (!b.pinned && b !== dragging) { b.vx -= fx; b.vy -= fy; }
                }
            }
        }

        // Link springs
        links.forEach(l => {
            const a = nodes.find(n => n.id === l.source);
            const b = nodes.find(n => n.id === l.target);
            if (!a || !b) return;
            const dx = b.x - a.x, dy = b.y - a.y;
            const d  = Math.sqrt(dx * dx + dy * dy) || 0.1;
            const diff = d - LDIST;
            const k    = (l.strength || 0.5) * 0.019;
            const fx   = (dx / d) * diff * k, fy = (dy / d) * diff * k;
            if (!a.pinned && a !== dragging) { a.vx += fx; a.vy += fy; }
            if (!b.pinned && b !== dragging) { b.vx -= fx; b.vy -= fy; }
        });

        // Integrate
        const MARGIN = 62;
        nodes.forEach(n => {
            if (n === dragging) return;
            n.vx *= DAMP; n.vy *= DAMP;
            n.x = Math.max(MARGIN, Math.min(w - MARGIN, n.x + n.vx));
            n.y = Math.max(MARGIN, Math.min(h - MARGIN, n.y + n.vy));
            n.pulse += 0.022;
        });
    }

    // ══════════════════════════════════════════════════════════════
    // DRAW
    // ══════════════════════════════════════════════════════════════
    function draw() {
        if (!ctx || !w || !h) return;
        ctx.clearRect(0, 0, w, h);

        // Background
        ctx.fillStyle = '#07090F';
        ctx.fillRect(0, 0, w, h);

        // Radial ambient
        const rg = ctx.createRadialGradient(w/2, h/2, 0, w/2, h/2, Math.min(w,h) * 0.55);
        rg.addColorStop(0, 'rgba(79,142,247,0.05)');
        rg.addColorStop(1, 'transparent');
        ctx.fillStyle = rg;
        ctx.fillRect(0, 0, w, h);

        // Dot grid
        ctx.fillStyle = 'rgba(30,45,64,0.48)';
        for (let gx = 32; gx < w; gx += 32)
            for (let gy = 32; gy < h; gy += 32) {
                ctx.beginPath(); ctx.arc(gx, gy, 0.7, 0, Math.PI * 2); ctx.fill();
            }

        drawLinks();
        // Draw in z-order: normal then hovered on top
        const sorted = [...nodes].sort((a, b) => (a === hoveredNode ? 1 : 0) - (b === hoveredNode ? 1 : 0));
        sorted.forEach(drawNode);
    }

    // ── Links ────────────────────────────────────────────────────
    function drawLinks() {
        links.forEach(l => {
            const a = nodes.find(n => n.id === l.source);
            const b = nodes.find(n => n.id === l.target);
            if (!a || !b) return;

            const hov = hoveredNode && (hoveredNode.id === a.id || hoveredNode.id === b.id);
            const col = C[b.type] || C[a.type] || '#4F8EF7';

            ctx.save();
            ctx.beginPath(); ctx.moveTo(a.x, a.y); ctx.lineTo(b.x, b.y);
            ctx.strokeStyle = hov ? col + '6a' : col + '1e';
            ctx.lineWidth   = hov ? 1.6 : 0.9;
            ctx.setLineDash([]);
            ctx.stroke();

            if (hov && l.label) {
                const mx = (a.x + b.x) / 2, my = (a.y + b.y) / 2;
                ctx.font = '9px "Consolas", monospace';
                ctx.fillStyle = 'rgba(148,163,184,0.72)';
                ctx.textAlign = 'center'; ctx.textBaseline = 'middle';
                ctx.fillText(l.label, mx, my - 9);
            }
            ctx.restore();
        });
    }

    // ── Single node dispatch ─────────────────────────────────────
    function drawNode(n) {
        const col   = C[n.type]    || '#94A3B8';
        const bg    = C_BG[n.type] || '#0a0a0a';
        const glow  = C_GLOW[n.type] || 'transparent';
        const hov   = n === hoveredNode;
        const r     = n.r;
        const ps    = Math.sin(n.pulse) * 1.8;

        ctx.save();

        // Glow halo
        if (hov || n.isCenter || n.status === 'critical' || n.status === 'warning') {
            const gs = r + (hov ? 18 : n.isCenter ? 12 : 9) + ps;
            const g  = ctx.createRadialGradient(n.x, n.y, r * 0.25, n.x, n.y, gs);
            g.addColorStop(0, glow); g.addColorStop(1, 'transparent');
            ctx.beginPath(); ctx.arc(n.x, n.y, gs, 0, Math.PI * 2);
            ctx.fillStyle = g; ctx.fill();
        }

        // Shape routing
        switch (n.type) {
            case 'materiel':
            case 'incident':
                drawHex(n, col, bg, r, hov, ps);
                break;
            case 'projet':
                drawDiamond(n, col, bg, r, hov, ps);
                break;
            case 'commande':
            case 'demande':
                drawRect(n, col, bg, r, hov, ps);
                break;
            case 'utilisateur':
                drawUser(n, col, bg, r, hov, ps);
                break;
            case 'commentaire':
                drawBubble(n, col, bg, r, hov, ps);
                break;
            default:
                drawUser(n, col, bg, r, hov, ps);
        }

        // Warning / critical badge
        if (n.status === 'critical' || n.status === 'warning') {
            const bx = n.x + r * 0.7, by = n.y - r * 0.7;
            ctx.beginPath(); ctx.arc(bx, by, 5.5, 0, Math.PI * 2);
            ctx.fillStyle = '#F59E0B'; ctx.fill();
            ctx.fillStyle = '#1a0800';
            ctx.font = 'bold 7px sans-serif';
            ctx.textAlign = 'center'; ctx.textBaseline = 'middle';
            ctx.fillText('!', bx, by + 0.5);
        }

        ctx.restore();
        drawLabel(n, col, r, ps);
    }

    // ── HEXAGON (materiel, incident, centre) ──────────────────────
    function drawHex(n, col, bg, r, hov, ps) {
        const R = n.isCenter ? r + ps * 0.35 : r;

        if (n.isCenter) {
            // Outer orbit rings
            pathHex(n.x, n.y, R + 11 + ps);
            ctx.strokeStyle = col + '35'; ctx.lineWidth = 0.6;
            ctx.setLineDash([6, 4]); ctx.stroke(); ctx.setLineDash([]);

            pathHex(n.x, n.y, R + 5);
            ctx.strokeStyle = col + '55'; ctx.lineWidth = 0.85; ctx.stroke();
        }

        // Fill
        pathHex(n.x, n.y, R);
        ctx.fillStyle = bg; ctx.fill();

        // Inner tint
        pathHex(n.x, n.y, R - 2);
        ctx.fillStyle = col + (n.isCenter ? '1a' : '12'); ctx.fill();

        // Center accent hex
        if (n.isCenter) {
            pathHex(n.x, n.y, R * 0.46);
            ctx.fillStyle = col + '22'; ctx.fill();
        }

        // Border + shadow
        pathHex(n.x, n.y, R);
        ctx.strokeStyle = hov ? col + 'cc' : col + (n.isCenter ? '95' : '75');
        ctx.lineWidth   = n.isCenter ? 1.8 : 1.2;
        ctx.shadowColor = col; ctx.shadowBlur = hov ? 14 : (n.isCenter ? 10 : 5);
        ctx.stroke(); ctx.shadowBlur = 0;

        // Center icon dots
        if (n.isCenter) {
            [[-6,0],[0,0],[6,0],[-3,5],[3,5]].forEach(([dx, dy]) => {
                ctx.beginPath(); ctx.arc(n.x+dx, n.y+dy, 1.8, 0, Math.PI*2);
                ctx.fillStyle = col + 'bb'; ctx.fill();
            });
        }
    }

    function pathHex(cx, cy, r) {
        ctx.beginPath();
        for (let i = 0; i < 6; i++) {
            const a = Math.PI / 180 * (60 * i - 30);
            const x = cx + r * Math.cos(a), y = cy + r * Math.sin(a);
            i === 0 ? ctx.moveTo(x, y) : ctx.lineTo(x, y);
        }
        ctx.closePath();
    }

    // ── DIAMOND (projet) ─────────────────────────────────────────
    function drawDiamond(n, col, bg, r, hov, ps) {
        const S = r + ps * 0.3;

        // Outer dashed ring
        pathDiamond(n.x, n.y, S + 8);
        ctx.strokeStyle = col + '30'; ctx.lineWidth = 0.65;
        ctx.setLineDash([4, 3]); ctx.stroke(); ctx.setLineDash([]);

        // Fill
        pathDiamond(n.x, n.y, S);
        ctx.fillStyle = bg; ctx.fill();
        pathDiamond(n.x, n.y, S - 2);
        ctx.fillStyle = col + '10'; ctx.fill();

        // Inner diamond
        pathDiamond(n.x, n.y, S * 0.40);
        ctx.fillStyle = col + '1e'; ctx.fill();
        ctx.strokeStyle = col + '55'; ctx.lineWidth = 0.6; ctx.stroke();

        // Border
        pathDiamond(n.x, n.y, S);
        ctx.strokeStyle = hov ? col + 'cc' : col + '78';
        ctx.lineWidth = 1.5;
        ctx.shadowColor = col; ctx.shadowBlur = hov ? 14 : 7;
        ctx.stroke(); ctx.shadowBlur = 0;
    }

    function pathDiamond(cx, cy, r) {
        ctx.beginPath();
        ctx.moveTo(cx, cy - r); ctx.lineTo(cx + r, cy);
        ctx.lineTo(cx, cy + r); ctx.lineTo(cx - r, cy);
        ctx.closePath();
    }

    // ── ROUNDED RECT (commande, demande) ─────────────────────────
    function drawRect(n, col, bg, r, hov, ps) {
        const W  = r * 2.7 + ps * 0.25, H = r * 1.9;
        const rx = 8;
        const x  = n.x - W / 2, y = n.y - H / 2;

        // Fill + inner tint
        rrFill(x, y, W, H, rx, bg);
        rrFill(x + 2, y + 2, W - 4, H - 4, rx - 1, col + '0c');

        // Top accent bar
        rrFill(x, y, W, 4, rx, col + '95');

        // Border
        ctx.beginPath(); rrPath(x, y, W, H, rx);
        ctx.strokeStyle = hov ? col + 'cc' : col + '78';
        ctx.lineWidth = 1.2;
        ctx.shadowColor = col; ctx.shadowBlur = hov ? 14 : 5;
        ctx.stroke(); ctx.shadowBlur = 0;

        // Text inside
        const lbl = (n.label || '').length > 8 ? (n.label || '').slice(0, 8) + '…' : (n.label || '');
        ctx.font = 'bold 8px "DM Sans", sans-serif';
        ctx.fillStyle = col + 'ee'; ctx.textAlign = 'center'; ctx.textBaseline = 'middle';
        ctx.fillText(lbl, n.x, n.y - 3);

        if (n.detail) {
            const sub = n.detail.length > 10 ? n.detail.slice(0, 10) + '…' : n.detail;
            ctx.font = '7px monospace'; ctx.fillStyle = col + '90';
            ctx.fillText(sub, n.x, n.y + 7);
        }
    }

    function rrFill(x, y, w2, h2, r2, style) {
        ctx.beginPath(); rrPath(x, y, w2, h2, r2);
        ctx.fillStyle = style; ctx.fill();
    }

    function rrPath(x, y, ww, hh, r) {
        r = Math.min(r, ww / 2, hh / 2);
        ctx.moveTo(x + r, y); ctx.lineTo(x + ww - r, y);
        ctx.quadraticCurveTo(x + ww, y,    x + ww, y + r);
        ctx.lineTo(x + ww, y + hh - r);
        ctx.quadraticCurveTo(x + ww, y + hh, x + ww - r, y + hh);
        ctx.lineTo(x + r,  y + hh);
        ctx.quadraticCurveTo(x, y + hh, x, y + hh - r);
        ctx.lineTo(x, y + r);
        ctx.quadraticCurveTo(x, y, x + r, y);
        ctx.closePath();
    }

    // ── CIRCLE + AVATAR (utilisateur) ────────────────────────────
    function drawUser(n, col, bg, r, hov, ps) {
        // Outer dashed ring
        ctx.beginPath(); ctx.arc(n.x, n.y, r + 7 + ps * 0.35, 0, Math.PI * 2);
        ctx.strokeStyle = col + '45'; ctx.lineWidth = 0.75;
        ctx.setLineDash([3, 3]); ctx.stroke(); ctx.setLineDash([]);

        // Fill
        ctx.beginPath(); ctx.arc(n.x, n.y, r, 0, Math.PI * 2);
        ctx.fillStyle = bg; ctx.fill();
        ctx.fillStyle = col + '16'; ctx.fill();

        // Border
        ctx.beginPath(); ctx.arc(n.x, n.y, r, 0, Math.PI * 2);
        ctx.strokeStyle = hov ? col + 'cc' : col + '78';
        ctx.lineWidth = 1.2;
        ctx.shadowColor = col; ctx.shadowBlur = hov ? 14 : 5;
        ctx.stroke(); ctx.shadowBlur = 0;

        // Avatar silhouette
        ctx.beginPath(); ctx.arc(n.x, n.y - r * 0.26, r * 0.29, 0, Math.PI * 2);
        ctx.fillStyle = col + 'c0'; ctx.fill();

        ctx.beginPath(); ctx.arc(n.x, n.y + r * 0.55, r * 0.52, Math.PI, 0);
        ctx.fillStyle = col + 'a0'; ctx.fill();
    }

    // ── SPEECH BUBBLE (commentaire) ──────────────────────────────
    function drawBubble(n, col, bg, r, hov, ps) {
        const S = r + ps * 0.18;

        // Circle body
        ctx.beginPath(); ctx.arc(n.x, n.y - 2, S, 0, Math.PI * 2);
        ctx.fillStyle = bg; ctx.fill();
        ctx.fillStyle = col + '14'; ctx.fill();

        // Tail
        ctx.beginPath();
        ctx.moveTo(n.x - 3, n.y + S - 4);
        ctx.lineTo(n.x - 9, n.y + S + 6);
        ctx.lineTo(n.x + 5, n.y + S - 4);
        ctx.closePath();
        ctx.fillStyle = bg; ctx.fill();
        ctx.strokeStyle = hov ? col + 'cc' : col + '65';
        ctx.lineWidth = 0.9; ctx.stroke();

        // Border
        ctx.beginPath(); ctx.arc(n.x, n.y - 2, S, 0, Math.PI * 2);
        ctx.strokeStyle = hov ? col + 'cc' : col + '65';
        ctx.lineWidth = 1.1;
        ctx.shadowColor = col; ctx.shadowBlur = hov ? 12 : 4;
        ctx.stroke(); ctx.shadowBlur = 0;

        // Three dots
        [-4, 0, 4].forEach(dx => {
            ctx.beginPath(); ctx.arc(n.x + dx, n.y - 2, 1.8, 0, Math.PI * 2);
            ctx.fillStyle = col + 'a0'; ctx.fill();
        });
    }

    // ── Label below node ─────────────────────────────────────────
    function drawLabel(n, col, r, ps) {
        // Rect nodes have label drawn inside
        if ((n.type === 'commande' || n.type === 'demande') && !n.isCenter) return;

        const text = (n.label || '').length > 13 ? (n.label || '').slice(0, 13) + '…' : (n.label || '');
        const fs   = Math.max(9, Math.min(11, r * 0.62));
        ctx.font   = `500 ${fs}px "DM Sans", sans-serif`;

        const tw = ctx.measureText(text).width;
        // Diamond nodes need extra offset
        const ly = n.y + r + (n.type === 'projet' ? r + 12 : 13);

        // Pill bg
        ctx.beginPath(); rrPath(n.x - tw / 2 - 6, ly - 10, tw + 12, 14, 4);
        ctx.fillStyle = 'rgba(7,9,15,0.88)'; ctx.fill();

        ctx.fillStyle = col + 'd8';
        ctx.textAlign = 'center'; ctx.textBaseline = 'alphabetic';
        ctx.fillText(text, n.x, ly);
    }

    // ══════════════════════════════════════════════════════════════
    // TOOLTIP
    // ══════════════════════════════════════════════════════════════
    function showTip(e, n) {
        const tip = document.getElementById('node-tip');
        if (!tip || !canvas) return;

        const rect = canvas.getBoundingClientRect();
        tip.style.display = 'block';
        tip.style.left    = (rect.left + n.x) + 'px';
        tip.style.top     = (rect.top  + n.y - n.r - 12) + 'px';

        const tc = document.getElementById('tip-type');
        if (tc) {
            tc.textContent    = TYPE_LABELS[n.type] || n.type;
            tc.style.background = TIP_BG[n.type] || 'rgba(148,163,184,.15)';
            tc.style.color    = C[n.type] || '#94A3B8';
        }
        const tt = document.getElementById('tip-title');
        if (tt) tt.textContent = n.label || '';
        const tb = document.getElementById('tip-body');
        if (tb) tb.textContent = n.detail || '';
    }

    function hideTip() {
        const tip = document.getElementById('node-tip');
        if (tip) tip.style.display = 'none';
    }

    // ══════════════════════════════════════════════════════════════
    // HIT TEST
    // ══════════════════════════════════════════════════════════════
    function nodeAt(clientX, clientY) {
        if (!canvas) return null;
        const rect = canvas.getBoundingClientRect();
        const mx = clientX - rect.left, my = clientY - rect.top;

        for (let i = nodes.length - 1; i >= 0; i--) {
            const n = nodes[i];
            if (n.type === 'commande' || n.type === 'demande') {
                const W = n.r * 2.7, H = n.r * 1.9;
                if (mx >= n.x-W/2-5 && mx <= n.x+W/2+5 &&
                    my >= n.y-H/2-5 && my <= n.y+H/2+5) return n;
            } else if (n.type === 'projet') {
                // Diamond hit test: |dx| + |dy| <= r
                if (Math.abs(mx - n.x) + Math.abs(my - n.y) <= n.r + 8) return n;
            } else {
                const dx = mx - n.x, dy = my - n.y;
                if (dx*dx + dy*dy <= (n.r + 8) * (n.r + 8)) return n;
            }
        }
        return null;
    }

    // ══════════════════════════════════════════════════════════════
    // EVENTS
    // ══════════════════════════════════════════════════════════════
    function attachEvents() {
        if (!canvas) return;
        // Remove previous listeners by cloning
        const fresh = canvas.cloneNode(false);
        canvas.parentNode.replaceChild(fresh, canvas);
        canvas = fresh;
        ctx    = canvas.getContext('2d');

        canvas.addEventListener('mousemove', e => {
            const n = nodeAt(e.clientX, e.clientY);
            hoveredNode = n;
            canvas.style.cursor = n ? 'pointer' : 'default';
            n ? showTip(e, n) : hideTip();

            if (dragging) {
                const rect = canvas.getBoundingClientRect();
                dragging.x  = e.clientX - rect.left + dragOX;
                dragging.y  = e.clientY - rect.top  + dragOY;
                dragging.vx = 0; dragging.vy = 0;
            }
        });

        canvas.addEventListener('mousedown', e => {
            const n = nodeAt(e.clientX, e.clientY);
            if (n) {
                const rect = canvas.getBoundingClientRect();
                dragging = n;
                dragOX   = n.x - (e.clientX - rect.left);
                dragOY   = n.y - (e.clientY - rect.top);
            }
        });

        canvas.addEventListener('mouseup',    () => { dragging = null; });
        canvas.addEventListener('mouseleave', () => { hoveredNode = null; dragging = null; hideTip(); });

        // Touch support
        canvas.addEventListener('touchstart', e => {
            const t = e.touches[0];
            const n = nodeAt(t.clientX, t.clientY);
            if (n) {
                const rect = canvas.getBoundingClientRect();
                dragging = n;
                dragOX   = n.x - (t.clientX - rect.left);
                dragOY   = n.y - (t.clientY - rect.top);
            }
        }, { passive: true });

        canvas.addEventListener('touchmove', e => {
            if (!dragging) return;
            e.preventDefault();
            const t    = e.touches[0];
            const rect = canvas.getBoundingClientRect();
            dragging.x  = t.clientX - rect.left + dragOX;
            dragging.y  = t.clientY - rect.top  + dragOY;
            dragging.vx = 0; dragging.vy = 0;
        }, { passive: false });

        canvas.addEventListener('touchend', () => { dragging = null; });
    }

    // ══════════════════════════════════════════════════════════════
    // LOOP
    // ══════════════════════════════════════════════════════════════
    function loop() {
        tick();
        draw();
        animFrame = requestAnimationFrame(loop);
    }

    // ── Public surface ────────────────────────────────────────────
    return { init, setData, destroy, highlight, setIntelligenceMode, resize };

})();
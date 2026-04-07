// Graphes pour le tableau de bord IT

window.ApexITInterop = (function () {

    const _charts = {};

    function _destroy(id) {
        if (_charts[id]) {
            try { _charts[id].destroy(); } catch (_) {}
            delete _charts[id];
        }
    }

    function _getHeight(id, fallback) {
        const el = document.getElementById(id);
        if (!el) return fallback;
        const h = el.clientHeight || el.offsetHeight;
        return h > 40 ? h : fallback;
    }

    function _p(dark) {
        return {
            bg:    dark ? '#1a232e' : '#ffffff',
            grid:  dark ? '#2d3f55' : '#e2e8f0',
            label: dark ? '#94a3b8' : '#64748b',
            title: dark ? '#e2e8f0' : '#0f172a',
        };
    }

    const C = {
        blue:    '#136dec',
        indigo:  '#6366f1',
        emerald: '#10b981',
        amber:   '#f59e0b',
        rose:    '#f43f5e',
        purple:  '#a855f7',
        cyan:    '#06b6d4',
        orange:  '#f97316',
        teal:    '#14b8a6',
        lime:    '#84cc16',
        violet:  '#8b5cf6',
        pink:    '#ec4899',
    };

    function _base(id, dark, fallbackH) {
        const p = _p(dark);
        const h = _getHeight(id, fallbackH || 260);
        return {
            chart: {
                height:     h,
                background: p.bg,
                foreColor:  p.label,
                fontFamily: "'Segoe UI', system-ui, sans-serif",
                toolbar: {
                    show:  true,
                    tools: { download: true, selection: false, zoom: false,
                             zoomin: false, zoomout: false, pan: false, reset: false },
                },
                animations:           { enabled: true, easing: 'easeinout', speed: 500 },
                parentHeightOffset:   0,
                redrawOnParentResize: true,
                redrawOnWindowResize: true,
            },
            grid: {
                borderColor:    p.grid,
                strokeDashArray:3,
                padding:        { top: 0, right: 8, bottom: 0, left: 8 },
            },
            tooltip:    { theme: dark ? 'dark' : 'light' },
            legend:     { labels: { colors: p.label }, fontSize: '11px' },
            dataLabels: { enabled: false },
            xaxis: {
                labels:     { style: { colors: p.label, fontSize: '11px' } },
                axisBorder: { color: p.grid },
                axisTicks:  { color: p.grid },
            },
            yaxis: {
                labels: { style: { colors: p.label, fontSize: '11px' } },
            },
        };
    }

    function _render(id, options) {
        _destroy(id);
        const el = document.getElementById(id);
        if (!el) return;
        const chart = new ApexCharts(el, options);
        chart.render();
        _charts[id] = chart;
    }

    // ================================================================
    // 1. Incidents par Type — Donut (remplace pie simple)
    // ================================================================
    function renderIncidentsParType(containerId, data, dark) {
        if (!data || !data.length) return;
        const p    = _p(dark);
        const base = _base(containerId, dark, 260);
        const total = data.reduce((a, b) => a + b.count, 0);
        const palette = [C.rose, C.amber, C.blue, C.purple, C.emerald, C.cyan, C.orange];

        _render(containerId, {
            ...base,
            series: data.map(d => d.count),
            chart:  { ...base.chart, type: 'donut' },
            colors: palette.slice(0, data.length),
            labels: data.map(d => d.type),
            plotOptions: {
                pie: {
                    donut: {
                        size: '60%',
                        labels: {
                            show:  true,
                            total: {
                                show:       true,
                                label:      'Total',
                                color:      p.label,
                                fontSize:   '12px',
                                fontWeight: 700,
                                formatter:  () => total,
                            },
                            value: { color: p.title, fontSize: '20px', fontWeight: 800 },
                        }
                    }
                }
            },
            legend:     { position: 'bottom', labels: { colors: p.label }, fontSize: '11px' },
            stroke:     { width: 2, colors: [p.bg] },
            dataLabels: { enabled: false },
            tooltip: {
                theme: dark ? 'dark' : 'light',
                y: { formatter: v => `${v} incident(s)` },
            },
        });
    }

    // ================================================================
    // 2. Évolution incidents par semaine — Area stacked
    // ================================================================
    function renderEvolutionIncidents(containerId, data, dark) {
        if (!data || !data.length) return;
        const p    = _p(dark);
        const base = _base(containerId, dark, 250);

        _render(containerId, {
            ...base,
            series: [
                { name: 'En attente', data: data.map(d => d.enAttente) },
                { name: 'En cours',   data: data.map(d => d.enCours)   },
                { name: 'Résolus',    data: data.map(d => d.resolu)    },
            ],
            chart: { ...base.chart, type: 'area', stacked: true },
            colors: [C.amber, C.blue, C.emerald],
            fill: {
                type:     'gradient',
                gradient: { opacityFrom: 0.55, opacityTo: 0.05, shadeIntensity: 0.3 },
            },
            stroke:  { curve: 'smooth', width: 2 },
            xaxis: {
                categories: data.map(d => d.label),
                labels:     { style: { colors: p.label, fontSize: '11px' } },
                axisBorder: { color: p.grid },
                axisTicks:  { color: p.grid },
            },
            yaxis:   { labels: { style: { colors: p.label } }, min: 0 },
            legend:  { position: 'top', labels: { colors: p.label }, fontSize: '11px' },
            markers: { size: 3 },
            tooltip: { theme: dark ? 'dark' : 'light', shared: true, intersect: false },
        });
    }

    // ================================================================
    // 3. Articles par Statut — Bar horizontal stacked
    // ================================================================
    function renderArticlesParStatut(containerId, data, dark) {
        if (!data) return;
        const p    = _p(dark);
        const base = _base(containerId, dark, 260);
        const total = data.disponible + data.affecte + data.horsService + data.enReparation;

        _render(containerId, {
            ...base,
            series: [
                { name: 'Disponible',    data: [data.disponible]   },
                { name: 'Affecté',       data: [data.affecte]      },
                { name: 'Hors service',  data: [data.horsService]  },
                { name: 'En réparation', data: [data.enReparation] },
            ],
            chart: { ...base.chart, type: 'bar', stacked: true, stackType: '100%' },
            colors: [C.emerald, C.blue, C.rose, C.orange],
            plotOptions: {
                bar: {
                    horizontal:  true,
                    borderRadius:6,
                    barHeight:   '40%',
                }
            },
            xaxis: {
                categories: ['Articles'],
                labels: {
                    style:     { colors: p.label, fontSize: '11px' },
                    formatter: v => `${Math.round(v)}%`,
                },
                axisBorder: { color: p.grid },
                axisTicks:  { color: p.grid },
            },
            yaxis: { labels: { style: { colors: p.label } } },
            legend: { position: 'bottom', labels: { colors: p.label }, fontSize: '11px' },
            fill:   { opacity: 1 },
            dataLabels: {
                enabled:   true,
                formatter: (val, opts) => {
                    const n = opts.w.globals.series[opts.seriesIndex][0];
                    return n > 0 ? `${n}` : '';
                },
                style: { fontSize: '11px', colors: ['#fff'] },
                dropShadow: { enabled: false },
            },
            tooltip: {
                theme: dark ? 'dark' : 'light',
                y: { formatter: v => `${v} article(s)` },
            },
        });
    }

    // ================================================================
    // 4. Affectations par Département — Bar vertical distribué
    // ================================================================
    function renderAffectationsParDept(containerId, data, dark) {
        if (!data || !data.length) return;
        const p    = _p(dark);
        const base = _base(containerId, dark, 260);
        const palette = [C.blue,C.indigo,C.cyan,C.teal,C.emerald,C.lime,C.amber,C.orange,C.rose,C.violet];

        _render(containerId, {
            ...base,
            series: [{ name: 'Affectations', data: data.map(d => d.count) }],
            chart: { ...base.chart, type: 'bar' },
            colors: palette.slice(0, data.length),
            plotOptions: {
                bar: {
                    horizontal:   false,
                    borderRadius: 6,
                    columnWidth:  '55%',
                    distributed:  true,
                }
            },
            xaxis: {
                categories: data.map(d => d.departement),
                labels: {
                    style:     { colors: p.label, fontSize: '10px' },
                    rotate:    -25,
                    formatter: v => v && v.length > 12 ? v.slice(0, 11) + '…' : v,
                },
                axisBorder: { color: p.grid },
                axisTicks:  { color: p.grid },
            },
            yaxis: { labels: { style: { colors: p.label } }, min: 0 },
            legend: { show: false },
            dataLabels: {
                enabled:   true,
                style:     { fontSize: '10px', colors: ['#fff'] },
                formatter: v => v > 0 ? v : '',
                dropShadow: { enabled: false },
            },
            tooltip: {
                theme: dark ? 'dark' : 'light',
                y: { formatter: v => `${v} affectation(s)` },
            },
        });
    }

    // ================================================================
    // 5. Tendance résolution des incidents — Line avec gradient fill
    // ================================================================
    function renderTendanceResolution(containerId, data, dark) {
        if (!data || !data.length) return;
        const p    = _p(dark);
        const base = _base(containerId, dark, 250);

        _render(containerId, {
            ...base,
            series: [{
                name: 'Durée moy. (heures)',
                data: data.map(d => d.moyenneHeures),
            }],
            chart: { ...base.chart, type: 'line' },
            colors: [C.purple],
            stroke: { curve: 'smooth', width: 3 },
            fill: {
                type:     'gradient',
                gradient: {
                    shadeIntensity: 1,
                    type:           'vertical',
                    opacityFrom:    0.4,
                    opacityTo:      0.0,
                    stops:          [0, 90, 100],
                },
            },
            markers: { size: 5, colors: [C.purple], strokeColors: p.bg, strokeWidth: 2 },
            xaxis: {
                categories: data.map(d => d.label),
                labels:     { style: { colors: p.label, fontSize: '11px' } },
                axisBorder: { color: p.grid },
                axisTicks:  { color: p.grid },
            },
            yaxis: {
                labels: { style: { colors: p.label }, formatter: v => `${v}h` },
                min: 0,
            },
            legend: { show: false },
            dataLabels: {
                enabled:   true,
                formatter: v => v > 0 ? `${v}h` : '',
                style:     { fontSize: '10px', colors: [p.title] },
                offsetY:   -6,
                background: { enabled: false },
            },
            tooltip: {
                theme: dark ? 'dark' : 'light',
                y: { formatter: v => `${v} heure(s)` },
            },
        });
    }

    // ================================================================
    // 6. Équipements par Catégorie — Radial Bar (multi-séries)
    // ================================================================
    function renderEquipementsParCategorie(containerId, data, dark) {
        if (!data || !data.length) return;
        const p    = _p(dark);
        const base = _base(containerId, dark, 260);

        // Pourcentage d'affectation par catégorie
        const series = data.map(d => d.total > 0 ? Math.round(100 * d.affectes / d.total) : 0);
        const labels = data.map(d => d.categorie);
        const palette = [C.blue, C.emerald, C.amber, C.rose, C.cyan, C.purple, C.orange, C.teal];

        _render(containerId, {
            ...base,
            series,
            chart: { ...base.chart, type: 'radialBar' },
            colors: palette.slice(0, data.length),
            labels,
            plotOptions: {
                radialBar: {
                    hollow:    { size: '15%', margin: 5 },
                    track:     { background: dark ? '#2d3f55' : '#e2e8f0', margin: 5 },
                    dataLabels:{
                        name:  { fontSize: '10px', offsetY: -10 },
                        value: { fontSize: '11px', fontWeight: 700, formatter: v => `${v}%` },
                        total: {
                            show:      true,
                            label:     'Moy.',
                            formatter: w => {
                                const avg = Math.round(w.globals.seriesTotals.reduce((a, b) => a + b, 0) / w.globals.series.length);
                                return `${avg}%`;
                            },
                        },
                    },
                }
            },
            legend: { position: 'bottom', labels: { colors: p.label }, fontSize: '10px' },
        });
    }

    // ================================================================
    // 7. Statut incidents — Gauge (RadialBar simple)
    // ================================================================
    function renderIncidentStatutGauge(containerId, data, dark) {
        if (!data) return;
        const p    = _p(dark);
        const base = _base(containerId, dark, 260);
        const total = data.enAttente + data.enCours + data.resolu + data.cloture;

        _render(containerId, {
            ...base,
            series: [data.enAttente, data.enCours, data.resolu, data.cloture],
            chart:  { ...base.chart, type: 'donut' },
            colors: [C.amber, C.blue, C.emerald, C.indigo],
            labels: ['En attente', 'En cours', 'Résolu', 'Clôturé'],
            plotOptions: {
                pie: {
                    donut: {
                        size: '65%',
                        labels: {
                            show:  true,
                            total: {
                                show:       true,
                                label:      'Incidents',
                                color:      p.label,
                                fontSize:   '12px',
                                fontWeight: 700,
                                formatter:  () => total,
                            },
                            value: { color: p.title, fontSize: '18px', fontWeight: 800 },
                        }
                    }
                }
            },
            legend:     { position: 'bottom', labels: { colors: p.label }, fontSize: '11px' },
            stroke:     { width: 2, colors: [p.bg] },
            dataLabels: { enabled: false },
            tooltip: {
                theme: dark ? 'dark' : 'light',
                y: { formatter: v => `${v} incident(s)` },
            },
        });
    }
    // ================================================================
    // 8. Heatmap — Activité des incidents par jour (style GitHub)
    // ================================================================
    function renderHeatmap(gridId, monthsId, data, year, dark) {
        console.log("🔥 renderHeatmap appelé", { gridId, year, data, keys: Object.keys(data) });
        const grid     = document.getElementById(gridId);
        const monthsEl = document.getElementById(monthsId);
        console.log("🔥 grid:", grid, "monthsEl:", monthsEl);
        if (!grid || !monthsEl) return;

        grid.innerHTML     = '';
        monthsEl.innerHTML = '';

        const MONTHS_FR = ['Jan','Fév','Mar','Avr','Mai','Jun','Jul','Aoû','Sep','Oct','Nov','Déc'];

        // Trouver le premier lundi ≤ 1er janvier
        const jan1    = new Date(year, 0, 1);
        const dec31   = new Date(year, 11, 31);
        const startDay = new Date(jan1);
        const dow      = startDay.getDay(); // 0 = Dim
        startDay.setDate(startDay.getDate() + (dow === 0 ? -6 : 1 - dow));

        // Max pour les niveaux relatifs
        const vals   = Object.values(data).map(Number);
        const maxVal = vals.length ? Math.max(...vals) : 1;
        const getLevel = count => {
            if (!count)          return 0;
            const r = count / maxVal;
            if (r <= 0.20)       return 1;
            if (r <= 0.50)       return 2;
            if (r <= 0.80)       return 3;
            return 4;
        };

        let monthPositions = {};
        let colIndex = 0;
        let cur = new Date(startDay);

        while (cur <= dec31) {
            const col = document.createElement('div');
            col.className = 'it-heatmap-col';

            for (let d = 0; d < 7; d++) {
                const cell = document.createElement('div');
                cell.className = 'it-heatmap-cell';

                const inYear  = cur.getFullYear() === year;
                const dateKey = [
                    cur.getFullYear(),
                    String(cur.getMonth() + 1).padStart(2, '0'),
                    String(cur.getDate()).padStart(2, '0')
                ].join('-');

                const count   = inYear ? (data[dateKey] || 0) : 0;

                cell.dataset.level = inYear ? getLevel(count) : 0;
                if (!inYear) cell.style.opacity = '0.15';

                if (inYear && count > 0) {
                    const label = cur.toLocaleDateString('fr-FR', { day: 'numeric', month: 'short', year: 'numeric' });
                    cell.title = `${label} — ${count} incident${count > 1 ? 's' : ''}`;
                } else if (inYear) {
                    cell.title = cur.toLocaleDateString('fr-FR', { day: 'numeric', month: 'short' });
                }

                // Enregistrer la colonne du premier jour de chaque mois
                if (d === 0 && inYear) {
                    const m = cur.getMonth();
                    if (!(m in monthPositions)) monthPositions[m] = colIndex;
                }

                col.appendChild(cell);
                cur.setDate(cur.getDate() + 1);
            }

            grid.appendChild(col);
            colIndex++;
        }

        // Labels des mois positionnés dynamiquement
        const CELL_W = 20; // 14px cell + 3px gap
        MONTHS_FR.forEach((name, m) => {
            const pos = monthPositions[m];
            if (pos === undefined) return;
            const nextPos = monthPositions[m + 1] ?? colIndex;
            const span    = document.createElement('span');
            span.className   = 'it-heatmap-month-label';
            span.textContent = name;
            span.style.width = ((nextPos - pos) * CELL_W) + 'px';
            monthsEl.appendChild(span);
        });
    }


    // ── API publique ──────────────────────────────────────────
    return {
        renderIncidentsParType,
        renderEvolutionIncidents,
        renderArticlesParStatut,
        renderAffectationsParDept,
        renderTendanceResolution,
        renderEquipementsParCategorie,
        renderIncidentStatutGauge,
        renderHeatmap,
        destroyChart: _destroy,
        destroyAll:   () => Object.keys(_charts).forEach(_destroy),
    };

})();

// wwwroot/js/scrapingNotify.js

// Générer un son de succès via Web Audio API
function playSuccessSound() {
    try {
        const ctx = new (window.AudioContext || window.webkitAudioContext)();

        const notes = [523, 659, 784, 1047]; // Do Mi Sol Do (accord majeur)
        notes.forEach((freq, i) => {
            const osc  = ctx.createOscillator();
            const gain = ctx.createGain();
            osc.connect(gain);
            gain.connect(ctx.destination);
            osc.type = 'sine';
            osc.frequency.value = freq;
            gain.gain.setValueAtTime(0, ctx.currentTime + i * 0.12);
            gain.gain.linearRampToValueAtTime(0.18, ctx.currentTime + i * 0.12 + 0.02);
            gain.gain.exponentialRampToValueAtTime(0.001, ctx.currentTime + i * 0.12 + 0.35);
            osc.start(ctx.currentTime + i * 0.12);
            osc.stop(ctx.currentTime + i * 0.12 + 0.4);
        });
    } catch (e) { console.warn('Audio non disponible', e); }
}

function playErrorSound() {
    try {
        const ctx = new (window.AudioContext || window.webkitAudioContext)();
        const osc  = ctx.createOscillator();
        const gain = ctx.createGain();
        osc.connect(gain);
        gain.connect(ctx.destination);
        osc.type = 'sawtooth';
        osc.frequency.value = 220;
        gain.gain.setValueAtTime(0.15, ctx.currentTime);
        gain.gain.exponentialRampToValueAtTime(0.001, ctx.currentTime + 0.5);
        osc.start(ctx.currentTime);
        osc.stop(ctx.currentTime + 0.5);
    } catch (e) {}
}

window.scrapingNotifyBrowser = async function(succes, query, count) {
    // 1. Son
    if (succes) playSuccessSound();
    else        playErrorSound();

    // 2. Notification navigateur
    const title = succes
        ? `✅ Recherche terminée — ${count} résultat(s)`
        : `❌ Échec de la recherche`;
    const body = succes
        ? `"${query}" est prêt. Cliquez pour voir les prix.`
        : `Impossible de scraper "${query}".`;

    if (!("Notification" in window)) return;

    if (Notification.permission === "granted") {
        const n = new Notification(title, {
            body,
            icon: "/images/logo.png",
            badge: "/images/logo.png",
            tag: "scraping-result",   // remplace la notif précédente
            requireInteraction: true  // reste affichée jusqu'au clic
        });
        n.onclick = () => {
            window.focus();
            window.location.href = "/achat/web-scraping";
            n.close();
        };
    } else if (Notification.permission !== "denied") {
        const perm = await Notification.requestPermission();
        if (perm === "granted")
            window.scrapingNotifyBrowser(succes, query, count);
    }
};

// Demander la permission au chargement (à appeler depuis Blazor)
window.requestNotificationPermission = async function() {
    if ("Notification" in window && Notification.permission === "default") {
        await Notification.requestPermission();
    }
    return Notification.permission;
};
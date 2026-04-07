// ── Voice Recorder ─────────────────────────────────────────────────────────
// Fichier : wwwroot/js/voiceRecorder.js
// À référencer dans index.html avant </body> :
//   <script src="js/voiceRecorder.js"></script>

window.voiceRecorder = (() => {
    let mediaRecorder = null;
    let chunks        = [];
    let blobBase64    = null;

    return {
        // Démarre l'enregistrement (demande la permission micro si nécessaire)
        start: async function () {
            chunks     = [];
            blobBase64 = null;

            let stream;
            try {
                stream = await navigator.mediaDevices.getUserMedia({ audio: true });
            } catch (err) {
                console.error('Accès microphone refusé :', err);
                throw err;
            }

            // Choisir le codec disponible (webm > ogg > default)
            const mimeType = MediaRecorder.isTypeSupported('audio/webm;codecs=opus')
                ? 'audio/webm;codecs=opus'
                : MediaRecorder.isTypeSupported('audio/webm')
                    ? 'audio/webm'
                    : MediaRecorder.isTypeSupported('audio/ogg;codecs=opus')
                        ? 'audio/ogg;codecs=opus'
                        : '';

            const options = mimeType ? { mimeType } : {};
            mediaRecorder = new MediaRecorder(stream, options);

            mediaRecorder.ondataavailable = e => {
                if (e.data && e.data.size > 0) chunks.push(e.data);
            };

            mediaRecorder.start(100); // timeslice 100ms pour des chunks réguliers
        },

        // Arrête l'enregistrement et retourne le base64
        stop: function () {
            return new Promise((resolve, reject) => {
                if (!mediaRecorder || mediaRecorder.state === 'inactive') {
                    resolve('');
                    return;
                }

                mediaRecorder.onstop = () => {
                    // Déterminer le type MIME réel utilisé
                    const mimeType = mediaRecorder.mimeType || 'audio/webm';
                    const blob = new Blob(chunks, { type: mimeType });

                    // Arrêter toutes les pistes audio
                    mediaRecorder.stream.getTracks().forEach(t => t.stop());

                    // Convertir en base64
                    const reader = new FileReader();
                    reader.onloadend = () => {
                        blobBase64 = reader.result.split(',')[1];
                        resolve(blobBase64);
                    };
                    reader.onerror = reject;
                    reader.readAsDataURL(blob);
                };

                mediaRecorder.stop();
            });
        },

        // Annule et nettoie sans enregistrer
        cancel: function () {
            if (mediaRecorder) {
                if (mediaRecorder.state !== 'inactive') {
                    mediaRecorder.stream.getTracks().forEach(t => t.stop());
                    try { mediaRecorder.stop(); } catch (_) { }
                }
                mediaRecorder = null;
            }
            chunks     = [];
            blobBase64 = null;
        },

        // Retourne le base64 du dernier enregistrement
        getBlob: function () {
            return blobBase64 || '';
        }
    };
})();

// ── Lecture d'un message vocal ───────────────────────────────────────────────
// base64 : la partie base64 du contenu (sans le préfixe [VOICE])
window.playVoiceMessage = function (btn, base64) {
    // Stopper si déjà en lecture sur ce bouton
    if (btn._audio && !btn._audio.paused) {
        btn._audio.pause();
        btn._audio.currentTime = 0;
        btn.innerHTML = iconPlay();
        btn._audio = null;
        return;
    }

    // Détecter le type MIME selon le navigateur
    const mimeType = MediaRecorder.isTypeSupported
        && MediaRecorder.isTypeSupported('audio/webm') ? 'audio/webm' : 'audio/ogg';

    const audio = new Audio(`data:${mimeType};base64,${base64}`);
    btn._audio  = audio;

    // Icône pause pendant la lecture
    btn.innerHTML = iconPause();

    // Animer la waveform pendant la lecture
    const wave = btn.closest('.msg-audio-player')?.querySelector('.msg-audio-wave');
    if (wave) wave.classList.add('playing');

    audio.play().catch(err => {
        console.warn('Lecture audio impossible :', err);
        btn.innerHTML = iconPlay();
        if (wave) wave.classList.remove('playing');
    });

    audio.onended = () => {
        btn.innerHTML = iconPlay();
        btn._audio    = null;
        if (wave) wave.classList.remove('playing');
    };
    audio.onerror = () => {
        btn.innerHTML = iconPlay();
        btn._audio    = null;
        if (wave) wave.classList.remove('playing');
    };
};

function iconPlay() {
    return `<svg viewBox="0 0 24 24"><polygon points="5 3 19 12 5 21 5 3"/></svg>`;
}
function iconPause() {
    return `<svg viewBox="0 0 24 24"><rect x="6" y="4" width="4" height="16" rx="1"/><rect x="14" y="4" width="4" height="16" rx="1"/></svg>`;
}

// ── Scroll to bottom ──────────────────────────────────────────────────────────
window.scrollToBottom = function (id) {
    const el = document.getElementById(id);
    if (el) el.scrollTop = el.scrollHeight;
};

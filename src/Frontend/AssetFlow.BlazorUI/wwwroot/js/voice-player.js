/* ═══════════════════════════════════════════════════════════════
   Lecteur vocal pro — à inclure dans wwwroot/js/voice-player.js
   et référencer dans index.html / _Host.cshtml :
   <script src="js/voice-player.js"></script>
   ═══════════════════════════════════════════════════════════════ */

(function () {
    let currentAudio = null;
    let currentBtn   = null;
    let rafId        = null;

    function stopCurrent() {
        if (currentAudio) {
            currentAudio.pause();
            currentAudio.currentTime = 0;
        }
        if (currentBtn) {
            currentBtn.classList.remove('playing');
            const wave = currentBtn.parentElement?.querySelector('.mai-voice-wave');
            if (wave) {
                wave.querySelectorAll('span').forEach(s => s.classList.remove('played'));
            }
        }
        if (rafId) cancelAnimationFrame(rafId);
        currentAudio = null;
        currentBtn   = null;
        rafId        = null;
    }

    function updateProgress(audio, wave) {
        if (!audio.duration || !isFinite(audio.duration)) return;
        const ratio = audio.currentTime / audio.duration;
        const bars  = wave.querySelectorAll('span');
        const filled = Math.floor(ratio * bars.length);
        bars.forEach((b, i) => b.classList.toggle('played', i < filled));

        // Mise à jour du temps restant
        const timeEl = wave.parentElement?.querySelector('.mai-voice-time');
        if (timeEl) {
            const remaining = Math.max(0, audio.duration - audio.currentTime);
            const m = Math.floor(remaining / 60);
            const s = Math.floor(remaining % 60);
            timeEl.textContent = `${m}:${s.toString().padStart(2,'0')}`;
        }
    }

    window.toggleVoice = function (audioId, btn) {
        const audio = document.getElementById(audioId);
        if (!audio) return;

        const voiceWrap = btn.parentElement;
        const wave      = voiceWrap.querySelector('.mai-voice-wave');

        // Si on clique sur le bouton déjà actif → pause
        if (currentAudio === audio && !audio.paused) {
            stopCurrent();
            return;
        }

        // Stoppe tout autre lecteur en cours
        if (currentAudio && currentAudio !== audio) stopCurrent();

        currentAudio = audio;
        currentBtn   = btn;
        btn.classList.add('playing');

        audio.play().catch(err => {
            console.warn('Lecture vocal impossible:', err);
            stopCurrent();
        });

        const tick = () => {
            if (!audio.paused) {
                updateProgress(audio, wave);
                rafId = requestAnimationFrame(tick);
            }
        };
        rafId = requestAnimationFrame(tick);

        audio.onended = () => stopCurrent();
    };

    // Permet de cliquer sur la waveform pour se positionner
    document.addEventListener('click', (e) => {
        const wave = e.target.closest?.('.mai-voice-wave');
        if (!wave) return;
        const audio = wave.closest('.mai-voice')?.querySelector('audio');
        if (!audio || !audio.duration || !isFinite(audio.duration)) return;
        const rect = wave.getBoundingClientRect();
        const ratio = Math.min(1, Math.max(0, (e.clientX - rect.left) / rect.width));
        audio.currentTime = ratio * audio.duration;
        updateProgress(audio, wave);
    });
})();

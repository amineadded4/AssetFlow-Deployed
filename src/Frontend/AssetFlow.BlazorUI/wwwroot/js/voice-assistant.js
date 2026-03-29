// wwwroot/js/voice-assistant.js
window.VoiceAssistant = (function () {
    let recognition = null;
    let dotNetRef   = null;

    function init(ref) {
        dotNetRef = ref;
        const SR = window.SpeechRecognition || window.webkitSpeechRecognition;
        if (!SR) {
            console.warn("VoiceAssistant: SpeechRecognition non supporté");
            return;
        }
        recognition = new SR();
        recognition.lang            = 'fr-FR';
        recognition.continuous      = false;
        recognition.interimResults  = false;
        recognition.maxAlternatives = 1;

        recognition.onresult = (e) => {
            const transcript = e.results[0][0].transcript;
            dotNetRef.invokeMethodAsync('OnResult', transcript);
        };

        recognition.onerror = (e) => {
            const msg = e.error === 'not-allowed' ? 'Micro non autorisé' : e.error;
            dotNetRef.invokeMethodAsync('OnError', msg);
        };

        recognition.onnomatch = () => {
            dotNetRef.invokeMethodAsync('OnFeedback', "Commande non reconnue.");
        };
    }

    function start() {
        if (recognition) recognition.start();
    }

    function stop() {
        if (recognition) { try { recognition.stop(); } catch(e) {} }
    }

    return { init, start, stop };
})();
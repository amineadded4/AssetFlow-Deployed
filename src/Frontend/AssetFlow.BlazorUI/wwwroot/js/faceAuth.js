// wwwroot/js/faceAuth.js  — script classique (PAS type="module")
let faceLandmarker = null;
let stream = null;
let animationId = null;
let dotnetRef = null;

// ✅ L'import dynamique est DANS la fonction async — pas au top level
window.initMediaPipe = async () => {
    try {
        // Import dynamique à l'intérieur = pas besoin de type="module"
        const mediapipe = await import(
            "https://cdn.jsdelivr.net/npm/@mediapipe/tasks-vision@0.10.14/vision_bundle.mjs"
        );

        const FaceLandmarker = mediapipe.FaceLandmarker;
        const FilesetResolver = mediapipe.FilesetResolver;

        if (!FaceLandmarker || !FilesetResolver) {
            throw new Error("FaceLandmarker ou FilesetResolver introuvable dans le bundle");
        }

        const vision = await FilesetResolver.forVisionTasks(
            "https://cdn.jsdelivr.net/npm/@mediapipe/tasks-vision@0.10.14/wasm"
        );

        faceLandmarker = await FaceLandmarker.createFromOptions(vision, {
            baseOptions: {
                modelAssetPath: "/mediapipe/models/face_landmarker.task",
                delegate: "GPU"
            },
            runningMode: "VIDEO",
            numFaces: 1,
            minFaceDetectionConfidence: 0.5,
            minFacePresenceConfidence: 0.5,
            minTrackingConfidence: 0.5
        });

        console.log("✅ MediaPipe FaceLandmarker initialisé");
        return true;
    } catch (err) {
        console.error("❌ Erreur MediaPipe:", err);
        return false;
    }
};

window.startCamera = async (videoElementId) => {
    try {
        stream = await navigator.mediaDevices.getUserMedia({
            video: { width: 640, height: 480, facingMode: "user" }
        });
        const video = document.getElementById(videoElementId);
        video.srcObject = stream;
        await video.play();
        return true;
    } catch (err) {
        console.error("❌ Erreur caméra:", err);
        return false;
    }
};

window.stopCamera = (videoElementId) => {
    if (animationId) { cancelAnimationFrame(animationId); animationId = null; }
    if (stream) { stream.getTracks().forEach(t => t.stop()); stream = null; }
    const video = document.getElementById(videoElementId);
    if (video) video.srcObject = null;
};

window.captureKeypoints = async (videoElementId) => {
    if (!faceLandmarker) return null;
    const video = document.getElementById(videoElementId);
    if (!video || video.readyState < 2) return null;
    try {
        const results = faceLandmarker.detectForVideo(video, performance.now());
        if (!results.faceLandmarks || results.faceLandmarks.length === 0) return null;
        const keypoints = results.faceLandmarks[0].map(lm => [lm.x, lm.y]);
        return JSON.stringify(keypoints);
    } catch (err) {
        console.error("Erreur capture:", err);
        return null;
    }
};

window.startPreview = (videoElementId, canvasElementId, dotnetHelper) => {
    dotnetRef = dotnetHelper;
    const video = document.getElementById(videoElementId);
    const canvas = document.getElementById(canvasElementId);
    const ctx = canvas.getContext("2d");

    const loop = async () => {
        if (!video || video.paused || video.ended) return;
        canvas.width = video.videoWidth;
        canvas.height = video.videoHeight;
        ctx.save();
        ctx.scale(-1, 1);
        ctx.drawImage(video, -canvas.width, 0, canvas.width, canvas.height);
        ctx.restore();

        if (faceLandmarker && video.readyState >= 2) {
            try {
                const results = faceLandmarker.detectForVideo(video, performance.now());
                const detected = results.faceLandmarks?.length > 0;
                if (detected) drawFaceBox(ctx, results.faceLandmarks[0], canvas.width, canvas.height);
                if (dotnetRef) dotnetRef.invokeMethodAsync("OnFaceDetected", detected);
            } catch (_) {}
        }
        animationId = requestAnimationFrame(loop);
    };
    animationId = requestAnimationFrame(loop);
};

function drawFaceBox(ctx, landmarks, width, height) {
    const xs = landmarks.map(l => (1 - l.x) * width);
    const ys = landmarks.map(l => l.y * height);
    const minX = Math.min(...xs) - 20, maxX = Math.max(...xs) + 20;
    const minY = Math.min(...ys) - 20, maxY = Math.max(...ys) + 20;
    ctx.strokeStyle = "#00ff88";
    ctx.lineWidth = 3;
    ctx.shadowColor = "#00ff88";
    ctx.shadowBlur = 10;
    ctx.strokeRect(minX, minY, maxX - minX, maxY - minY);
    ctx.fillStyle = "#00ff88";
    ctx.font = "bold 16px monospace";
    ctx.fillText("✓ Visage détecté", minX, minY - 8);
}
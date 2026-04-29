// wwwroot/js/audio-recorder.js - Version Web Speech API
let recognition = null;
let isRecordingActive = false;
let transcribedText = "";

window.initAudioRecorder = function() {
    const SpeechRecognition = window.SpeechRecognition || window.webkitSpeechRecognition;
    if (SpeechRecognition) {
        recognition = new SpeechRecognition();
        recognition.lang = 'fr-FR';
        recognition.continuous = false;
        recognition.interimResults = false;
        recognition.maxAlternatives = 1;
        
        recognition.onstart = function() {
            console.log("🎙️ Microphone activé - Parlez maintenant");
            isRecordingActive = true;
        };
        
        recognition.onresult = function(event) {
            const text = event.results[0][0].transcript;
            transcribedText = text;
            console.log("✅ Texte reconnu:", text);
            
            // Remplir le textarea automatiquement
            const textarea = document.querySelector('textarea');
            if (textarea) {
                textarea.value = text;
                textarea.dispatchEvent(new Event('input', { bubbles: true }));
            }
        };
        
        recognition.onerror = function(event) {
            console.error("❌ Erreur:", event.error);
            if (event.error === 'not-allowed') {
                alert("Veuillez autoriser l'accès au microphone");
            }
            isRecordingActive = false;
        };
        
        recognition.onend = function() {
            console.log("🔇 Microphone désactivé");
            isRecordingActive = false;
        };
        
        console.log("✅ API Web Speech prête");
        return true;
    } else {
        console.error("❌ Reconnaissance vocale non supportée par ce navigateur");
        alert("Votre navigateur ne supporte pas la reconnaissance vocale. Utilisez Chrome, Edge ou Safari.");
        return false;
    }
};

window.startRecording = function() {
    if (recognition && !isRecordingActive) {
        try {
            recognition.start();
            return true;
        } catch (e) {
            console.error("Erreur démarrage:", e);
            return false;
        }
    }
    return false;
};

window.stopRecording = function() {
    if (recognition && isRecordingActive) {
        recognition.stop();
        return true;
    }
    return false;
};

window.getTranscribedText = function() {
    const text = transcribedText;
    transcribedText = "";
    return text;
};
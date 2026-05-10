// wwwroot/js/speech-recorder.js
class SpeechRecorder {
    constructor() {
        this.mediaRecorder = null;
        this.audioChunks = [];
        this.isRecording = false;
        this.apiBaseUrl = "http://localhost:5057";
        this.transcribedText = "";
        this.recordingStartTime = null;
    }

    async init() {
        console.log(" SpeechRecorder initialisé");
        return true;
    }

    async startRecording() {
        try {
            // Demander l'accès au microphone
            const stream = await navigator.mediaDevices.getUserMedia({
                audio: {
                    echoCancellation: true,
                    noiseSuppression: true,
                    autoGainControl: true
                }
            });

            this.mediaRecorder = new MediaRecorder(stream);
            this.audioChunks = [];
            this.recordingStartTime = Date.now();

            this.mediaRecorder.ondataavailable = (event) => {
                if (event.data.size > 0) {
                    this.audioChunks.push(event.data);
                }
            };

            this.mediaRecorder.onstop = async () => {
                await this.processAudio();
                stream.getTracks().forEach(track => track.stop());
            };

            this.mediaRecorder.start(100); // Collecter des chunks toutes les 100ms
            this.isRecording = true;

            this.showUIState('recording');
            console.log("🎙️ Enregistrement démarré");
            return true;

        } catch (error) {
            console.error(" Erreur accès microphone:", error);
            this.showError("Impossible d'accéder au microphone. Vérifiez les permissions.");
            return false;
        }
    }

    stopRecording() {
        if (this.mediaRecorder && this.isRecording) {
            this.mediaRecorder.stop();
            this.isRecording = false;
            this.showUIState('processing');
            console.log(" Enregistrement arrêté");
            return true;
        }
        return false;
    }

    async processAudio() {
        try {
            // Créer le blob audio
            const audioBlob = new Blob(this.audioChunks, { type: 'audio/webm' });

            // Convertir en base64
            const base64 = await this.blobToBase64(audioBlob);

            // Envoyer au backend
            const response = await fetch(`${this.apiBaseUrl}/api/speech/transcribe`, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'Authorization': `Bearer ${localStorage.getItem('sympnet_token')}`
                },
                body: JSON.stringify({ audio: base64 })
            });

            if (!response.ok) {
                throw new Error(`HTTP ${response.status}`);
            }

            const result = await response.json();

            if (result.success && result.text) {
                this.transcribedText = result.text;
                this.updateTextarea(result.text);
                console.log(" Texte transcrit:", result.text);
                this.showUIState('success');

                // Notification optionnelle
                this.showNotification("Transcription terminée!", "success");
            } else {
                throw new Error(result.error || "Erreur de transcription");
            }

        } catch (error) {
            console.error("Erreur traitement audio:", error);
            this.showError("Erreur lors de la transcription: " + error.message);
            this.showUIState('error');
        }
    }

    blobToBase64(blob) {
        return new Promise((resolve, reject) => {
            const reader = new FileReader();
            reader.onloadend = () => resolve(reader.result.split(',')[1]);
            reader.onerror = reject;
            reader.readAsDataURL(blob);
        });
    }

    updateTextarea(text) {
        const textarea = document.querySelector('textarea');
        if (textarea) {
            const currentText = textarea.value;
            if (currentText && !currentText.endsWith(text)) {
                textarea.value = currentText + " " + text;
            } else if (!currentText) {
                textarea.value = text;
            }
            textarea.dispatchEvent(new Event('input', { bubbles: true }));
        }
    }

    getTranscribedText() {
        const text = this.transcribedText;
        this.transcribedText = "";
        return text;
    }

    resetText() {
        this.transcribedText = "";
        const textarea = document.querySelector('textarea');
        if (textarea) {
            textarea.value = "";
        }
    }

    showUIState(state) {
        const btn = document.getElementById('btnDictation');
        if (!btn) return;

        switch (state) {
            case 'recording':
                btn.style.animation = 'pulse 1s infinite';
                btn.style.backgroundColor = '#ff4444';
                btn.innerHTML = '<i class="fas fa-microphone-alt"></i>';
                break;
            case 'processing':
                btn.style.backgroundColor = '#f59e0b';
                btn.innerHTML = '<i class="fas fa-spinner fa-pulse"></i>';
                break;
            case 'success':
                btn.style.backgroundColor = '#10b981';
                btn.innerHTML = '<i class="fas fa-check"></i>';
                setTimeout(() => {
                    if (!this.isRecording) {
                        btn.style.backgroundColor = '';
                        btn.innerHTML = '<i class="fas fa-microphone-alt"></i>';
                    }
                }, 1000);
                break;
            case 'error':
                btn.style.backgroundColor = '#ef4444';
                btn.innerHTML = '<i class="fas fa-exclamation"></i>';
                setTimeout(() => {
                    if (!this.isRecording) {
                        btn.style.backgroundColor = '';
                        btn.innerHTML = '<i class="fas fa-microphone-alt"></i>';
                    }
                }, 2000);
                break;
        }
    }

    showNotification(message, type = 'info') {
        const colors = {
            info: '#0D9488',
            success: '#10B981',
            error: '#EF4444'
        };

        const notification = document.createElement('div');
        notification.style.cssText = `
            position: fixed;
            bottom: 20px;
            right: 20px;
            background: ${colors[type]};
            color: white;
            padding: 12px 20px;
            border-radius: 8px;
            z-index: 10000;
            font-size: 14px;
            animation: slideIn 0.3s ease-out;
            cursor: pointer;
        `;
        notification.innerHTML = message;
        notification.onclick = () => notification.remove();
        document.body.appendChild(notification);
        setTimeout(() => notification.remove(), 3000);
    }

    showError(message) {
        this.showNotification(message, 'error');
    }
}

// Créer une instance globale
const speechRecorder = new SpeechRecorder();

// Exposer les fonctions globales
window.initSpeechRecorder = () => speechRecorder.init();
window.startDictation = () => speechRecorder.startRecording();
window.stopDictation = () => speechRecorder.stopRecording();
window.getDictationText = () => speechRecorder.getTranscribedText();
window.resetDictation = () => speechRecorder.resetText();

// Ajouter les styles CSS
const speechStyle = document.createElement('style');
speechStyle.textContent = `
    @keyframes pulse {
        0%, 100% { transform: scale(1); opacity: 1; }
        50% { transform: scale(1.05); opacity: 0.8; }
    }
    
    @keyframes slideIn {
        from {
            transform: translateX(100%);
            opacity: 0;
        }
        to {
            transform: translateX(0);
            opacity: 1;
        }
    }
`;
document.head.appendChild(speechStyle);

console.log(" SpeechRecorder chargé");
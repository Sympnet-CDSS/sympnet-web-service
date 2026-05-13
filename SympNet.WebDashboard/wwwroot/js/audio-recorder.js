// wwwroot/js/audio-recorder.js - Version définitive
let recognition = null;
let isRecordingActive = false;
let transcribedText = "";
let reconnectAttempts = 0;
let forcedStop = false;
let currentDotNetHelper = null;
const MAX_RECONNECT_ATTEMPTS = 3;

// Filtrer les erreurs MetaMask
const originalConsoleError = console.error;
console.error = function(...args) {
    if (args[0] && typeof args[0] === 'string') {
        if (args[0].includes('ObjectMultiplex') || 
            args[0].includes('StreamMiddleware') ||
            args[0].includes('Unknown response id') ||
            args[0].includes('inpage.js')) {
            return;
        }
    }
    originalConsoleError.apply(console, args);
};

// Détection de l'environnement
const isLocalhost = location.hostname === 'localhost' || location.hostname === '127.0.0.1';
const isHttps = location.protocol === 'https:';

if (isLocalhost && !isHttps) {
    console.warn('⚠️ Reconnaissance vocale: HTTPS recommandé pour meilleure stabilité');
}

window.initAudioRecorder = function(helper = null) {
    // On ne stocke plus de helper global unique pour éviter les conflits
    const SpeechRecognition = window.SpeechRecognition || window.webkitSpeechRecognition;
    
    if (!SpeechRecognition) {
        console.error("❌ Reconnaissance vocale non supportée");
        alert("Votre navigateur ne supporte pas la reconnaissance vocale.\n" +
              "Utilisez Chrome, Edge ou Safari.");
        return false;
    }

    try {
        recognition = new SpeechRecognition();
        recognition.lang = 'fr-FR';
        recognition.continuous = true;
        recognition.interimResults = true;
        recognition.maxAlternatives = 1;
        
        // Augmenter le timeout pour éviter les erreurs réseau
        if (recognition.continuousTimeout) {
            recognition.continuousTimeout = 0; // Pas de timeout
        }
        
        recognition.onstart = function() {
            console.log("🎙️ Microphone activé - Parlez maintenant");
            isRecordingActive = true;
            reconnectAttempts = 0;
            forcedStop = false;
            
            // Mettre à jour l'UI
            const btn = document.querySelector('[onclick*="Dictation"]');
            if (btn) btn.classList.add('dictating');
        };
        
        recognition.onresult = function(event) {
            let finalTranscript = '';
            let interimTranscript = '';
            
            for (let i = event.resultIndex; i < event.results.length; i++) {
                const transcript = event.results[i][0].transcript;
                if (event.results[i].isFinal) {
                    finalTranscript += transcript;
                } else {
                    interimTranscript += transcript;
                }
            }
            
            if (finalTranscript || interimTranscript) {
                const currentText = finalTranscript || interimTranscript;
                const textToDisplay = transcribedText + (transcribedText && currentText ? ' ' : '') + currentText;
                
                // DIAGNOSTIC: Lister tous les inputs pour comprendre pourquoi on ne trouve pas
                const allInputs = document.querySelectorAll('input, textarea');
                console.log(`🔍 Recherche d'input parmi ${allInputs.length} éléments...`);
                allInputs.forEach((el, idx) => {
                    console.log(`   [${idx}] Tag:${el.tagName} ID:${el.id} PlaceHolder:${el.placeholder} Type:${el.type}`);
                });

                // Recherche ultra-agressive du champ
                const input = document.getElementById('chatbot-input-field') || 
                              document.getElementById('consultation-symptoms') ||
                              document.querySelector('.chatbot-input input') ||
                              document.querySelector('input[placeholder*="question"]') ||
                              document.querySelector('input[placeholder*="médicale"]') ||
                              document.querySelector('textarea[placeholder*="symptômes"]') ||
                              document.querySelector('.chatbot-window input') ||
                              document.querySelector('textarea');
                              
                if (input) {
                    console.log("🎯 Cible IDENTIFIÉE:", input.tagName, "ID:", input.id);
                    
                    const nativeInputValueSetter = Object.getOwnPropertyDescriptor(window.HTMLInputElement.prototype, "value").set;
                    const nativeTextAreaValueSetter = Object.getOwnPropertyDescriptor(window.HTMLTextAreaElement.prototype, "value").set;
                    
                    if (input.tagName === 'TEXTAREA') {
                        nativeTextAreaValueSetter.call(input, textToDisplay);
                    } else {
                        nativeInputValueSetter.call(input, textToDisplay);
                    }
                    
                    input.dispatchEvent(new Event('input', { bubbles: true }));
                }

                // BACKFALL / SYNC: Envoyer directement au composant qui a lancé l'enregistrement
                if (currentDotNetHelper) {
                    currentDotNetHelper.invokeMethodAsync('UpdateUserInput', textToDisplay);
                }
                
                if (!input && !currentDotNetHelper) {
                    console.warn("❌ ÉCHEC: Aucun champ trouvé et pas de helper actif.");
                }
                
                if (finalTranscript) {
                    transcribedText += (transcribedText ? ' ' : '') + finalTranscript;
                    console.log("✅ Texte final ajouté:", finalTranscript);
                }
            }
        };
        
        recognition.onerror = function(event) {
            console.log(`Erreur reconnaissance: ${event.error}`);
            
            switch(event.error) {
                case 'network':
                    if (!forcedStop) {
                        console.warn(` Erreur réseau - Tentative ${reconnectAttempts + 1}/${MAX_RECONNECT_ATTEMPTS}`);
                        if (reconnectAttempts < MAX_RECONNECT_ATTEMPTS) {
                            reconnectAttempts++;
                            setTimeout(() => {
                                if (!isRecordingActive && !forcedStop) {
                                    window.startRecording();
                                }
                            }, 2000);
                        } else {
                            showNotification("Problème de connexion. Rafraîchissez la page.", "error");
                            reconnectAttempts = 0;
                        }
                    }
                    break;
                    
                case 'not-allowed':
                    showNotification("Veuillez autoriser l'accès au microphone", "warning");
                    isRecordingActive = false;
                    break;
                    
                case 'no-speech':
                    // Ignorer silencieusement - l'utilisateur n'a pas parlé
                    break;
                    
                case 'aborted':
                    if (!forcedStop) {
                        console.log(" Enregistrement interrompu");
                    }
                    isRecordingActive = false;
                    break;
                    
                default:
                    console.log(`Erreur non critique: ${event.error}`);
            }
        };
        
        recognition.onend = function() {
            console.log(" Microphone désactivé");
            isRecordingActive = false;
            
            // Nettoyer l'UI
            const btn = document.querySelector('[onclick*="Dictation"]');
            if (btn) btn.classList.remove('dictating');
            
            // Redémarrer automatiquement si nécessaire
            if (!forcedStop && reconnectAttempts > 0 && reconnectAttempts <= MAX_RECONNECT_ATTEMPTS) {
                console.log(" Redémarrage automatique...");
                setTimeout(() => window.startRecording(currentDotNetHelper), 1000);
            }
        };
        
        console.log(" API Web Speech prête");
        return true;
        
    } catch (error) {
        console.error(" Erreur initialisation:", error);
        return false;
    }
};

window.startRecording = function(helper = null) {
    if (helper) currentDotNetHelper = helper;
    
    if (!recognition) {
        console.error(" API non initialisée");
        return false;
    }
    
    if (isRecordingActive) {
        console.log(" Enregistrement déjà actif");
        return true;
    }
    
    try {
        forcedStop = false;
        reconnectAttempts = 0;
        
        // Demander d'abord la permission microphone
        navigator.mediaDevices.getUserMedia({ audio: true })
            .then(stream => {
                stream.getTracks().forEach(track => track.stop());
                recognition.start();
            })
            .catch(err => {
                console.error("Erreur permission microphone:", err);
                showNotification("Impossible d'accéder au microphone", "error");
            });
        
        return true;
    } catch (e) {
        console.error("Erreur démarrage:", e);
        
        if (e.name === 'InvalidStateError') {
            console.log("État invalide - Redémarrage...");
            try {
                recognition.stop();
                setTimeout(() => {
                    if (!isRecordingActive) {
                        recognition.start();
                    }
                }, 100);
            } catch (retryError) {
                console.error("Échec redémarrage:", retryError);
            }
        }
        return false;
    }
};

window.stopRecording = function(keepText = false) {
    forcedStop = true;
    
    if (recognition && isRecordingActive) {
        try {
            recognition.stop();
            reconnectAttempts = 0;
            
            if (!keepText) {
                transcribedText = "";
            }
            
            return true;
        } catch (e) {
            console.error("Erreur arrêt:", e);
            return false;
        }
    }
    return false;
};

window.getTranscribedText = function() {
    const text = transcribedText;
    transcribedText = "";
    
    // Nettoyer l'input
    const input = document.getElementById('chatbot-input-field') || document.querySelector('textarea');
    if (input && !input.value) {
        input.value = "";
    }
    
    return text;
};

window.resetTranscribedText = function() {
    transcribedText = "";
    const input = document.getElementById('chatbot-input-field') || 
                  document.getElementById('consultation-symptoms') ||
                  document.querySelector('input[placeholder*="question"]') ||
                  document.querySelector('input[placeholder*="médicale"]') ||
                  document.querySelector('.chatbot-input input') ||
                  document.querySelector('textarea');
    if (input) {
        input.value = "";
        input.dispatchEvent(new Event('input', { bubbles: true }));
    }
    console.log("🗑️ Texte réinitialisé");
    return true;
};

window.isMicrophoneActive = function() {
    return isRecordingActive;
};

// Fonction utilitaire pour les notifications
function showNotification(message, type = 'info') {
    const colors = {
        info: '#0D9488',
        warning: '#F59E0B',
        error: '#EF4444',
        success: '#10B981'
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
        box-shadow: 0 4px 12px rgba(0,0,0,0.15);
        cursor: pointer;
    `;
    notification.innerHTML = message;
    notification.onclick = () => notification.remove();
    document.body.appendChild(notification);
    
    setTimeout(() => {
        if (notification && notification.remove) {
            notification.style.animation = 'slideOut 0.3s ease-out';
            setTimeout(() => notification.remove(), 300);
        }
    }, 5000);
}

// Ajouter les animations CSS
const style = document.createElement('style');
style.textContent = `
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
    
    @keyframes slideOut {
        from {
            transform: translateX(0);
            opacity: 1;
        }
        to {
            transform: translateX(100%);
            opacity: 0;
        }
    }
    
    .dictating {
        animation: pulse 1s infinite;
        background-color: #ff4444 !important;
    }
    
    @keyframes pulse {
        0%, 100% { transform: scale(1); opacity: 1; }
        50% { transform: scale(1.05); opacity: 0.8; }
    }
`;
document.head.appendChild(style);

// Auto-reconnexion
window.addEventListener('online', () => {
    console.log(" Connexion rétablie");
    showNotification("Connexion rétablie", "success");
    if (!isRecordingActive && reconnectAttempts > 0) {
        console.log(" Reconnexion automatique...");
        setTimeout(() => window.startRecording(), 1000);
    }
});

window.addEventListener('offline', () => {
    console.log("📡 Connexion perdue");
    showNotification("Connexion perdue", "warning");
});
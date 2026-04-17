let mediaRecorder;
let audioChunks = [];
let isCurrentlyRecording = false;

function initAudioRecorder() {
    console.log("Audio recorder initialized");
}

async function startRecording() {
    audioChunks = [];
    try {
        const stream = await navigator.mediaDevices.getUserMedia({ audio: true });
        mediaRecorder = new MediaRecorder(stream);
        
        mediaRecorder.ondataavailable = event => {
            if (event.data.size > 0) {
                audioChunks.push(event.data);
            }
        };
        
        mediaRecorder.onstop = async () => {
            const audioBlob = new Blob(audioChunks, { type: 'audio/wav' });
            const formData = new FormData();
            formData.append('audio', audioBlob, 'recording.wav');
            
            try {
                const response = await fetch('http://localhost:8000/voice-to-text', {
                    method: 'POST',
                    body: formData
                });
                const result = await response.json();
                if (result.success && result.text) {
                    window.transcribedText = result.text;
                    console.log("Transcribed:", result.text);
                } else {
                    window.transcribedText = "";
                }
            } catch (error) {
                console.error('Error sending audio:', error);
                window.transcribedText = "";
            }
            
            // Stop all tracks
            stream.getTracks().forEach(track => track.stop());
            isCurrentlyRecording = false;
        };
        
        mediaRecorder.start();
        isCurrentlyRecording = true;
        console.log("Recording started");
    } catch (error) {
        console.error('Error accessing microphone:', error);
        alert("Impossible d'accéder au microphone. Vérifiez les permissions.");
    }
}

function stopRecording() {
    if (mediaRecorder && mediaRecorder.state === 'recording') {
        mediaRecorder.stop();
        console.log("Recording stopped");
    }
    isCurrentlyRecording = false;
}

function getTranscribedText() {
    const text = window.transcribedText || '';
    window.transcribedText = '';
    return text;
}

function isRecording() {
    return isCurrentlyRecording;
}
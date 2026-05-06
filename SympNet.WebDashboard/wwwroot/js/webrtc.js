window.webrtc = {
    localStream: null,
    peerConnection: null,
    targetUserId: null,
    hubConnection: null,
    dotNetHelper: null,
    configuration: {
        iceServers: [
            { urls: 'stun:stun.l.google.com:19302' },
            { urls: 'stun:stun1.l.google.com:19302' },
            { urls: 'stun:stun2.l.google.com:19302' },
            {
                urls: 'turn:openrelay.metered.ca:80',
                username: 'openrelayproject',
                credential: 'openrelayproject'
            },
            {
                urls: 'turn:openrelay.metered.ca:443',
                username: 'openrelayproject',
                credential: 'openrelayproject'
            }
        ]
    },

    init: async function(dotNetHelper, hubConnection) {
        this.dotNetHelper = dotNetHelper;
        this.hubConnection = hubConnection;
        await this.startLocalStream();
        this.setupEventListeners();
        console.log("WebRTC initialisé");
    },

    startLocalStream: async function() {
        try {
            this.localStream = await navigator.mediaDevices.getUserMedia({ 
                video: { width: { ideal: 1280 }, height: { ideal: 720 } }, 
                audio: true 
            });
            const localVideo = document.getElementById('localVideo');
            if (localVideo) {
                localVideo.srcObject = this.localStream;
            }
            return this.localStream;
        } catch (e) {
            console.error("Erreur accès caméra/micro:", e);
            alert("Impossible d'accéder à la caméra ou au microphone");
        }
    },

    setupEventListeners: function() {
        // Les événements sont déjà gérés par les boutons dans le composant
    },

    startCall: async function(targetUserId) {
        this.targetUserId = targetUserId;
        
        this.peerConnection = new RTCPeerConnection(this.configuration);
        
        if (this.localStream) {
            this.localStream.getTracks().forEach(track => {
                this.peerConnection.addTrack(track, this.localStream);
            });
        }
        
        this.peerConnection.ontrack = (event) => {
            const remoteVideo = document.getElementById('remoteVideo');
            if (remoteVideo && event.streams[0]) {
                remoteVideo.srcObject = event.streams[0];
                console.log("Remote stream reçu");
            }
        };
        
        this.peerConnection.onicecandidate = (event) => {
            if (event.candidate) {
                this.dotNetHelper.invokeMethodAsync('SendIceCandidate', 
                    this.targetUserId, 
                    JSON.stringify(event.candidate), 
                    event.candidate.sdpMid, 
                    event.candidate.sdpMLineIndex);
            }
        };
        
        this.peerConnection.oniceconnectionstatechange = () => {
            console.log("ICE connection state:", this.peerConnection.iceConnectionState);
            if (this.peerConnection.iceConnectionState === 'connected') {
                console.log("Appel connecté!");
            }
        };
        
        const offer = await this.peerConnection.createOffer();
        await this.peerConnection.setLocalDescription(offer);
        await this.dotNetHelper.invokeMethodAsync('SendOffer', this.targetUserId, JSON.stringify(offer));
    },

    handleOffer: async function(fromUserId, sdp) {
        this.targetUserId = fromUserId;
        
        if (!this.localStream) {
            await this.startLocalStream();
        }
        
        this.peerConnection = new RTCPeerConnection(this.configuration);
        
        if (this.localStream) {
            this.localStream.getTracks().forEach(track => {
                this.peerConnection.addTrack(track, this.localStream);
            });
        }
        
        this.peerConnection.ontrack = (event) => {
            const remoteVideo = document.getElementById('remoteVideo');
            if (remoteVideo && event.streams[0]) {
                remoteVideo.srcObject = event.streams[0];
            }
        };
        
        this.peerConnection.onicecandidate = (event) => {
            if (event.candidate) {
                this.dotNetHelper.invokeMethodAsync('SendIceCandidate', 
                    this.targetUserId, 
                    JSON.stringify(event.candidate), 
                    event.candidate.sdpMid, 
                    event.candidate.sdpMLineIndex);
            }
        };
        
        await this.peerConnection.setRemoteDescription(JSON.parse(sdp));
        const answer = await this.peerConnection.createAnswer();
        await this.peerConnection.setLocalDescription(answer);
        await this.dotNetHelper.invokeMethodAsync('SendAnswer', this.targetUserId, JSON.stringify(answer));
    },

    handleAnswer: async function(fromUserId, sdp) {
        if (this.peerConnection) {
            await this.peerConnection.setRemoteDescription(JSON.parse(sdp));
        }
    },

    handleIceCandidate: async function(fromUserId, candidate, sdpMid, sdpMLineIndex) {
        if (this.peerConnection) {
            try {
                const iceCandidate = new RTCIceCandidate(JSON.parse(candidate));
                await this.peerConnection.addIceCandidate(iceCandidate);
            } catch (e) {
                console.error("Erreur ajout ICE candidate:", e);
            }
        }
    },

    toggleMicrophone: function() {
        if (this.localStream) {
            const audioTrack = this.localStream.getAudioTracks()[0];
            if (audioTrack) {
                audioTrack.enabled = !audioTrack.enabled;
                const btn = document.getElementById('btnMic');
                if (btn) {
                    if (audioTrack.enabled) {
                        btn.innerHTML = '<i class="fas fa-microphone-alt"></i>';
                        btn.classList.add('active');
                    } else {
                        btn.innerHTML = '<i class="fas fa-microphone-slash"></i>';
                        btn.classList.remove('active');
                    }
                }
            }
        }
    },

    toggleVideo: function() {
        if (this.localStream) {
            const videoTrack = this.localStream.getVideoTracks()[0];
            if (videoTrack) {
                videoTrack.enabled = !videoTrack.enabled;
                const btn = document.getElementById('btnCamera');
                if (btn) {
                    if (videoTrack.enabled) {
                        btn.innerHTML = '<i class="fas fa-video"></i>';
                        btn.classList.add('active');
                    } else {
                        btn.innerHTML = '<i class="fas fa-video-slash"></i>';
                        btn.classList.remove('active');
                    }
                }
            }
        }
    },

    toggleScreenShare: async function() {
        try {
            const screenStream = await navigator.mediaDevices.getDisplayMedia({ video: true });
            if (this.peerConnection && screenStream) {
                const videoTrack = screenStream.getVideoTracks()[0];
                const sender = this.peerConnection.getSenders().find(s => s.track?.kind === 'video');
                if (sender) {
                    await sender.replaceTrack(videoTrack);
                }
                videoTrack.onended = () => {
                    // Restaurer la caméra quand le partage s'arrête
                    if (this.localStream) {
                        const cameraTrack = this.localStream.getVideoTracks()[0];
                        if (cameraTrack && sender) {
                            sender.replaceTrack(cameraTrack);
                        }
                    }
                };
            }
            const btn = document.getElementById('btnScreen');
            if (btn) {
                btn.style.background = '#0D9488';
                setTimeout(() => { btn.style.background = ''; }, 500);
            }
        } catch (e) {
            console.log("Partage d'écran annulé");
        }
    },

    endCall: function() {
        if (this.peerConnection) {
            this.peerConnection.close();
            this.peerConnection = null;
        }
        if (this.localStream) {
            this.localStream.getTracks().forEach(track => track.stop());
        }
        const remoteVideo = document.getElementById('remoteVideo');
        if (remoteVideo) {
            remoteVideo.srcObject = null;
        }
    },

    call: function(targetUserId) {
        this.startCall(targetUserId);
    },

    hangup: function() {
        this.endCall();
    }
};
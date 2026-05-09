window.webrtc = {
    localStream: null,
    peerConnection: null,
    targetUserId: null,
    hubConnection: null,
    dotNetHelper: null,
    configuration: {
        iceServers: [
            { urls: 'stun:stun.l.google.com:19302' },
            { urls: 'stun:stun1.l.google.com:19302' }
        ]
    },

    init: async function (dotNetHelper, hubConnection) {
        this.dotNetHelper = dotNetHelper;
        this.hubConnection = hubConnection;
        await this.startLocalStream();
        this.setupEventListeners();
    },

    startLocalStream: async function () {
        try {
            this.localStream = await navigator.mediaDevices.getUserMedia({ video: true, audio: true });
            const localVideo = document.getElementById('localVideo');
            if (localVideo) localVideo.srcObject = this.localStream;
            return this.localStream;
        } catch (e) {
            console.error("Error accessing media devices:", e);
        }
    },

    setupEventListeners: function () {
        const btnMic = document.getElementById('btnMic');
        const btnCamera = document.getElementById('btnCamera');

        if (btnMic) {
            btnMic.onclick = () => this.toggleMicrophone();
        }
        if (btnCamera) {
            btnCamera.onclick = () => this.toggleVideo();
        }
    },

    startCall: async function (targetUserId) {
        this.targetUserId = targetUserId;

        this.peerConnection = new RTCPeerConnection(this.configuration);

        this.localStream.getTracks().forEach(track => {
            this.peerConnection.addTrack(track, this.localStream);
        });

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

        const offer = await this.peerConnection.createOffer();
        await this.peerConnection.setLocalDescription(offer);
        await this.dotNetHelper.invokeMethodAsync('SendOffer', this.targetUserId, JSON.stringify(offer));
    },

    handleOffer: async function (fromUserId, sdp) {
        this.targetUserId = fromUserId;

        this.peerConnection = new RTCPeerConnection(this.configuration);

        this.localStream.getTracks().forEach(track => {
            this.peerConnection.addTrack(track, this.localStream);
        });

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

    handleAnswer: async function (fromUserId, sdp) {
        await this.peerConnection.setRemoteDescription(JSON.parse(sdp));
    },

    handleIceCandidate: async function (fromUserId, candidate, sdpMid, sdpMLineIndex) {
        const iceCandidate = new RTCIceCandidate(JSON.parse(candidate));
        await this.peerConnection.addIceCandidate(iceCandidate);
    },

    toggleMicrophone: function () {
        if (this.localStream) {
            const audioTrack = this.localStream.getAudioTracks()[0];
            if (audioTrack) {
                audioTrack.enabled = !audioTrack.enabled;
                const btn = document.getElementById('btnMic');
                if (btn) {
                    btn.innerHTML = audioTrack.enabled ? '<i class="fas fa-microphone-alt"></i>' : '<i class="fas fa-microphone-slash"></i>';
                    if (!audioTrack.enabled) btn.classList.remove('active');
                    else btn.classList.add('active');
                }
            }
        }
    },

    toggleVideo: function () {
        if (this.localStream) {
            const videoTrack = this.localStream.getVideoTracks()[0];
            if (videoTrack) {
                videoTrack.enabled = !videoTrack.enabled;
                const btn = document.getElementById('btnCamera');
                if (btn) {
                    btn.innerHTML = videoTrack.enabled ? '<i class="fas fa-video"></i>' : '<i class="fas fa-video-slash"></i>';
                    if (!videoTrack.enabled) btn.classList.remove('active');
                    else btn.classList.add('active');
                }
            }
        }
    },

    toggleScreenShare: async function () {
        try {
            const screenStream = await navigator.mediaDevices.getDisplayMedia({ video: true });
            if (this.peerConnection) {
                const videoTrack = screenStream.getVideoTracks()[0];
                const sender = this.peerConnection.getSenders().find(s => s.track?.kind === 'video');
                if (sender) await sender.replaceTrack(videoTrack);
            }
            const btn = document.getElementById('btnScreen');
            if (btn) {
                btn.style.background = '#0D9488';
                setTimeout(() => {
                    btn.style.background = '';
                }, 1000);
            }
        } catch (e) {
            console.log("Screen share cancelled");
        }
    },

    endCall: function () {
        if (this.peerConnection) this.peerConnection.close();
        if (this.localStream) this.localStream.getTracks().forEach(track => track.stop());
        if (this.hubConnection && this.targetUserId) {
            this.hubConnection.invoke('EndCall');
        }
    }
};
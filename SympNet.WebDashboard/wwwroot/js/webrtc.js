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
        console.log("[WebRTC] Initializing...");
        this.dotNetHelper = dotNetHelper;
        this.hubConnection = hubConnection;
        await this.startLocalStream();
        this.setupEventListeners();
    },

    startLocalStream: async function () {
        try {
            console.log("[WebRTC] Requesting camera/mic...");
            this.localStream = await navigator.mediaDevices.getUserMedia({ video: true, audio: true });
            const localVideo = document.getElementById('localVideo');
            if (localVideo) {
                localVideo.srcObject = this.localStream;
                console.log("[WebRTC] Local stream attached to UI");
            }
            return this.localStream;
        } catch (e) {
            console.error("[WebRTC] Error accessing media devices:", e);
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
                console.log("[WebRTC] New ICE Candidate found");
                this.dotNetHelper.invokeMethodAsync('SendIceCandidate',
                    this.targetUserId,
                    event.candidate.candidate,
                    event.candidate.sdpMid,
                    event.candidate.sdpMLineIndex);
            }
        };

        console.log("[WebRTC] Creating offer for " + targetUserId);
        const offer = await this.peerConnection.createOffer();
        await this.peerConnection.setLocalDescription(offer);
        await this.dotNetHelper.invokeMethodAsync('SendOffer', this.targetUserId, offer.sdp);
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
                    event.candidate.candidate,
                    event.candidate.sdpMid,
                    event.candidate.sdpMLineIndex);
            }
        };

        await this.peerConnection.setRemoteDescription({ type: 'offer', sdp: sdp });
        const answer = await this.peerConnection.createAnswer();
        await this.peerConnection.setLocalDescription(answer);
        await this.dotNetHelper.invokeMethodAsync('SendAnswer', this.targetUserId, answer.sdp);
    },

    handleAnswer: async function (fromUserId, sdp) {
        console.log("[WebRTC] Answer received from " + fromUserId);
        await this.peerConnection.setRemoteDescription({ type: 'answer', sdp: sdp });
    },

    handleIceCandidate: async function (fromUserId, candidate, sdpMid, sdpMLineIndex) {
        console.log("[WebRTC] Remote ICE Candidate received");
        const iceCandidate = new RTCIceCandidate({
            candidate: candidate,
            sdpMid: sdpMid,
            sdpMLineIndex: sdpMLineIndex
        });
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
    }
};
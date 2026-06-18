window.PingMeWebRTC = (function () {
    let peerConnection = null;
    let localStream = null;
    let dotNetHelper = null;
    let localVideoEl = null;
    let remoteVideoEl = null;

    const configuration = {
        iceServers: [
            { urls: 'stun:stun.l.google.com:19302' },
            { urls: 'stun:stun1.l.google.com:19302' }
        ]
    };

    function cleanup() {
        if (peerConnection) {
            peerConnection.close();
            peerConnection = null;
        }
        if (localStream) {
            localStream.getTracks().forEach(track => track.stop());
            localStream = null;
        }
        if (localVideoEl) localVideoEl.srcObject = null;
        if (remoteVideoEl) remoteVideoEl.srcObject = null;
    }

    async function initMedia(isVideo, localId, remoteId) {
        cleanup();
        localVideoEl = document.getElementById(localId);
        remoteVideoEl = document.getElementById(remoteId);

        try {
            localStream = await navigator.mediaDevices.getUserMedia({
                video: isVideo,
                audio: true
            });
            if (localVideoEl) {
                localVideoEl.srcObject = localStream;
            }
            return true;
        } catch (e) {
            console.error("Error accessing media devices.", e);
            return false;
        }
    }

    function createPeerConnection() {
        peerConnection = new RTCPeerConnection(configuration);

        // Add local stream tracks to PC
        if (localStream) {
            localStream.getTracks().forEach(track => {
                peerConnection.addTrack(track, localStream);
            });
        }

        // Handle remote tracks
        peerConnection.ontrack = (event) => {
            if (remoteVideoEl) {
                if (remoteVideoEl.srcObject !== event.streams[0]) {
                    remoteVideoEl.srcObject = event.streams[0];
                }
            }
        };

        // Handle ICE candidates
        peerConnection.onicecandidate = (event) => {
            if (event.candidate && dotNetHelper) {
                dotNetHelper.invokeMethodAsync('OnIceCandidateGenerated', JSON.stringify(event.candidate));
            }
        };
        
        peerConnection.onconnectionstatechange = () => {
            console.log("WebRTC state: " + peerConnection.connectionState);
            if (peerConnection.connectionState === 'disconnected' || peerConnection.connectionState === 'failed') {
                if (dotNetHelper) {
                    dotNetHelper.invokeMethodAsync('OnConnectionFailed');
                }
            }
        };
    }

    return {
        initialize: function (helper) {
            dotNetHelper = helper;
        },

        startCall: async function (isVideo, localId, remoteId) {
            const success = await initMedia(isVideo, localId, remoteId);
            if (!success) return false;

            createPeerConnection();
            
            try {
                const offer = await peerConnection.createOffer();
                await peerConnection.setLocalDescription(offer);
                if (dotNetHelper) {
                    await dotNetHelper.invokeMethodAsync('OnOfferGenerated', JSON.stringify(offer));
                }
                return true;
            } catch (e) {
                console.error("Error creating offer", e);
                return false;
            }
        },

        answerCall: async function (isVideo, localId, remoteId) {
            const success = await initMedia(isVideo, localId, remoteId);
            if (!success) return false;

            createPeerConnection();
            return true;
        },

        handleOffer: async function (offerStr) {
            if (!peerConnection) createPeerConnection();
            try {
                const offer = JSON.parse(offerStr);
                await peerConnection.setRemoteDescription(new RTCSessionDescription(offer));
                const answer = await peerConnection.createAnswer();
                await peerConnection.setLocalDescription(answer);
                if (dotNetHelper) {
                    await dotNetHelper.invokeMethodAsync('OnAnswerGenerated', JSON.stringify(answer));
                }
            } catch (e) {
                console.error("Error handling offer", e);
            }
        },

        handleAnswer: async function (answerStr) {
            try {
                const answer = JSON.parse(answerStr);
                await peerConnection.setRemoteDescription(new RTCSessionDescription(answer));
            } catch (e) {
                console.error("Error handling answer", e);
            }
        },

        handleIceCandidate: async function (candidateStr) {
            try {
                const candidate = JSON.parse(candidateStr);
                if (peerConnection) {
                    await peerConnection.addIceCandidate(new RTCIceCandidate(candidate));
                }
            } catch (e) {
                console.error("Error handling ICE candidate", e);
            }
        },

        toggleMute: function (isMuted) {
            if (localStream) {
                localStream.getAudioTracks().forEach(track => track.enabled = !isMuted);
            }
        },

        toggleVideo: function (isVideoOff) {
            if (localStream) {
                localStream.getVideoTracks().forEach(track => track.enabled = !isVideoOff);
            }
        },

        endCall: function () {
            cleanup();
        }
    };
})();

const pmWebRTC = (function () {
    let peerConnection = null;
    let localStream = null;
    let signalRConn = null;
    let currentPartnerId = null;

    let isVideoCall = false;
    let currentPartnerAvatar = '';
    let isMuted = false;
    let isVideoOff = false;
    let callState = 'Idle'; // Idle, IncomingRinging, OutgoingRinging, InCall
    let callStartTime = null;
    let timerInt = null;

    const configuration = {
        iceServers: [
            { urls: 'stun:stun.l.google.com:19302' },
            { urls: 'stun:stun1.l.google.com:19302' }
        ]
    };

    function $(id) { return document.getElementById(id); }

    function updateOverlay() {
        const overlay = $('pm-call-overlay');
        if (!overlay) return;

        if (callState === 'Idle') {
            overlay.style.display = 'none';
            return;
        }

        overlay.style.display = 'flex';
        
        const title = $('pm-call-title');
        const icon = overlay.querySelector('.pm-call-icon');
        const dur = $('pm-call-duration');
        const body = $('pm-call-body');
        const videos = $('pm-call-videos');
        const avatarWrap = $('pm-call-avatar-wrap');
        const controls = $('pm-call-controls');

        // Body state
        if (isVideoCall) body.classList.remove('no-video');
        else body.classList.add('no-video');

        if (isVideoCall && callState === 'InCall') {
            videos.style.display = '';
            avatarWrap.style.display = 'none';
        } else {
            videos.style.display = 'none';
            avatarWrap.style.display = 'flex';
        }

        // Icon
        icon.innerHTML = isVideoCall 
            ? `<svg viewBox="0 0 24 24"><path d="M17 10.5V7c0-.55-.45-1-1-1H4c-.55 0-1 .45-1 1v10c0 .55.45 1 1 1h12c.55 0 1-.45 1-1v-3.5l4 4v-11l-4 4z"/></svg>`
            : `<svg viewBox="0 0 24 24"><path d="M20.01 15.38c-1.23 0-2.42-.2-3.53-.56-.35-.12-.74-.03-1.01.24l-1.57 1.97c-2.83-1.35-5.48-3.9-6.89-6.83l1.95-1.66c.27-.28.35-.67.24-1.02-.37-1.11-.56-2.3-.56-3.53 0-.54-.45-.99-.99-.99H4.19C3.65 3 3 3.24 3 3.99 3 13.28 10.73 21 20.01 21c.71 0 .99-.63.99-1.18v-3.45c0-.54-.45-.99-.99-.99z"/></svg>`;

        // Title
        let pName = pmEscape(window.pmChat?.getPartnerName?.(currentPartnerId) || 'User');
        if (callState === 'IncomingRinging') title.textContent = 'Cuộc gọi đến từ ' + pName + '...';
        else if (callState === 'OutgoingRinging') title.textContent = 'Đang gọi ' + pName + '...';
        else title.textContent = 'Đang trong cuộc gọi với ' + pName;

        // Avatar (initial + optional image)
        const initEl = $('pm-call-avatar-initial');
        const imgEl  = $('pm-call-avatar-img');
        if (initEl) initEl.textContent = (pName || '?').trim().charAt(0).toUpperCase() || '?';
        const avatarUrl = currentPartnerAvatar || (window.pmChat?.getPartnerAvatar?.(currentPartnerId) || '');
        if (imgEl) {
            if (avatarUrl) { imgEl.src = avatarUrl; imgEl.style.display = 'block'; }
            else { imgEl.style.display = 'none'; imgEl.removeAttribute('src'); }
        }

        // Duration
        if (callState === 'InCall') {
            dur.style.display = '';
        } else {
            dur.style.display = 'none';
        }

        // Controls
        if (callState === 'IncomingRinging') {
            controls.innerHTML = `
                <button type="button" class="mud-button-root mud-fab mud-fab-medium mud-fab-success mud-ripple" onclick="pmWebRTC.answerCall(true)">
                    <span class="mud-fab-label"><span class="mud-icon-root mud-svg-icon"><svg viewBox="0 0 24 24"><path d="M20.01 15.38c-1.23 0-2.42-.2-3.53-.56-.35-.12-.74-.03-1.01.24l-1.57 1.97c-2.83-1.35-5.48-3.9-6.89-6.83l1.95-1.66c.27-.28.35-.67.24-1.02-.37-1.11-.56-2.3-.56-3.53 0-.54-.45-.99-.99-.99H4.19C3.65 3 3 3.24 3 3.99 3 13.28 10.73 21 20.01 21c.71 0 .99-.63.99-1.18v-3.45c0-.54-.45-.99-.99-.99z"/></svg></span></span>
                </button>
                <button type="button" class="mud-button-root mud-fab mud-fab-medium mud-fab-error mud-ripple" onclick="pmWebRTC.answerCall(false)">
                    <span class="mud-fab-label"><span class="mud-icon-root mud-svg-icon"><svg viewBox="0 0 24 24"><path d="M12 9c-1.6 0-3.15.25-4.6.72v3.1c0 .39-.23.74-.56.9-.98.49-1.87 1.12-2.66 1.85-.18.18-.43.28-.7.28-.28 0-.53-.11-.71-.29L.29 13.08c-.18-.17-.29-.42-.29-.7 0-.28.11-.53.29-.71C3.34 8.78 7.46 7 12 7s8.66 1.78 11.71 4.67c.18.18.29.43.29.71 0 .28-.11.53-.29.71l-2.48 2.48c-.18.18-.43.29-.71.29-.27 0-.52-.11-.7-.28-.79-.74-1.69-1.36-2.67-1.85-.33-.16-.56-.5-.56-.9v-3.1C15.15 9.25 13.6 9 12 9z"/></svg></span></span>
                </button>
            `;
        } else {
            let html = '';
            if (callState === 'InCall') {
                html += `
                    <button type="button" class="mud-button-root mud-fab mud-fab-medium ${isMuted?'mud-fab-error':'mud-fab-dark'} mud-ripple" onclick="pmWebRTC.toggleMute()">
                        <span class="mud-fab-label"><span class="mud-icon-root mud-svg-icon">
                            ${isMuted ? '<svg viewBox="0 0 24 24"><path d="M19 11h-1.7c0 .74-.16 1.43-.43 2.05l1.23 1.23c.56-.98.9-2.12.9-3.28zm-4.02.17c0-.06.02-.11.02-.17V5c0-1.66-1.34-3-3-3S9 3.34 9 5v.18l5.98 5.99zM4.27 3L3 4.27l6 6V11c0 1.66 1.34 3 3 3 .23 0 .44-.03.65-.08l1.66 1.66c-.71.33-1.5.52-2.31.52-2.76 0-5.3-2.1-5.3-5.1H5c0 3.41 2.72 6.23 6 6.72V21h2v-3.28c.91-.13 1.77-.45 2.54-.9L19.73 21 21 19.73 4.27 3z"/></svg>' : '<svg viewBox="0 0 24 24"><path d="M12 14c1.66 0 2.99-1.34 2.99-3L15 5c0-1.66-1.34-3-3-3S9 3.34 9 5v6c0 1.66 1.34 3 3 3zm5.3-3c0 3-2.54 5.1-5.3 5.1S6.7 14 6.7 11H5c0 3.41 2.72 6.23 6 6.72V21h2v-3.28c3.28-.48 6-3.3 6-6.72h-1.7z"/></svg>'}
                        </span></span>
                    </button>
                `;
                if (isVideoCall) {
                    html += `
                        <button type="button" class="mud-button-root mud-fab mud-fab-medium ${isVideoOff?'mud-fab-error':'mud-fab-dark'} mud-ripple" onclick="pmWebRTC.toggleVideo()">
                            <span class="mud-fab-label"><span class="mud-icon-root mud-svg-icon">
                                ${isVideoOff ? '<svg viewBox="0 0 24 24"><path d="M21 6.5l-4 4V7c0-.55-.45-1-1-1H9.82L21 17.18V6.5zM3.27 2L2 3.27 4.73 6H4c-.55 0-1 .45-1 1v10c0 .55.45 1 1 1h12c.21 0 .39-.08.54-.18L19.73 21 21 19.73 3.27 2z"/></svg>' : '<svg viewBox="0 0 24 24"><path d="M17 10.5V7c0-.55-.45-1-1-1H4c-.55 0-1 .45-1 1v10c0 .55.45 1 1 1h12c.55 0 1-.45 1-1v-3.5l4 4v-11l-4 4z"/></svg>'}
                            </span></span>
                        </button>
                    `;
                }
            }
            html += `
                <button type="button" class="mud-button-root mud-fab mud-fab-medium mud-fab-error mud-ripple" onclick="pmWebRTC.endCall()">
                    <span class="mud-fab-label"><span class="mud-icon-root mud-svg-icon"><svg viewBox="0 0 24 24"><path d="M12 9c-1.6 0-3.15.25-4.6.72v3.1c0 .39-.23.74-.56.9-.98.49-1.87 1.12-2.66 1.85-.18.18-.43.28-.7.28-.28 0-.53-.11-.71-.29L.29 13.08c-.18-.17-.29-.42-.29-.7 0-.28.11-.53.29-.71C3.34 8.78 7.46 7 12 7s8.66 1.78 11.71 4.67c.18.18.29.43.29.71 0 .28-.11.53-.29.71l-2.48 2.48c-.18.18-.43.29-.71.29-.27 0-.52-.11-.7-.28-.79-.74-1.69-1.36-2.67-1.85-.33-.16-.56-.5-.56-.9v-3.1C15.15 9.25 13.6 9 12 9z"/></svg></span></span>
                </button>
            `;
            controls.innerHTML = html;
        }

        // Timer
        if (callState === 'InCall') {
            if (!timerInt) {
                callStartTime = new Date();
                timerInt = setInterval(() => {
                    const diff = Math.floor((new Date() - callStartTime) / 1000);
                    const m = String(Math.floor(diff / 60)).padStart(2, '0');
                    const s = String(diff % 60).padStart(2, '0');
                    $('pm-call-duration').textContent = `${m}:${s}`;
                }, 1000);
            }
        } else {
            clearInterval(timerInt);
            timerInt = null;
            $('pm-call-duration').textContent = '00:00';
        }
    }

    function cleanup() {
        if (peerConnection) {
            peerConnection.close();
            peerConnection = null;
        }
        if (localStream) {
            localStream.getTracks().forEach(track => track.stop());
            localStream = null;
        }
        const lv = $('local-video');
        const rv = $('remote-video');
        if (lv) lv.srcObject = null;
        if (rv) rv.srcObject = null;
        
        callState = 'Idle';
        currentPartnerId = null;
        currentPartnerAvatar = '';
        updateOverlay();
    }

    async function initMedia(video) {
        try {
            localStream = await navigator.mediaDevices.getUserMedia({ video, audio: true });
            const lv = $('local-video');
            if (lv) lv.srcObject = localStream;
            return true;
        } catch (e) {
            console.error("Error accessing media devices.", e);
            pmToast.error('Không thể truy cập camera/micro');
            return false;
        }
    }

    function createPeerConnection() {
        peerConnection = new RTCPeerConnection(configuration);

        if (localStream) {
            localStream.getTracks().forEach(track => {
                peerConnection.addTrack(track, localStream);
            });
        }

        peerConnection.ontrack = (event) => {
            const rv = $('remote-video');
            if (rv && rv.srcObject !== event.streams[0]) {
                rv.srcObject = event.streams[0];
            }
        };

        peerConnection.onicecandidate = (event) => {
            if (event.candidate && signalRConn && currentPartnerId) {
                signalRConn.invoke('SendWebRTCSignal', currentPartnerId, 'ice', JSON.stringify(event.candidate));
            }
        };

        peerConnection.onconnectionstatechange = () => {
            if (peerConnection.connectionState === 'disconnected' || peerConnection.connectionState === 'failed') {
                endCall();
            }
        };
    }

    async function startCall(partnerId, video) {
        currentPartnerId = partnerId;
        currentPartnerAvatar = window.pmChat?.getPartnerAvatar?.(partnerId) || '';
        isVideoCall = video;
        callState = 'OutgoingRinging';
        isMuted = false;
        isVideoOff = false;
        updateOverlay();

        const success = await initMedia(video);
        if (!success) { endCall('Failed'); return; }

        createPeerConnection();

        try {
            await signalRConn.invoke('CallUser', partnerId, video);
            
            const offer = await peerConnection.createOffer();
            await peerConnection.setLocalDescription(offer);
            if (signalRConn) {
                await signalRConn.invoke('SendWebRTCSignal', currentPartnerId, 'offer', JSON.stringify(offer));
            }
        } catch (e) {
            console.error("Error creating offer", e);
            endCall('Failed');
        }
    }

    async function answerCall(accept) {
        if (!accept) {
            if (signalRConn && currentPartnerId) {
                await signalRConn.invoke('AnswerCall', currentPartnerId, false);
                await signalRConn.invoke('EndCall', currentPartnerId, 0, isVideoCall, 'Rejected');
            }
            cleanup();
            return;
        }

        if (signalRConn && currentPartnerId) {
            await signalRConn.invoke('AnswerCall', currentPartnerId, true);
        }

        callState = 'InCall';
        updateOverlay();

        const success = await initMedia(isVideoCall);
        if (!success) { endCall('Failed'); return; }

        createPeerConnection();

        if (window._pendingOffer) {
            await handleOfferObj(window._pendingOffer);
            window._pendingOffer = null;
        }
    }

    async function handleOfferObj(offer) {
        if (!peerConnection) createPeerConnection();
        try {
            await peerConnection.setRemoteDescription(new RTCSessionDescription(offer));
            const answer = await peerConnection.createAnswer();
            await peerConnection.setLocalDescription(answer);
            if (signalRConn && currentPartnerId) {
                await signalRConn.invoke('SendWebRTCSignal', currentPartnerId, 'answer', JSON.stringify(answer));
            }
        } catch (e) {
            console.error("Error handling offer", e);
        }
    }

    function endCall(reason) {
        const dur = callStartTime ? Math.floor((new Date() - callStartTime) / 1000) : 0;
        if (signalRConn && currentPartnerId && callState !== 'Idle') {
            signalRConn.invoke('EndCall', currentPartnerId, dur, isVideoCall, reason || 'Ended').catch(()=>{});
        }
        cleanup();
    }

    function handleIncoming(data) {
        if (callState !== 'Idle') {
            // Busy
            signalRConn.invoke('AnswerCall', data.callerId, false);
            return;
        }
        currentPartnerId = data.callerId;
        currentPartnerAvatar = data.callerAvatar || '';
        isVideoCall = data.isVideoCall;
        callState = 'IncomingRinging';
        updateOverlay();
    }

    function handleCallAnswered(data) {
        if (!data.accepted) {
            pmToast.info('Cuộc gọi bị từ chối');
            cleanup();
        } else {
            callState = 'InCall';
            updateOverlay();
        }
    }

    function handleCallEnded(data) {
        pmToast.info('Cuộc gọi kết thúc');
        cleanup();
    }

    function handleWebRTCSignal(data) {
        if (data.type === 'offer') {
            let offer = JSON.parse(data.payload);
            if (callState === 'InCall') {
                handleOfferObj(offer);
            } else {
                window._pendingOffer = offer;
            }
        } 
        else if (data.type === 'answer') {
            if (peerConnection) {
                peerConnection.setRemoteDescription(new RTCSessionDescription(JSON.parse(data.payload))).catch(console.error);
                callState = 'InCall';
                updateOverlay();
            }
        }
        else if (data.type === 'ice') {
            if (peerConnection) {
                peerConnection.addIceCandidate(new RTCIceCandidate(JSON.parse(data.payload))).catch(console.error);
            }
        }
    }

    function bindDraggable() {
        const overlay = $('pm-call-overlay');
        const header = $('pm-call-header');
        if (!overlay || !header) return;

        let isDragging = false;
        let startX, startY, startLeft, startTop;

        header.addEventListener('mousedown', e => {
            isDragging = true;
            startX = e.clientX;
            startY = e.clientY;
            startLeft = parseInt(overlay.style.left || 0, 10);
            startTop = parseInt(overlay.style.top || 0, 10);
        });

        document.addEventListener('mousemove', e => {
            if (!isDragging) return;
            const dx = e.clientX - startX;
            const dy = e.clientY - startY;
            overlay.style.left = (startLeft + dx) + 'px';
            overlay.style.top = (startTop + dy) + 'px';
        });

        document.addEventListener('mouseup', () => {
            isDragging = false;
        });
    }

    document.addEventListener('DOMContentLoaded', bindDraggable);

    return {
        init: function(conn) { signalRConn = conn; },
        startCall,
        answerCall,
        endCall,
        handleIncoming,
        handleCallAnswered,
        handleCallEnded,
        handleWebRTCSignal,
        toggleMute: function() {
            isMuted = !isMuted;
            if (localStream) localStream.getAudioTracks().forEach(t => t.enabled = !isMuted);
            updateOverlay();
        },
        toggleVideo: function() {
            isVideoOff = !isVideoOff;
            if (localStream) localStream.getVideoTracks().forEach(t => t.enabled = !isVideoOff);
            updateOverlay();
        }
    };
})();

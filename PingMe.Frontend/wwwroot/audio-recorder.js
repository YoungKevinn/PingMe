// audio-recorder.js
// Simple wrapper around MediaRecorder API for voice chat
// Provides startRecording, stopRecording, getBlobUrl functions.

let mediaRecorder = null;
let recordedChunks = [];

export async function startRecording() {
    if (!navigator.mediaDevices || !navigator.mediaDevices.getUserMedia) {
        throw new Error('Media devices not supported');
    }
    const stream = await navigator.mediaDevices.getUserMedia({ audio: true });
    mediaRecorder = new MediaRecorder(stream);
    recordedChunks = [];
    mediaRecorder.ondataavailable = e => {
        if (e.data.size > 0) recordedChunks.push(e.data);
    };
    mediaRecorder.start();
    return true;
}

export async function stopRecording() {
    return new Promise((resolve, reject) => {
        if (!mediaRecorder) {
            reject('Recorder not started');
            return;
        }
        mediaRecorder.onstop = () => {
            const blob = new Blob(recordedChunks, { type: mediaRecorder.mimeType || 'audio/webm' });
            resolve(blob);
        };
        mediaRecorder.stop();
    });
}

export async function stopRecordingBytes() {
    const blob = await stopRecording();
    const arrayBuffer = await blob.arrayBuffer();
    return new Uint8Array(arrayBuffer);
}

export function getBlobUrl(blob) {
    return URL.createObjectURL(blob);
}

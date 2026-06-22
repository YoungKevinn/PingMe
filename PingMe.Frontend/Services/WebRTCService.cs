using Microsoft.JSInterop;
using PingMe.Frontend.Models;
using System.Text.Json;

namespace PingMe.Frontend.Services;

public class WebRTCService : IDisposable
{
    private readonly IJSRuntime _js;
    private readonly ChatHubService _chatHub;
    private DotNetObjectReference<WebRTCService>? _objRef;

    public CallState CurrentState { get; private set; } = CallState.Idle;
    public int? PartnerId { get; private set; }
    public string? PartnerName { get; private set; }
    public string? PartnerAvatar { get; private set; }
    public bool IsVideoCall { get; private set; }
    public DateTime? CallStartTime { get; private set; }
    public bool IsMuted { get; private set; } = false;
    public bool IsVideoOff { get; private set; } = false;

    public event Action? OnCallStateChanged;

    public WebRTCService(IJSRuntime js, ChatHubService chatHub)
    {
        _js = js;
        _chatHub = chatHub;

        _chatHub.OnIncomingCall += HandleIncomingCall;
        _chatHub.OnCallAnswered += HandleCallAnswered;
        _chatHub.OnWebRTCSignal += HandleWebRTCSignal;
        _chatHub.OnCallEnded += HandleCallEnded;
    }

    public async Task InitializeAsync()
    {
        _objRef = DotNetObjectReference.Create(this);
        await _js.InvokeVoidAsync("PingMeWebRTC.initialize", _objRef);
    }

    // --- Actions ---

    public async Task InitiateCallAsync(int receiverId, string receiverName, string? receiverAvatar, bool isVideo)
    {
        if (CurrentState != CallState.Idle) return;

        PartnerId = receiverId;
        PartnerName = receiverName;
        PartnerAvatar = receiverAvatar;
        IsVideoCall = isVideo;
        CurrentState = CallState.OutgoingRinging;
        NotifyStateChanged();

        await _chatHub.CallUserAsync(receiverId, isVideo);
    }

    public async Task AnswerCallAsync(bool accept)
    {
        if (CurrentState != CallState.IncomingRinging || !PartnerId.HasValue) return;

        await _chatHub.AnswerCallAsync(PartnerId.Value, accept);

        if (accept)
        {
            CurrentState = CallState.InCall;
            CallStartTime = DateTime.UtcNow;
            NotifyStateChanged();

            // Init media and wait for offer
            await _js.InvokeAsync<bool>("PingMeWebRTC.answerCall", IsVideoCall, "local-video", "remote-video");
        }
        else
        {
            await EndCallInternalAsync("Rejected");
        }
    }

    public async Task EndCallAsync()
    {
        if (CurrentState == CallState.Idle || !PartnerId.HasValue) return;
        
        string reason = "Ended";
        if (CurrentState == CallState.OutgoingRinging) reason = "Missed";
        else if (CurrentState == CallState.IncomingRinging) reason = "Rejected";

        await EndCallInternalAsync(reason);
    }

    private async Task EndCallInternalAsync(string reason)
    {
        if (PartnerId.HasValue)
        {
            int duration = CallStartTime.HasValue ? (int)(DateTime.UtcNow - CallStartTime.Value).TotalSeconds : 0;
            // Tell backend to end call and log it
            await _chatHub.EndCallAsync(PartnerId.Value, duration, IsVideoCall, reason);
        }

        ResetState();
    }

    public async Task ToggleMuteAsync()
    {
        IsMuted = !IsMuted;
        await _js.InvokeVoidAsync("PingMeWebRTC.toggleMute", IsMuted);
        NotifyStateChanged();
    }

    public async Task ToggleVideoAsync()
    {
        IsVideoOff = !IsVideoOff;
        await _js.InvokeVoidAsync("PingMeWebRTC.toggleVideo", IsVideoOff);
        NotifyStateChanged();
    }

    // --- SignalR Handlers ---

    private void HandleIncomingCall(IncomingCallEvent evt)
    {
        if (CurrentState != CallState.Idle)
        {
            // Busy
            _ = _chatHub.AnswerCallAsync(evt.CallerId, false);
            return;
        }

        PartnerId = evt.CallerId;
        PartnerName = evt.CallerName;
        PartnerAvatar = evt.CallerAvatar;
        IsVideoCall = evt.IsVideoCall;
        CurrentState = CallState.IncomingRinging;
        NotifyStateChanged();
    }

    private async void HandleCallAnswered(CallAnsweredEvent evt)
    {
        if (CurrentState == CallState.OutgoingRinging && evt.ResponderId == PartnerId)
        {
            if (evt.Accepted)
            {
                CurrentState = CallState.InCall;
                CallStartTime = DateTime.UtcNow;
                NotifyStateChanged();

                // Start WebRTC and create offer
                await _js.InvokeAsync<bool>("PingMeWebRTC.startCall", IsVideoCall, "local-video", "remote-video");
            }
            else
            {
                // Rejected
                ResetState();
            }
        }
    }

    private async void HandleWebRTCSignal(WebRTCSignalEvent evt)
    {
        if (CurrentState != CallState.InCall || evt.SenderId != PartnerId) return;

        if (evt.Type == "offer")
        {
            await _js.InvokeVoidAsync("PingMeWebRTC.handleOffer", evt.Payload);
        }
        else if (evt.Type == "answer")
        {
            await _js.InvokeVoidAsync("PingMeWebRTC.handleAnswer", evt.Payload);
        }
        else if (evt.Type == "ice-candidate")
        {
            await _js.InvokeVoidAsync("PingMeWebRTC.handleIceCandidate", evt.Payload);
        }
    }

    private void HandleCallEnded(CallEndedEvent evt)
    {
        if (evt.SenderId == PartnerId)
        {
            ResetState();
        }
    }

    // --- JS Callbacks ---

    [JSInvokable]
    public async Task OnOfferGenerated(string offer)
    {
        if (PartnerId.HasValue)
            await _chatHub.SendWebRTCSignalAsync(PartnerId.Value, "offer", offer);
    }

    [JSInvokable]
    public async Task OnAnswerGenerated(string answer)
    {
        if (PartnerId.HasValue)
            await _chatHub.SendWebRTCSignalAsync(PartnerId.Value, "answer", answer);
    }

    [JSInvokable]
    public async Task OnIceCandidateGenerated(string candidate)
    {
        if (PartnerId.HasValue)
            await _chatHub.SendWebRTCSignalAsync(PartnerId.Value, "ice-candidate", candidate);
    }

    [JSInvokable]
    public void OnConnectionFailed()
    {
        _ = EndCallAsync();
    }

    // --- Helpers ---

    private void ResetState()
    {
        CurrentState = CallState.Idle;
        PartnerId = null;
        PartnerName = null;
        PartnerAvatar = null;
        CallStartTime = null;
        IsMuted = false;
        IsVideoOff = false;
        _ = _js.InvokeVoidAsync("PingMeWebRTC.endCall");
        NotifyStateChanged();
    }

    private void NotifyStateChanged() => OnCallStateChanged?.Invoke();

    public void Dispose()
    {
        _chatHub.OnIncomingCall -= HandleIncomingCall;
        _chatHub.OnCallAnswered -= HandleCallAnswered;
        _chatHub.OnWebRTCSignal -= HandleWebRTCSignal;
        _chatHub.OnCallEnded -= HandleCallEnded;
        _objRef?.Dispose();
    }
}

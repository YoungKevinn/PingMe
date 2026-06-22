using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.JSInterop;
using PingMe.Frontend.Models;

namespace PingMe.Frontend.Services;

public class ChatHubService : IAsyncDisposable
{
    private HubConnection? _hubConnection;
    private readonly IJSRuntime _js;

    public event Action<MessageResponse>? OnMessageReceived;
    public event Action<MessageResponse>? OnMessageEdited;
    public event Action<object>? OnMessageDeleted;
    public event Action<object>? OnUserTyping;
    public event Action<object>? OnUserStatusChanged;
    public event Action<ReadReceiptEvent>? OnMessageRead;
    public event Action<ReactionUpdateEvent>? OnMessageReactionUpdated;
    public event Action<MessagePinnedEvent>? OnMessagePinned;
    public event Action<GroupMemberAddedEvent>? OnGroupMemberAdded;
    public event Action<GroupMemberKickedEvent>? OnGroupMemberKicked;
    public event Action<TaskAssignedEvent>? OnTaskAssigned;
    public event Func<System.Text.Json.JsonElement, Task>? OnUserMentioned;
    public event Action<PollVoteUpdatedEvent>? OnPollVoteUpdated;

    // WebRTC Events
    public event Action<IncomingCallEvent>? OnIncomingCall;
    public event Action<CallAnsweredEvent>? OnCallAnswered;
    public event Action<WebRTCSignalEvent>? OnWebRTCSignal;
    public event Action<CallEndedEvent>? OnCallEnded;

    public ChatHubService(IJSRuntime js)
    {
        _js = js;
    }

    public async Task StartAsync()
    {
        if (_hubConnection is { State: HubConnectionState.Connected })
            return;

        var token = await _js.InvokeAsync<string>("localStorage.getItem", "auth_token");

        if (string.IsNullOrWhiteSpace(token))
            return;

        if (_hubConnection != null)
        {
            await _hubConnection.DisposeAsync();
            _hubConnection = null;
        }

        _hubConnection = new HubConnectionBuilder()
            .WithUrl($"https://localhost:5001/hubs/chat?access_token={token}")
            .WithAutomaticReconnect()
            .Build();

        RegisterHubEvents();

        await _hubConnection.StartAsync();
    }

    private void RegisterHubEvents()
    {
        if (_hubConnection == null)
            return;

        _hubConnection.On<MessageResponse>("ReceiveMessage", msg =>
        {
            OnMessageReceived?.Invoke(msg);
        });

        _hubConnection.On<MessageResponse>("MessageEdited", msg =>
        {
            OnMessageEdited?.Invoke(msg);
        });

        _hubConnection.On<object>("MessageDeleted", data =>
        {
            OnMessageDeleted?.Invoke(data);
        });

        _hubConnection.On<object>("UserTyping", data =>
        {
            OnUserTyping?.Invoke(data);
        });

        _hubConnection.On<System.Text.Json.JsonElement>("UserMentioned", data =>
        {
            if (OnUserMentioned != null)
                return OnUserMentioned.Invoke(data);
            return Task.CompletedTask;
        });

        _hubConnection.On<object>("UserStatusChanged", data =>
        {
            OnUserStatusChanged?.Invoke(data);
        });

        _hubConnection.On<ReadReceiptEvent>("MessageRead", data =>
        {
            OnMessageRead?.Invoke(data);
        });

        _hubConnection.On<ReactionUpdateEvent>("MessageReactionUpdated", data =>
        {
            OnMessageReactionUpdated?.Invoke(data);
        });

        _hubConnection.On<MessagePinnedEvent>("MessagePinned", data =>
        {
            OnMessagePinned?.Invoke(data);
        });

        _hubConnection.On<GroupMemberAddedEvent>("GroupMemberAdded", data =>
        {
            OnGroupMemberAdded?.Invoke(data);
        });

        _hubConnection.On<GroupMemberKickedEvent>("GroupMemberKicked", data =>
        {
            OnGroupMemberKicked?.Invoke(data);
        });

        _hubConnection.On<TaskAssignedEvent>("TaskAssigned", data =>
        {
            OnTaskAssigned?.Invoke(data);
        });

        _hubConnection.On<IncomingCallEvent>("IncomingCall", data =>
        {
            OnIncomingCall?.Invoke(data);
        });

        _hubConnection.On<CallAnsweredEvent>("CallAnswered", data =>
        {
            OnCallAnswered?.Invoke(data);
        });

        _hubConnection.On<WebRTCSignalEvent>("ReceiveWebRTCSignal", data =>
        {
            OnWebRTCSignal?.Invoke(data);
        });

        _hubConnection.On<CallEndedEvent>("CallEnded", data =>
        {
            OnCallEnded?.Invoke(data);
        });

        _hubConnection.On<PollVoteUpdatedEvent>("PollVoteUpdated", data =>
        {
            OnPollVoteUpdated?.Invoke(data);
        });
    }

    public async Task SendTypingAsync(int? groupId, int? receiverId)
    {
        if (_hubConnection?.State == HubConnectionState.Connected)
        {
            await _hubConnection.InvokeAsync("StartTyping", groupId, receiverId);
        }
    }

    public async Task StartTypingAsync(int? receiverId, int? groupId)
    {
        await SendTypingAsync(groupId, receiverId);
    }

    public async Task StopTypingAsync(int? receiverId, int? groupId)
    {
        if (_hubConnection?.State == HubConnectionState.Connected)
        {
            await _hubConnection.InvokeAsync("StopTyping", groupId, receiverId);
        }
    }

    public async Task MarkAsReadAsync(int messageId)
    {
        if (_hubConnection?.State == HubConnectionState.Connected)
        {
            await _hubConnection.InvokeAsync("MarkAsRead", messageId);
        }
    }

    public async Task LeaveGroupRoomAsync(int groupId)
    {
        if (_hubConnection?.State == HubConnectionState.Connected)
        {
            await _hubConnection.InvokeAsync("LeaveRoom", $"group_{groupId}");
        }
    }

    public async Task JoinGroupRoomAsync(int groupId)
    {
        if (_hubConnection?.State == HubConnectionState.Connected)
        {
            await _hubConnection.InvokeAsync("JoinRoom", $"group_{groupId}");
        }
    }

    public async Task CallUserAsync(int receiverId, bool isVideoCall)
    {
        if (_hubConnection?.State == HubConnectionState.Connected)
        {
            await _hubConnection.InvokeAsync("CallUser", receiverId, isVideoCall);
        }
    }

    public async Task AnswerCallAsync(int callerId, bool accepted)
    {
        if (_hubConnection?.State == HubConnectionState.Connected)
        {
            await _hubConnection.InvokeAsync("AnswerCall", callerId, accepted);
        }
    }

    public async Task SendWebRTCSignalAsync(int targetId, string type, string payload)
    {
        if (_hubConnection?.State == HubConnectionState.Connected)
        {
            await _hubConnection.InvokeAsync("SendWebRTCSignal", targetId, type, payload);
        }
    }

    public async Task EndCallAsync(int targetId, int durationSeconds, bool isVideoCall, string reason)
    {
        if (_hubConnection?.State == HubConnectionState.Connected)
        {
            await _hubConnection.InvokeAsync("EndCall", targetId, durationSeconds, isVideoCall, reason);
        }
    }

    public async Task StopAsync()
    {
        if (_hubConnection != null &&
            _hubConnection.State != HubConnectionState.Disconnected)
        {
            await _hubConnection.StopAsync();
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_hubConnection != null)
        {
            await _hubConnection.DisposeAsync();
            _hubConnection = null;
        }
    }
}

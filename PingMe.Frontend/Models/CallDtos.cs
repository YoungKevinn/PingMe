namespace PingMe.Frontend.Models;

public class IncomingCallEvent
{
    public int CallerId { get; set; }
    public string? CallerName { get; set; }
    public string? CallerAvatar { get; set; }
    public bool IsVideoCall { get; set; }
}

public class CallAnsweredEvent
{
    public int ResponderId { get; set; }
    public bool Accepted { get; set; }
}

public class WebRTCSignalEvent
{
    public int SenderId { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Payload { get; set; } = string.Empty;
}

public class CallEndedEvent
{
    public int SenderId { get; set; }
    public string Reason { get; set; } = string.Empty;
}

public enum CallState
{
    Idle,
    IncomingRinging,
    OutgoingRinging,
    InCall
}

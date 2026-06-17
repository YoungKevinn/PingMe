namespace PingMe.Models;

public enum GroupMemberRole
{
    Member,
    CoAdmin,
    Admin
}

public enum MessageType
{
    Text,
    Image,
    File,
    Audio,
    System,
    Snippet,   // shared code snippet card
    Command,   // slash command result
    Pinned,    // pin notification
    Cve,       // CVE/IOC card
    Vulnerability, // pentest finding card
    Reminder, // chat reminder card
    Task,     // group task card
    Call      // audio/video call log
}

public enum BackgroundType
{
    Color,
    Image,
    Gradient
}

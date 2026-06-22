namespace PingMe.Frontend.Services;

public sealed class UnreadStateService
{
    private int _totalUnread;

    public int TotalUnread => _totalUnread;

    public event Action? OnChanged;

    public void SetTotal(int totalUnread)
    {
        totalUnread = Math.Max(0, totalUnread);

        if (_totalUnread == totalUnread)
            return;

        _totalUnread = totalUnread;
        OnChanged?.Invoke();
    }

    public void Increment(int amount = 1)
    {
        if (amount <= 0)
            return;

        _totalUnread += amount;
        OnChanged?.Invoke();
    }
}

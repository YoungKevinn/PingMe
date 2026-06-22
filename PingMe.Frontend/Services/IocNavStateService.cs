namespace PingMe.Frontend.Services;

public sealed class IocNavStateService
{
    private readonly IocService _iocService;
    private int _totalIocBadgeCount;

    public IocNavStateService(IocService iocService)
    {
        _iocService = iocService;
    }

    public int TotalIocBadgeCount => _totalIocBadgeCount;

    public event Action? OnChanged;

    public async Task RefreshAsync()
    {
        var stats = await _iocService.GetStatsAsync();
        SetTotal(stats?.ActiveCount ?? 0);
    }

    private void SetTotal(int total)
    {
        total = Math.Max(0, total);

        if (_totalIocBadgeCount == total)
            return;

        _totalIocBadgeCount = total;
        OnChanged?.Invoke();
    }
}

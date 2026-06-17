using System.Collections.Concurrent;

namespace PingMe.Services;

public interface ISignalRConnectionTracker
{
    void AddConnection(int userId, string connectionId);
    void RemoveConnection(int userId, string connectionId);
    IReadOnlyCollection<string> GetConnections(int userId);
}

public class SignalRConnectionTracker : ISignalRConnectionTracker
{
    private readonly ConcurrentDictionary<int, ConcurrentDictionary<string, byte>> _connections = new();

    public void AddConnection(int userId, string connectionId)
    {
        var userConnections = _connections.GetOrAdd(userId, _ => new ConcurrentDictionary<string, byte>());
        userConnections.TryAdd(connectionId, 0);
    }

    public void RemoveConnection(int userId, string connectionId)
    {
        if (!_connections.TryGetValue(userId, out var userConnections))
            return;

        userConnections.TryRemove(connectionId, out _);

        if (userConnections.IsEmpty)
            _connections.TryRemove(userId, out _);
    }

    public IReadOnlyCollection<string> GetConnections(int userId)
    {
        return _connections.TryGetValue(userId, out var userConnections)
            ? userConnections.Keys.ToList()
            : Array.Empty<string>();
    }
}

using System.Collections.Concurrent;
using UDPServer.Models;

namespace UDPServer.Core;

public class TimeoutManager
{
    private readonly TimeSpan _timeoutLimit;
    private readonly Timer _timeoutTimer;
    private readonly ConcurrentDictionary<int, PlayerData> _players;
    private readonly Action<int> _onPlayerTimeout;
    private bool _isDisposed = false;

    public TimeoutManager(ConcurrentDictionary<int, PlayerData> players, int timeoutSeconds,
        Action<int> onPlayerTimeout)
    {
        _timeoutLimit = TimeSpan.FromSeconds(timeoutSeconds);
        _timeoutTimer = new Timer(CheckTimeout, null, 1000, 1000);
        _players = players;
        _onPlayerTimeout =  onPlayerTimeout;
    }

    private void CheckTimeout(object? state)
    {
        if (_isDisposed) return;
        try
        {
            DateTime now = DateTime.UtcNow;
            foreach (var player in _players.Values)
            {
                if (player.IsConnected && now - player.LastUpdateTime > _timeoutLimit)
                {
                    Console.WriteLine($"[TimeoutManager] {player.PlayerId} 타임아웃 감지");
                }
                player.SetConnectionStatus(false);
                _onPlayerTimeout?.Invoke(player.PlayerId);
            }
        }
        catch (Exception e)
        {
            Console.WriteLine($"[TimeoutManager] 타임아웃 체크 중 오류! {e.Message}");
        }
    }
}
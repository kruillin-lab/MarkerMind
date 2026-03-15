using TankCooldownHelper.Data;

namespace TankCooldownHelper.Core;

public class CombatEventBuffer
{
    private readonly Queue<CombatEvent> _events = new();
    private float _windowSeconds;
    private readonly object _lock = new();

    public CombatEventBuffer(float windowSeconds)
    {
        _windowSeconds = windowSeconds;
    }

    public void UpdateWindow(float newWindowSeconds)
    {
        lock (_lock)
        {
            _windowSeconds = newWindowSeconds;
            PruneOldEvents();
        }
    }

    public void AddEvent(CombatEvent evt)
    {
        lock (_lock)
        {
            _events.Enqueue(evt);
        }
    }

    public void PruneOldEvents()
    {
        lock (_lock)
        {
            var cutoff = DateTime.UtcNow.AddSeconds(-_windowSeconds);
            while (_events.Count > 0 && _events.Peek().Timestamp < cutoff)
            {
                _events.Dequeue();
            }
        }
    }

    public float GetTotalDamage(uint targetId)
    {
        lock (_lock)
        {
            return _events
                .Where(e => e.TargetId == targetId && e.Type == CombatEventType.Damage)
                .Sum(e => e.Amount);
        }
    }

    public float GetTotalHealing(uint targetId)
    {
        lock (_lock)
        {
            return _events
                .Where(e => e.TargetId == targetId && e.Type == CombatEventType.Healing)
                .Sum(e => e.Amount);
        }
    }

    public IEnumerable<uint> GetTrackedTargetIds()
    {
        lock (_lock)
        {
            return _events.Select(e => e.TargetId).Distinct().ToList();
        }
    }

    public void Clear()
    {
        lock (_lock)
        {
            _events.Clear();
        }
    }

    public int Count => _events.Count;
}

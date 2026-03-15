using ECommons.DalamudServices;
using FFXIVClientStructs.FFXIV.Client.Game;
using TankCooldownHelper.Data;

namespace TankCooldownHelper.Core;

public class CombatEventCollector : IDisposable
{
    private readonly CombatEventBuffer _buffer;
    private readonly Dictionary<uint, uint> _lastHpValues = new();

    public CombatEventCollector(CombatEventBuffer buffer)
    {
        _buffer = buffer;
    }

    public void PollCombatEvents()
    {
        // For MVP, we detect damage by monitoring HP changes
        DetectDamageFromHpChanges();
    }

    public void DetectDamageFromHpChanges()
    {
        var player = Svc.ClientState.LocalPlayer;
        if (player == null) return;
        
        // Check player HP
        CheckHpChange((uint)player.GameObjectId, player.CurrentHp, player.MaxHp);
        
        // Check party members
        foreach (var member in Svc.Party)
        {
            if (member?.GameObject is not { } gameObject) continue;
            if (gameObject is not Dalamud.Game.ClientState.Objects.Types.ICharacter character) continue;
            
            CheckHpChange((uint)gameObject.GameObjectId, character.CurrentHp, character.MaxHp);
        }
    }

    private void CheckHpChange(uint objectId, uint currentHp, uint maxHp)
    {
        if (_lastHpValues.TryGetValue(objectId, out var lastHp))
        {
            var hpDiff = (int)currentHp - (int)lastHp;
            
            if (hpDiff < 0)
            {
                // HP went down - damage taken
                AddDamageEvent(objectId, 0, 0, Math.Abs(hpDiff));
            }
            else if (hpDiff > 0)
            {
                // HP went up - healing received
                AddHealingEvent(objectId, 0, 0, hpDiff);
            }
        }
        
        _lastHpValues[objectId] = currentHp;
    }

    public void AddDamageEvent(uint targetId, uint sourceId, uint actionId, float amount)
    {
        _buffer.AddEvent(new CombatEvent(targetId, sourceId, actionId, amount, CombatEventType.Damage));
    }

    public void AddHealingEvent(uint targetId, uint sourceId, uint actionId, float amount)
    {
        _buffer.AddEvent(new CombatEvent(targetId, sourceId, actionId, amount, CombatEventType.Healing));
    }

    public void Dispose()
    {
        _lastHpValues.Clear();
    }
}

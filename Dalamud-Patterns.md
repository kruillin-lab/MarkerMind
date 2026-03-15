# Dalamud Plugin Patterns - Cross-Project Reference

Extracted from ParseLord3, VoiceMaster, and ActionStacksEX.

## ECommons Initialization

All three projects use ECommons. Two patterns observed:

### Pattern A: Svc Static Access (ParseLord3)
```csharp
ECommonsMain.Init(pluginInterface, this,
    ECommons.Module.DalamudReflector,
    ECommons.Module.ObjectFunctions);

// Access anywhere
var clientState = Svc.ClientState;
var framework = Svc.Framework;
```

### Pattern B: Traditional DI (VoiceMaster)
```csharp
[PluginService] internal static IClientState ClientState { get; private set; } = null!;
[PluginService] internal static IFramework Framework { get; private set; } = null!;
```

**Recommendation:** Use Pattern A for new plugins - cleaner, less boilerplate.

## Framework.Update Pattern

Standard game loop hook across all projects:

```csharp
public Plugin()
{
    Framework.Update += OnFrameworkUpdate;
}

private void OnFrameworkUpdate(IFramework framework)
{
    if (!ClientState.IsLoggedIn) return;
    if (ClientState.LocalPlayer is not { } player) return;

    // Plugin logic here
}

public void Dispose()
{
    Framework.Update -= OnFrameworkUpdate;
}
```

## Action Execution Patterns

### Safe Action Check (ParseLord3/ActionStacksEX)
```csharp
public static unsafe bool CanUseAction(uint actionId, IGameObject? target = null)
{
    var am = ActionManager.Instance();
    if (am == null) return false;

    var targetId = target?.GameObjectId ?? 0xE0000000;
    return am->CanUseAction(ActionType.Action, actionId, (uint)targetId);
}
```

### Weaving Windows (ParseLord3)
```csharp
public static bool CanWeave() => GetGcdRemaining() >= 0.65f && GetAnimationLock() <= 0;
public static bool CanDoubleWeave() => GetGcdRemaining() >= 1.25f && GetAnimationLock() <= 0;
public static bool InQueueWindow() => GetGcdRemaining() <= 0.5f && GetGcdRemaining() > 0;
```

## Target Resolution Patterns

### TargetTag Enum (Used in ParseLord3 & ActionStacksEX)
```csharp
public enum TargetTag
{
    Self, Target, Tank, LowestHpPartyMember,
    Mouseover, DispellablePartyMember, TargetsTarget
}
```

### Lowest HP Party Member (Common pattern)
```csharp
static IGameObject? GetLowestHpPartyMember()
{
    var player = ClientState.LocalPlayer;
    if (player is null) return null;

    var lowest = player;
    var lowestHpPercent = (float)player.CurrentHp / player.MaxHp;

    foreach (var member in PartyList)
    {
        if (member?.GameObject is not ICharacter character) continue;
        var hpPercent = (float)character.CurrentHp / character.MaxHp;
        if (hpPercent < lowestHpPercent)
        {
            lowestHpPercent = hpPercent;
            lowest = character;
        }
    }
    return lowest;
}
```

## Status Checking

### With Source Validation (ParseLord3 pattern)
```csharp
public static float? GetStatusDuration(uint statusId, bool onSelf = true)
{
    var character = onSelf ? ClientState.LocalPlayer : TargetManager.Target as ICharacter;
    if (character is null) return null;

    foreach (var status in character.StatusList)
    {
        if (status.StatusId == statusId &&
            status.SourceId == unchecked((uint)ClientState.LocalPlayer?.GameObjectId ?? 0))
        {
            return status.RemainingTime;
        }
    }
    return null;
}
```

## Addon Event Subscription

### VoiceMaster Pattern (Dialogue systems)
```csharp
public Plugin()
{
    AddonLifecycle.RegisterListener(AddonEvent.PostDraw, "Talk", OnTalkPostDraw);
    AddonLifecycle.RegisterListener(AddonEvent.PreFinalize, "SelectString", OnSelectStringPreFinalize);
}

private void OnTalkPostDraw(AddonEvent type, AddonArgs args)
{
    var addon = (AddonTalk*)args.Addon;
    // Process talk window
}
```

### ActionStacksEX Pattern (Action interception)
```csharp
// Hooks into ActionManager.UseAction via Hypostasis
ActionStackManager.OnPreUseAction += (ref ActionData data) => {
    // Modify or block actions
};
```

## Project File Auto-Deploy

All projects use similar PostBuild targets:

```xml
<Target Name="CopyToDevPlugins" AfterTargets="Build">
    <PropertyGroup>
        <DevPluginsPath>$(APPDATA)\XIVLauncher\devPlugins\MyPlugin\</DevPluginsPath>
    </PropertyGroup>
    <MakeDir Directories="$(DevPluginsPath)" Condition="!Exists('$(DevPluginsPath)')" />
    <Copy SourceFiles="$(OutputPath)MyPlugin.dll" DestinationFiles="$(DevPluginsPath)MyPlugin.dll" />
    <Copy SourceFiles="$(OutputPath)ECommons.dll" DestinationFiles="$(DevPluginsPath)ECommons.dll" />
    <Copy SourceFiles="$(MSBuildProjectDirectory)\MyPlugin.json" DestinationFiles="$(DevPluginsPath)MyPlugin.json" />
</Target>
```

## Common Pitfalls (Across All Projects)

1. **Null reference on player** - Always check `ClientState.LocalPlayer is not null`
2. **Not logged in** - Check `ClientState.IsLoggedIn` first
3. **Status SourceId** - Use `unchecked((uint)GameObjectId)` for comparison
4. **Action IDs** - Call `GetAdjustedActionId()` for trait upgrades
5. **Hostile detection** - Check `ObjectKind.BattleNpc`, not just party membership
6. **Thread safety** - Only access game state from Framework.Update

## See Also

- [[ActionStacksEX/CLAUDE.md]]
- [[ParseLord3/CLAUDE.md]]
- [[VoiceMaster/CLAUDE.md]]
- [[../.claude/skills/dalamud-dev.md|Dalamud Dev Skill]]

---

*Extracted from: ParseLord3, VoiceMaster, ActionStacksEX*

using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using ECommons.GameHelpers;
using System.Numerics;

namespace MarkerMind;

public enum PlayerRole
{
    Unknown,
    Tank,
    Healer,
    MeleeDPS,
    RangedDPS,
    CasterDPS
}

public class GameStateTracker : IDisposable
{
    public IPlayerCharacter? LocalPlayer { get; private set; }
    public Vector3 Position { get; private set; }
    public Vector3 Velocity { get; private set; }
    public float Facing { get; private set; }
    public PlayerRole Role { get; private set; } = PlayerRole.Unknown;
    public uint TerritoryId { get; private set; }
    
    private Vector3 lastPosition;
    private DateTime lastUpdate = DateTime.MinValue;

    public void Update(IPlayerCharacter player)
    {
        LocalPlayer = player;
        TerritoryId = Svc.ClientState.TerritoryType;
        
        var currentPos = player.Position;
        var now = DateTime.UtcNow;
        var deltaTime = (float)(now - lastUpdate).TotalSeconds;
        
        if (deltaTime > 0)
        {
            Velocity = (currentPos - lastPosition) / deltaTime;
        }
        
        Position = currentPos;
        lastPosition = currentPos;
        lastUpdate = now;
        
        Facing = player.Rotation;
        
        if (Role == PlayerRole.Unknown)
            DetectRole(player);
    }
    
    private void DetectRole(IPlayerCharacter player)
    {
        var jobId = player.ClassJob.RowId;
        Role = jobId switch
        {
            1 or 3 or 19 or 21 or 32 or 37 => PlayerRole.Tank, // GLA, MRD, PLD, WAR, DRK, GNB
            6 or 26 or 33 => PlayerRole.Healer, // CNJ, WHM, SCH, AST, SGE
            2 or 4 or 29 or 34 or 39 => PlayerRole.MeleeDPS, // PGL, LNC, ROG, SAM, RPR, VPR
            5 or 23 or 31 or 38 => PlayerRole.RangedDPS, // ARC, BRD, MCH, DNC
            7 or 26 or 35 or 40 => PlayerRole.CasterDPS, // THM, ACN, BLM, SMN, RDM, PCT
            _ => PlayerRole.Unknown
        };
    }
    
    public void Dispose()
    {
        LocalPlayer = null;
    }
}

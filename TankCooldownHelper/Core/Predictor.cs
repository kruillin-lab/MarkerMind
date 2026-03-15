using TankCooldownHelper.Data;

namespace TankCooldownHelper.Core;

public class Predictor
{
    private readonly DangerCalculator _dangerCalculator;

    public Predictor(DangerCalculator dangerCalculator)
    {
        _dangerCalculator = dangerCalculator;
    }

    public float? PredictTimeOfDeath(uint currentHp, float dps, float hps)
    {
        return _dangerCalculator.CalculateDeathTimer(currentHp, dps, hps);
    }

    public float[] ProjectHpTimeline(uint currentHp, uint maxHp, float dps, float hps, int secondsToProject)
    {
        var timeline = new float[secondsToProject + 1];
        var netDps = dps - hps;
        
        timeline[0] = currentHp;
        
        for (int i = 1; i <= secondsToProject; i++)
        {
            var projectedHp = currentHp - (netDps * i);
            timeline[i] = Math.Max(0, Math.Min(projectedHp, maxHp));
        }
        
        return timeline;
    }

    public DangerLevel PredictDangerState(float dps, float hps)
    {
        return _dangerCalculator.CalculateDangerLevel(dps, hps);
    }
}

// HazardTarget.cs
using UnityEngine;

public class HazardTarget : Target
{
    protected override void OnEnable()
    {
        base.OnEnable();
        // Treat a missed/expired hazard as a soft miss: no life loss on timeout.
        softMissOnly = true;
    }

    public override void OnClicked(bool wasAccurate)
    {
        if (!alive) return;
        alive = false;
        // Clicking a hazard should still punish the player.
        GameManager.I.RegisterHazard();
        FX.SpawnBuzzer(transform.position);
        Despawn();
    }

    protected override void Timeout()
    {
        // Override default behavior: DO NOT call RegisterMiss on hazards.
        // No penalty for simply avoiding hazards until they disappear.
        Despawn();
    }
}

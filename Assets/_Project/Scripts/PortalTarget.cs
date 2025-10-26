// PortalTarget.cs
using UnityEngine;
public class PortalTarget : Target
{
    [Tooltip("Name of the next scene to load")] public string nextScene;
    public override void OnClicked(bool wasAccurate)
    {
        if (!alive) return; alive = false;
        GameManager.I.RegisterHit(scoreValue, wasAccurate);
        FX.SpawnPortal(transform.position);
        SceneFlow.LoadScene(nextScene);
        Despawn();
    }
}
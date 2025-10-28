using UnityEngine;  // ✅ this is what you were missing

public class PortalTarget : Target
{
    [Tooltip("Name of the next scene to load")]
    public string nextScene;

    public override void OnClicked(bool wasAccurate)
    {
        if (!alive) return;
        alive = false;

        if (string.IsNullOrEmpty(nextScene))
        {
            Debug.LogError("[PortalTarget] nextScene is EMPTY. Did SceneConfig.nextSceneName get set for this scene?");
            GameManager.I.RegisterMiss(); // optional safeguard
            Despawn();
            return;
        }

        Debug.Log($"[PortalTarget] Loading next scene: '{nextScene}'");
        GameManager.I.RegisterHit(scoreValue, wasAccurate);
        FX.SpawnPortal(transform.position);
        SceneFlow.LoadScene(nextScene);
        Despawn();
    }
}

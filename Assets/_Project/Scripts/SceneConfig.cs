using UnityEngine;

public class SceneConfig : MonoBehaviour
{
    [Header("Portal Reveal Setting for THIS scene")]
    [Tooltip("-1 = reveal after all waves; 1 = after wave 1; 2 = after wave 2; etc.")]
    public int revealPortalAfterWave = -1;

    [Header("Where to go next (used for in-scene portal or spawned prefab)")]
    public string nextSceneName; // e.g., "Game_Scene3" or "FinalTally"

    [Header("Portal hookup options (pick ONE approach)")]
    public GameObject portalGO;        // assign a disabled scene-placed Portal (tagged Portal)
    public PortalTarget portalPrefab;  // OR assign the portal prefab to spawn
    public Transform portalSpawnPoint; // optional spawn location (else 0,0,0)

    void Awake()
    {
        if (GameManager.I != null)
        {
            GameManager.I.ApplySceneConfig(this);
        }
    }
}

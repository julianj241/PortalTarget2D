// FX.cs (centralized spawn helpers)
using UnityEngine;
public static class FX
{
    static GameObject hitPrefab, portalPrefab, muzzlePrefab;
    public static void Init(GameObject hit, GameObject portal, GameObject muzzle) { hitPrefab = hit; portalPrefab = portal; muzzlePrefab = muzzle; }
    public static void SpawnHit(Vector3 p) { if (hitPrefab) Object.Instantiate(hitPrefab, p, Quaternion.identity); }
    public static void SpawnPortal(Vector3 p) { if (portalPrefab) Object.Instantiate(portalPrefab, p, Quaternion.identity); AudioHub.PlayPortal(); }
    public static void SpawnBuzzer(Vector3 p)
    {
        // Placeholder effect for hazard hits
        // You can expand this later to spawn a red flash or explosion
        if (hitPrefab) Object.Instantiate(hitPrefab, p, Quaternion.identity);
        AudioHub.PlayHazard();
    }

}
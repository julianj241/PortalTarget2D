// TargetSpawner.cs — simple, interleaved, no pooling
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public struct SpawnEntry
{
    public Target prefab;      // e.g., P_Target_Standard or P_Target_Hazard
    public int count;          // how many of THIS prefab to spawn in this wave
    public Vector2 areaMin;    // spawn rect (x,y)
    public Vector2 areaMax;    // spawn rect (x,y)
}

[System.Serializable]
public class WaveConfig
{
    [Header("What spawns in this wave")]
    public List<SpawnEntry> entries = new List<SpawnEntry>();

    [Header("Timing")]
    [Tooltip("Average delay between spawns in this wave")]
    public float interval = 0.7f;

    [Tooltip("Random ± variance added to interval")]
    public float spawnJitter = 0.2f;
}

public class TargetSpawner : MonoBehaviour
{
    [Header("All Waves in this Scene")]
    public List<WaveConfig> waves = new List<WaveConfig>();

    [Header("Optional global interval override (set by GameManager)")]
    [Tooltip("If > 0, this value overrides the wave's interval.")]
    public float interval = -1f;   // your GameManager can set this; <=0 means 'use wave interval'

    void OnValidate()
    {
        // Clamp silly values in editor to avoid instant bursts
        if (waves != null)
        {
            foreach (var w in waves)
            {
                if (w != null)
                {
                    w.interval = Mathf.Max(0.05f, w.interval);
                    w.spawnJitter = Mathf.Clamp(w.spawnJitter, 0f, 1.0f);
                }
            }
        }
        if (interval > 0f) interval = Mathf.Max(0.05f, interval);
    }

    public IEnumerator RunWave(int waveIndex)
    {
        if (waves == null || waves.Count == 0)
        {
            Debug.LogWarning("[TargetSpawner] No waves configured. Set Waves > Size in Inspector.");
            yield break;
        }
        if (waveIndex < 0 || waveIndex >= waves.Count)
        {
            Debug.LogWarning($"[TargetSpawner] waveIndex {waveIndex} out of range 0..{waves.Count - 1}");
            yield break;
        }

        var w = waves[waveIndex];
        if (w.entries == null || w.entries.Count == 0)
        {
            Debug.LogWarning($"[TargetSpawner] Wave {waveIndex} has 0 entries.");
            yield break;
        }

        // Build an interleaving bag: we’ll randomly pick which entry to spawn each time
        var bag = new List<(SpawnEntry e, int remaining)>();
        foreach (var e in w.entries)
        {
            if (e.prefab == null)
            {
                Debug.LogWarning($"[TargetSpawner] Wave {waveIndex} has an entry with NO prefab assigned.");
                continue;
            }
            if (e.count > 0) bag.Add((e, e.count));
        }
        if (bag.Count == 0)
        {
            Debug.LogWarning($"[TargetSpawner] Wave {waveIndex} has no valid entries (prefabs missing or counts 0).");
            yield break;
        }

        // Spawn loop (interleaves all entries)
        while (bag.Count > 0)
        {
            int i = Random.Range(0, bag.Count);
            var (entry, remaining) = bag[i];

            SpawnOne(entry);

            remaining--;
            if (remaining <= 0) bag.RemoveAt(i);
            else bag[i] = (entry, remaining);

            // Compute wait using global override if set; else wave's own timing
            float baseInterval = (interval > 0f) ? interval : w.interval;
            float wait = baseInterval + Random.Range(-w.spawnJitter, w.spawnJitter);
            if (wait < 0.05f) wait = 0.05f;

            yield return new WaitForSeconds(wait);
        }

        // Wait for all active targets to despawn before ending the wave
        while (GameManager.I != null && GameManager.I.ActiveTargets > 0)
            yield return null;
    }

    private void SpawnOne(SpawnEntry e)
    {
        if (e.prefab == null) return;

        Vector3 pos = new Vector3(
            Random.Range(e.areaMin.x, e.areaMax.x),
            Random.Range(e.areaMin.y, e.areaMax.y),
            0f
        );

        // Instantiate a fresh target (no pooling for simplicity)
        var t = Instantiate(e.prefab, pos, Quaternion.identity);

        // Hook despawn callback so GameManager.ActiveTargets stays accurate
        t.OnDespawn = OnTargetDespawn;

        if (GameManager.I != null)
            GameManager.I.ActiveTargets++;
    }

    private void OnTargetDespawn(Target t)
    {
        if (GameManager.I != null)
            GameManager.I.ActiveTargets--;
        // target deactivates itself; nothing else needed
    }
}

// TargetSpawner.cs — simple, interleaved, no pooling (robust wave-end)
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
    public float interval = -1f;   // <= 0 means 'use wave interval'

    void OnValidate()
    {
        if (waves != null)
        {
            foreach (var w in waves)
            {
                if (w != null)
                {
                    w.interval = Mathf.Max(0.05f, w.interval);
                    w.spawnJitter = Mathf.Clamp(w.spawnJitter, 0f, 1f);
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

        // Build an interleaving bag
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

        Debug.Log($"[Spawner] Wave {waveIndex}: will spawn total {TotalCount(bag)} items across {bag.Count} entry types.");

        // ----- Local alive counter for THIS wave -----
        int aliveThisWave = 0;

        // Spawn loop (interleaves all entries)
        while (bag.Count > 0)
        {
            int i = Random.Range(0, bag.Count);
            var tuple = bag[i];
            var spawnEntry = tuple.e;
            int remaining = tuple.remaining;

            var t = SpawnOne(spawnEntry);
            if (t != null)
            {
                aliveThisWave++;

                // Hook OnDespawn to BOTH: local counter and GameManager
                t.OnDespawn = (target) =>
                {
                    aliveThisWave = Mathf.Max(0, aliveThisWave - 1);
                    if (GameManager.I != null)
                        GameManager.I.ActiveTargets = Mathf.Max(0, GameManager.I.ActiveTargets - 1);
                };

                if (GameManager.I != null) GameManager.I.ActiveTargets++;

                Debug.Log($"[Spawner] Spawned {t.name} at {t.transform.position}");
            }

            remaining--;
            if (remaining <= 0) bag.RemoveAt(i);
            else bag[i] = (spawnEntry, remaining);

            // Compute wait using global override if set; else wave's own timing
            float baseInterval = (interval > 0f) ? interval : w.interval;
            float wait = baseInterval + Random.Range(-w.spawnJitter, w.spawnJitter);
            if (wait < 0.05f) wait = 0.05f;

            yield return new WaitForSeconds(wait);
        }

        // If nothing spawned, end immediately
        if (aliveThisWave <= 0)
        {
            Debug.Log($"[Spawner] Wave {waveIndex} spawned 0 targets; ending wave.");
            yield break;
        }

        // Wait for all locally-tracked targets to despawn before ending the wave (with safety timeout)
        float waveWaitStart = Time.realtimeSinceStartup;
        while (aliveThisWave > 0)
        {
            if (Time.realtimeSinceStartup - waveWaitStart > 10f)
            {
                Debug.LogWarning("[TargetSpawner] Timeout waiting for wave targets to clear; forcing advance.");
                aliveThisWave = 0;
                break;
            }
            yield return null;
        }
    }


    private Target SpawnOne(SpawnEntry e)
    {
        if (e.prefab == null) return null;

        Vector3 pos = new Vector3(
            Random.Range(e.areaMin.x, e.areaMax.x),
            Random.Range(e.areaMin.y, e.areaMax.y),
            0f
        );

        // Instantiate a fresh target (no pooling for simplicity)
        var t = Instantiate(e.prefab, pos, Quaternion.identity);

        return t;
    }

    int TotalCount(List<(SpawnEntry e, int remaining)> b)
    {
        int sum = 0;
        foreach (var it in b) sum += it.remaining;
        return sum;
    }

    void OnDisable()
    {
        StopAllCoroutines();
    }

    void OnDestroy()
    {
        StopAllCoroutines();
    }
}

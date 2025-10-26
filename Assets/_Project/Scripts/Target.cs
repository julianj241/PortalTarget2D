// Target.cs (Base class for any clickable target)
using UnityEngine;


public class Target : MonoBehaviour, IClickable
{
    [Header("Target Settings")] public int scoreValue = 100; public float lifeSeconds = 2.0f;
    [Tooltip("When true, missing this counts as a miss against accuracy but not a life.")] public bool softMissOnly = false;


    protected bool alive = true; float t;
    public System.Action<Target> OnDespawn; // assigned by spawner/pool


    protected virtual void OnEnable() { alive = true; t = 0f; }
    protected virtual void Update()
    {
        t += Time.deltaTime; if (t >= lifeSeconds) { Timeout(); }
    }
    protected virtual void Timeout() { if (!softMissOnly) GameManager.I.RegisterMiss(); Despawn(); }


    public virtual void OnClicked(bool wasAccurate)
    {
        if (!alive) return; alive = false;
        GameManager.I.RegisterHit(scoreValue, wasAccurate);
        FX.SpawnHit(transform.position);
        Despawn();

       

    }


    protected virtual void Despawn() { OnDespawn?.Invoke(this); gameObject.SetActive(false); }


}
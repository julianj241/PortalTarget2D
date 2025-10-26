// ClickShooter.cs (attach to a singleton object in each Game scene)
using UnityEngine;


public class ClickShooter : MonoBehaviour
{
    [Header("Crosshair")] public RectTransform crosshairUI; // optional
    Camera cam;
    [SerializeField] float crosshairPulse = 1.12f;
    [SerializeField] float crosshairRecover = 12f; // higher = snappier
    Vector3 crosshairBase = Vector3.one;

    void Start()
    {
        if (crosshairUI) crosshairBase = crosshairUI.localScale;
    }


    void Awake() {
        cam = Camera.main;
        Cursor.visible = crosshairUI == null;
    }


    void Update()
    {
        Vector3 m = Input.mousePosition;
        if (crosshairUI) crosshairUI.position = m;

        if (Input.GetKeyDown(KeyCode.Escape))
            GameManager.I.TogglePause();

        if (GameManager.I.IsPaused) return;   // blocks shooting while paused

        if (Input.GetMouseButtonDown(0))
            TryShoot(m);

        if (Input.GetKeyDown(KeyCode.R))
            GameManager.I.TryReload();

        if(crosshairUI) crosshairUI.localScale = Vector3.Lerp(
       crosshairUI.localScale, crosshairBase, Time.unscaledDeltaTime * crosshairRecover);
    }



    void TryShoot(Vector3 mousePos)
    {
        Vector3 world = cam.ScreenToWorldPoint(mousePos);
        world.z = 0f;

        // Hit ALL colliders at the point (handles child colliders, stacked objects, etc.)
        var hits = Physics2D.OverlapPointAll(world);
        AudioHub.PlayShoot();

        
        var hit = Physics2D.OverlapPoint(world); // (Optional: switch to OverlapPoint)
        AudioHub.PlayShoot();

       

        if (hits != null && hits.Length > 0)
        {
            for (int i = 0; i < hits.Length; i++)
            {
                // Look on the object, then its parents—covers cases where the collider is on a child
                var clickable = hits[i].GetComponent<IClickable>() ?? hits[i].GetComponentInParent<IClickable>();
                if (clickable != null)
                {
                    // Debug.Log("HIT " + hits[i].name); // (optional) helps verify
                    clickable.OnClicked(true);
                    return;
                }
            }
        }

        if (crosshairUI) crosshairUI.localScale = crosshairBase * crosshairPulse;

        if (hit)
        {
            var clickable = hit.GetComponent<IClickable>();
            if (clickable != null) { clickable.OnClicked(true); return; }
        }

        // No clickable found at that point → count as a miss
        GameManager.I.RegisterMiss();
        AudioHub.PlayMiss();
    }


}
using UnityEngine;

public class ClickShooter : MonoBehaviour
{
    [Header("Crosshair (optional)")]
    public RectTransform crosshairUI;

    [Header("Hit Detection")]
    [SerializeField] private LayerMask targetMask = ~0; // default: everything (you can set "Targets" in Inspector)

    private Camera cam;

    void Awake()
    {
        cam = Camera.main;
        // Hide system cursor if we’re drawing our own
        if (crosshairUI != null)
        {
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Confined;
        }
        else
        {
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }
    }

    void Update()
    {
        // Scenes can swap cameras; keep this robust
        if (cam == null || !cam.isActiveAndEnabled) cam = Camera.main;

        Vector3 m = Input.mousePosition;

        // Move UI crosshair (simple version; works for Screen Space - Overlay)
        if (crosshairUI) crosshairUI.position = m;

        if (GameManager.I.IsPaused) return;

        if (Input.GetMouseButtonDown(0))
        {
            TryShoot(m);
        }

        if (Input.GetKeyDown(KeyCode.R)) GameManager.I.TryReload();
        if (Input.GetKeyDown(KeyCode.Escape)) GameManager.I.TogglePause();
    }

    void TryShoot(Vector3 mousePos)
    {
        if (!cam) return;

        Vector3 world = cam.ScreenToWorldPoint(mousePos);
        world.z = 0f;

        AudioHub.PlayShoot();

        // 1) Preferred: masked hit
        Collider2D hitCol = Physics2D.OverlapPoint((Vector2)world, targetMask);
        if (TryHandleHit(hitCol, masked: true)) return;

        // 2) Fallback: try all layers in case the collider is mis-layered
        var allHits = Physics2D.OverlapPointAll((Vector2)world);
        for (int i = 0; i < allHits.Length; i++)
        {
            if (TryHandleHit(allHits[i], masked: false)) return;
        }

        // Miss
        GameManager.I.RegisterMiss();
        AudioHub.PlayMiss();
    }

    bool TryHandleHit(Collider2D col, bool masked)
    {
        if (col == null) return false;

        var clickable = col.GetComponentInParent<IClickable>();
        if (clickable != null)
        {
#if UNITY_EDITOR
            string layerName = LayerMask.LayerToName(col.gameObject.layer);
            if (masked && ((1 << col.gameObject.layer) & targetMask.value) == 0)
            {
                Debug.LogWarning($"[ClickShooter] Hit '{col.name}' on layer '{layerName}' but it's OUTSIDE targetMask. Consider fixing layer.");
            }
            else
            {
                Debug.Log($"[ClickShooter] Hit '{col.name}' (layer '{layerName}') via {(masked ? "masked" : "fallback")} path.");
            }
#endif
            clickable.OnClicked(true);
            return true;
        }
        return false;
    }
}

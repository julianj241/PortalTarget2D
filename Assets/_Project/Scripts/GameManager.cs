// GameManager.cs
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;
using TMPro;

public class GameManager : MonoBehaviour
{
    public static GameManager I;

    void Awake()
    {
        if (I != null) { Destroy(gameObject); return; }
        I = this;
        DontDestroyOnLoad(gameObject);
    }

    [Header("Pause UI")]
    public GameObject pausePanel; // drag per scene (via HUDRefs or manually)

    [Header("UI (TMP)")]
    public TMP_Text scoreText, multiplierText, livesText, waveText;

    [Header("Rules")]
    public int startingLives = 3;
    public float spawnAccelPerWave = 0.1f;  // only used if your spawner honors its global interval

    // -------- Portal reveal control (per-scene, set via SceneConfig) ----------
    [Header("Portal Reveal")]
    [SerializeField] private int revealPortalAfterWave = -1;     // -1 = after all waves; 1 = after wave 1; etc.
    [SerializeField] private GameObject portalGO;                // scene-placed (disabled) portal (optional)
    [SerializeField] private PortalTarget portalPrefab;          // prefab to spawn if no scene portal present (optional)
    [SerializeField] private Transform portalSpawnPoint;         // spawn location if using prefab (optional)
    [SerializeField] private string nextSceneName;               // where portal should go (e.g., Game_Scene3)

    // -------------------- Runtime state --------------------
    int score, lives, chain;
    float multiplier = 1f;
    int waveIndex;

    public bool IsPaused { get; private set; }
    public int ActiveTargets { get; set; }

    // -------------------- Unity lifecycle ------------------
    void Start()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;

        // Handle the already-loaded scene once (Editor play from any scene)
        var sc = SceneManager.GetActiveScene();
        OnSceneLoaded(sc, LoadSceneMode.Single);
    }

    void OnDestroy()
    {
        if (I == this) SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnSceneLoaded(Scene sc, LoadSceneMode m)
    {
        Time.timeScale = 1f;
        IsPaused = false;
        if (pausePanel) pausePanel.SetActive(false);

        ActiveTargets = 0;

        // Reset portal state for the new scene
        portalRevealed = false;
        portalGO = null;

        BindUI();
        ResetRunIfNeeded();

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // Apply per-scene config first (may set portalGO/Prefab/nextSceneName)
        var cfg = Object.FindObjectOfType<SceneConfig>(true);
        ApplySceneConfig(cfg);

        // ✅ Fallback: if still no portalGO, find a PortalTarget even if it's INACTIVE
        if (portalGO == null)
        {
            var pt = Object.FindObjectOfType<PortalTarget>(true); // 'true' includes inactive
            if (pt) portalGO = pt.gameObject;
        }

        // (Optional) last-ditch: try active tagged portal (won’t find inactive)
        if (portalGO == null)
        {
            var taggedPortal = GameObject.FindWithTag("Portal");
            if (taggedPortal) portalGO = taggedPortal;
        }

        if (IsGameScene(sc.name))
            StartCoroutine(RunScene());
    }


    // -------------------- Scene helpers --------------------
    bool IsGameScene(string name) => name.StartsWith("Game_");

    void ResetRunIfNeeded()
    {
        if (lives <= 0)
        {
            lives = startingLives;
            score = 0;
            chain = 0;
            multiplier = 1f;
            waveIndex = 0;
        }
        UpdateUI();
    }

    void BindUI()
    {
        // If you have a HUDRefs helper in each game scene, use it.
        var hud = Object.FindObjectOfType<HUDRefs>(true);

        if (!hud)
        {
            // Title/FinalTally scenes likely don't have HUD
            scoreText = multiplierText = livesText = waveText = null;
            pausePanel = null;
            return;
        }

        scoreText = hud.scoreText ? hud.scoreText.GetComponent<TMP_Text>() : null;
        multiplierText = hud.multiplierText ? hud.multiplierText.GetComponent<TMP_Text>() : null;
        livesText = hud.livesText ? hud.livesText.GetComponent<TMP_Text>() : null;
        waveText = hud.waveText ? hud.waveText.GetComponent<TMP_Text>() : null;
        pausePanel = hud.pausePanel;

        if (pausePanel) pausePanel.SetActive(false);
        UpdateUI();

        // Optional: wire HUD buttons
        if (hud.resumeButton)
        {
            hud.resumeButton.onClick.RemoveAllListeners();
            hud.resumeButton.onClick.AddListener(ResumeGame);
        }
        if (hud.menuButton)
        {
            hud.menuButton.onClick.RemoveAllListeners();
            hud.menuButton.onClick.AddListener(GoToMenu);
        }
    }

    // -------------------- Core loop ------------------------
    IEnumerator RunScene()
    {
        var spawner = Object.FindObjectOfType<TargetSpawner>();
        if (spawner == null || spawner.waves == null || spawner.waves.Count == 0)
        {
            Debug.LogWarning("[GM] No spawner or no waves in this scene; revealing portal.");
            yield return new WaitForSecondsRealtime(0.2f);
            RevealPortal();
            yield break;
        }

        int wavesThisScene = spawner.waves.Count; // run exactly what's configured per scene
        Debug.Log($"[GM] {SceneManager.GetActiveScene().name}: Running {wavesThisScene} wave(s)");

        for (int i = 0; i < wavesThisScene; i++)
        {
            waveIndex = i + 1;
            UpdateUI();
            Debug.Log($"[GM] Starting Wave {waveIndex}");
            yield return spawner.RunWave(i);
            Debug.Log($"[GM] Finished Wave {waveIndex}. ActiveTargets={ActiveTargets}");

            // Reveal portal after finishing a specific wave (if set via SceneConfig)
            if (revealPortalAfterWave > 0 && waveIndex == revealPortalAfterWave)
            {
                Debug.Log($"[GM] Wave {waveIndex} finished -> revealing portal per SceneConfig.");
                yield return new WaitForSecondsRealtime(0.4f);
                RevealPortal();
            }

            // Optional: adjust spawner's global override pacing
            if (spawner.interval > 0f)
                spawner.interval = Mathf.Max(0.05f, spawner.interval * (1f - spawnAccelPerWave));
        }

        // If not revealed mid-run, reveal after all waves
        if (revealPortalAfterWave <= 0)
        {
            yield return new WaitForSecondsRealtime(0.4f);
            RevealPortal();
        }
    }

    // -------------------- Scoring & state ------------------
    public void RegisterHit(int baseScore, bool accurate)
    {
        chain++;
        multiplier = 1f + (chain / 5f);
        int add = Mathf.RoundToInt(baseScore * multiplier);
        score += add;
        AudioHub.PlayHit();
        UpdateUI();
    }

    public void RegisterMiss()
    {
        chain = 0; multiplier = 1f; lives--;
        AudioHub.PlayMiss();
        UpdateUI();
        if (lives <= 0) SceneFlow.ToFinalTally(score);
    }

    public void RegisterHazard()
    {
        chain = 0; multiplier = 1f; lives--;
        AudioHub.PlayHazard();
        UpdateUI();
        if (lives <= 0) SceneFlow.ToFinalTally(score);
    }

    public void TryReload() { /* optional */ }

    // -------- Public pause APIs (use these from UI scripts) --------
    public void TogglePause()
    {
        SetPaused(!IsPaused);
    }

    public void ResumeGame()
    {
        SetPaused(false);
    }

    public void SetPaused(bool paused)
    {
        IsPaused = paused;
        Time.timeScale = paused ? 0f : 1f;
        if (pausePanel) pausePanel.SetActive(paused);
        Cursor.visible = paused;
        Cursor.lockState = paused ? CursorLockMode.None : CursorLockMode.Confined;

        if (AudioHub.I && AudioHub.I.bgm)
            AudioHub.I.bgm.volume = paused ? 0.25f : 1f;
    }

    public void GoToMenu()
    {
        SetPaused(false);
        SceneFlow.LoadScene("Title");
    }

    void UpdateUI()
    {
        if (scoreText) scoreText.text = $"Score: {score}";
        if (multiplierText) multiplierText.text = $"x{multiplier:0.0}";
        if (livesText) livesText.text = $"Lives: {lives}";
        if (waveText) waveText.text = $"Wave {waveIndex}";
    }

    // -------------------- SceneConfig hookup ----------------
    public void ApplySceneConfig(SceneConfig cfg)
    {
        if (cfg == null) return;

        // Per-scene override settings
        this.revealPortalAfterWave = cfg.revealPortalAfterWave;
        this.nextSceneName = cfg.nextSceneName;

        if (cfg.portalGO != null) this.portalGO = cfg.portalGO;
        if (cfg.portalPrefab != null) this.portalPrefab = cfg.portalPrefab;
        if (cfg.portalSpawnPoint != null) this.portalSpawnPoint = cfg.portalSpawnPoint;

        // If still no scene-placed portal, try tag
        if (this.portalGO == null)
        {
            var tagged = GameObject.FindWithTag("Portal");
            if (tagged) this.portalGO = tagged;
        }

        string portalName = portalGO ? portalGO.name : "null";
        string prefabName = portalPrefab ? portalPrefab.name : "null";

        Debug.Log($"[GM] Applied SceneConfig: revealAfterWave={revealPortalAfterWave}, next='{nextSceneName}', " +
                  $"portalGO={portalName}, portalPrefab={prefabName}");
    }

    // -------------------- Portal reveal (robust) ------------
    private bool portalRevealed;

    private void RevealPortal()
    {
        if (portalRevealed) return;
        portalRevealed = true;

        // 1) Enable a scene-placed portal if present
        if (portalGO != null)
        {
            var pt = portalGO.GetComponent<PortalTarget>();
            if (pt != null && !string.IsNullOrEmpty(nextSceneName)) pt.nextScene = nextSceneName;
            portalGO.SetActive(true);
            Debug.Log("[GM] Revealed scene-placed portal.");
            return;
        }

        // 2) Try to find a portal by tag and enable it
        var tagged = GameObject.FindWithTag("Portal");
        if (tagged != null)
        {
            var pt = tagged.GetComponent<PortalTarget>();
            if (pt != null && !string.IsNullOrEmpty(nextSceneName)) pt.nextScene = nextSceneName;
            tagged.SetActive(true);
            Debug.Log("[GM] Revealed portal found by tag.");
            return;
        }

        // 3) Spawn from prefab if provided
        if (portalPrefab != null)
        {
            Vector3 pos = portalSpawnPoint ? portalSpawnPoint.position : Vector3.zero;
            var spawned = Instantiate(portalPrefab, pos, Quaternion.identity);
            if (!string.IsNullOrEmpty(nextSceneName)) spawned.nextScene = nextSceneName;
            Debug.Log("[GM] Spawned portal from prefab.");
            return;
        }

        Debug.LogWarning("[GM] No portalGO, no Portal tag in scene, and no portalPrefab set. Cannot reveal portal.");
    }
}

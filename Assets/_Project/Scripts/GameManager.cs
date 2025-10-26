// GameManager.cs
using UnityEngine;
using UnityEngine.UI;            // for Text
using UnityEngine.SceneManagement;
using System.Collections;        // for IEnumerator / Coroutines
using TMPro;





public class GameManager : MonoBehaviour
{
    public static GameManager I; void Awake() { if (I != null) { Destroy(gameObject); return; } I = this; DontDestroyOnLoad(gameObject); }


    [Header("Pause UI")]
    public GameObject pausePanel; // drag your PausePanel here in each scene

    [Header("UI")] public TMP_Text scoreText, multiplierText, livesText, waveText;
    [Header("Rules")] public int startingLives = 3; public int wavesPerScene = 3; public float spawnAccelPerWave = 0.1f;

    [SerializeField] private GameObject portalGO; // assign per scene in Inspector

    int score, lives, chain;
    float multiplier = 1f;
    int waveIndex;
    public bool IsPaused{
        get; set;
    }
    public int ActiveTargets{
        get; set;
    }



    void Start()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;

        // Handle the already-loaded scene once (cleanly):
        var sc = SceneManager.GetActiveScene();
        OnSceneLoaded(sc, LoadSceneMode.Single);
    }


    void OnSceneLoaded(Scene sc, LoadSceneMode m)
    {
        // Always restore normal time on a new scene
        Time.timeScale = 1f;
        IsPaused = false;

        BindUI();
        ResetRunIfNeeded();

        // Fresh scene has its own pause panel; if you pass it via a binder, it will override here.
        if (pausePanel) pausePanel.SetActive(false);

        // Cursor policy for 2D: no lock; visible only if you want OS cursor (otherwise hide it if you use a crosshair UI)
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;   // <— keep OS cursor during gameplay

        if (IsGameScene(sc.name))
            StartCoroutine(RunScene());
    }



    void ResetRunIfNeeded() { if (lives <= 0) { lives = startingLives; score = 0; chain = 0; multiplier = 1f; waveIndex = 0; } UpdateUI(); }


    bool IsGameScene(string name) { return name.StartsWith("Game_"); }


    IEnumerator RunScene()
    {
        var spawner = FindObjectOfType<TargetSpawner>(); for (int i = 0; i < wavesPerScene; i++) { waveIndex = i + 1; UpdateUI(); yield return spawner.RunWave(i); spawner.interval = Mathf.Max(0.25f, spawner.interval * (1f - spawnAccelPerWave)); }
        // After waves, wait a moment then show portal if scene expects it OR auto‑portal via a prefab placed in scene.
        yield return new WaitForSeconds(0.4f); // tiny beat so the end feels clear
        RevealPortal();

    }


    public void RegisterHit(int baseScore, bool accurate) { chain++; multiplier = 1f + (chain / 5f); int add = Mathf.RoundToInt(baseScore * multiplier); score += add; AudioHub.PlayHit(); UpdateUI(); }


    public void RegisterMiss() { chain = 0; multiplier = 1f; lives--; AudioHub.PlayMiss(); UpdateUI(); if (lives <= 0) SceneFlow.ToFinalTally(score); }


    public void RegisterHazard() { chain = 0; multiplier = 1f; lives--; AudioHub.PlayHazard(); UpdateUI(); if (lives <= 0) SceneFlow.ToFinalTally(score); }


    public void TryReload() { /* optional: play reload SFX + brief input lock */ }


    public void TogglePause()
    {
        IsPaused = !IsPaused;
        Time.timeScale = IsPaused ? 0f : 1f;

        // Show or hide the pause panel (if assigned)
        if (pausePanel) pausePanel.SetActive(IsPaused);

        // Toggle the mouse cursor
        Cursor.visible = IsPaused; // show while paused, hide while playing
        Cursor.lockState = IsPaused ? CursorLockMode.None : CursorLockMode.Confined;

        // Optional: adjust BGM volume when paused
        if (AudioHub.I && AudioHub.I.bgm)
            AudioHub.I.bgm.volume = IsPaused ? 0.25f : 1f;
    }



    void UpdateUI() { if (scoreText) scoreText.text = $"Score: {score}"; if (multiplierText) multiplierText.text = $"x{multiplier:0.0}"; if (livesText) livesText.text = $"Lives: {lives}"; if (waveText) waveText.text = $"Wave {waveIndex}"; }

    void BindUI()
    {
        // Find HUD (even if some parts start inactive)
        var hud = Object.FindObjectOfType<HUDRefs>(true);

        // Reset time & pause state on scene load
        IsPaused = false;
        Time.timeScale = 1f;

        if (!hud)
        {
            // Title/FinalTally scenes likely don't have a HUD
            scoreText = multiplierText = livesText = waveText = null;
            pausePanel = null;
            return;
        }

        // Assign text + panel refs
        // Assign text + panel refs (adapt legacy Text -> TMP_Text)
        scoreText = hud.scoreText ? hud.scoreText.GetComponent<TMP_Text>() : null;
        multiplierText = hud.multiplierText ? hud.multiplierText.GetComponent<TMP_Text>() : null;
        livesText = hud.livesText ? hud.livesText.GetComponent<TMP_Text>() : null;
        waveText = hud.waveText ? hud.waveText.GetComponent<TMP_Text>() : null;

        pausePanel = hud.pausePanel;


        if (pausePanel) pausePanel.SetActive(false);
        UpdateUI();

        // --- (RE)WIRE BUTTONS SAFELY ---
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
    public void ResumeGame()
    {
        if (!IsPaused) return;
        TogglePause();
    }

    public void GoToMenu()
    {
        // Ensure time is flowing again
        Time.timeScale = 1f;
        IsPaused = false;
        if (pausePanel) pausePanel.SetActive(false);
        SceneFlow.LoadScene("Title");
    }

    private bool portalRevealed;
    private void RevealPortal()
    {
        if (portalRevealed) return;
        portalRevealed = true;
        if (portalGO) portalGO.SetActive(true);
    }


}
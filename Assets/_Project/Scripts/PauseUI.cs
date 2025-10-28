// PauseUI.cs
using UnityEngine;
using UnityEngine.UI;

public class PauseUI : MonoBehaviour
{
    [Header("Pause Menu References")]
    public GameObject pausePanel;       // Assign your Pause Panel in the Inspector
    public Button resumeButton;         // Assign Resume Button
    public Button menuButton;           // Assign Menu Button
    public Button quitButton;           // Optional: Assign Quit Button

    void Start()
    {
        // Ensure the pause panel starts hidden
        if (pausePanel != null)
            pausePanel.SetActive(false);

        // Wire buttons safely
        if (resumeButton != null)
        {
            resumeButton.onClick.RemoveAllListeners();
            resumeButton.onClick.AddListener(() =>
            {
                GameManager.I.ResumeGame(); // ✅ Unpauses properly
            });
        }

        if (menuButton != null)
        {
            menuButton.onClick.RemoveAllListeners();
            menuButton.onClick.AddListener(() =>
            {
                GameManager.I.GoToMenu(); // ✅ Goes back to Title scene
            });
        }

        if (quitButton != null)
        {
            quitButton.onClick.RemoveAllListeners();
            quitButton.onClick.AddListener(() =>
            {
                Application.Quit(); // ✅ Optional: quits the game build
            });
        }
    }

    void Update()
    {
        // Listen for the Escape key to toggle pause
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            GameManager.I.TogglePause(); // ✅ Uses GameManager’s internal pause logic
            if (pausePanel != null)
                pausePanel.SetActive(GameManager.I.IsPaused);
        }
    }
}

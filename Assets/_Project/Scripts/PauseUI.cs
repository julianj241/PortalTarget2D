using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PauseUI : MonoBehaviour
{
    [Header("Refs")]
    public Button resumeButton;
    public Button menuButton;

    void Awake()
    {
        // Safety: ensure panel starts hidden (GameManager will show it)
        gameObject.SetActive(false);

        // Clear and (re)wire listeners so duplicates never happen
        if (resumeButton != null)
        {
            resumeButton.onClick.RemoveAllListeners();
            resumeButton.onClick.AddListener(OnResumeClicked);
        }

        if (menuButton != null)
        {
            menuButton.onClick.RemoveAllListeners();
            menuButton.onClick.AddListener(OnMenuClicked);
        }
    }

    void OnResumeClicked()
    {
        if (GameManager.I != null)
        {
            Debug.Log("[PauseUI] Resume clicked");
            GameManager.I.ResumeGame();
        }
        else
        {
            Debug.LogWarning("[PauseUI] GameManager not found");
        }
    }

    void OnMenuClicked()
    {
        if (GameManager.I != null)
        {
            Debug.Log("[PauseUI] Menu clicked");
            // Ensure time flow resumes before swapping scenes
            Time.timeScale = 1f;
            GameManager.I.IsPaused = false;
            GameManager.I.TogglePause(); // will hide panel and reset state
            SceneFlow.LoadScene("Title");
        }
        else
        {
            Debug.LogWarning("[PauseUI] GameManager not found");
        }
    }
}

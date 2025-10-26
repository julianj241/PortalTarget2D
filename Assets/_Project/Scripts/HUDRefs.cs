using UnityEngine;      // <-- Needed for MonoBehaviour, GameObject, etc.
using UnityEngine.UI;   // <-- Needed for Button
using TMPro;            // <-- Needed for TMP_Text

public class HUDRefs : MonoBehaviour
{
    public TMP_Text scoreText;
    public TMP_Text multiplierText;
    public TMP_Text livesText;
    public TMP_Text waveText;
    public GameObject pausePanel;
    public Button resumeButton;
    public Button menuButton;
}

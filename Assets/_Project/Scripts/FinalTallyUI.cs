// FinalTallyUI.cs (for TextMeshPro)
using UnityEngine;
using TMPro;   // ← important change

public class FinalTallyUI : MonoBehaviour
{
    public TMP_Text scoreText;   // ← use TMP_Text instead of Text

    void Start()
    {
        int s = PlayerPrefs.GetInt("finalScore", 0);
        if (scoreText)
            scoreText.text = $"Final Score: {s}";
    }

    // Button hooks (optional)
    public void OnRestart() => SceneFlow.LoadScene("Game_Scene1");
    public void OnMenu() => SceneFlow.LoadScene("Title");
}

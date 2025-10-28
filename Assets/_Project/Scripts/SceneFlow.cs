// SceneFlow.cs
using UnityEngine.SceneManagement;
using UnityEngine;
public static class SceneFlow
{
    public static void LoadScene(string name)
    {
        Time.timeScale = 1f;

        // 🔴 Ensure no gameplay coroutines survive the hop
        if (GameManager.I != null)
            GameManager.I.AbortAllGameplay();

        SceneManager.LoadScene(name);
    }

    public static void ToFinalTally(int finalScore)
    {
        PlayerPrefs.SetInt("finalScore", finalScore);
        LoadScene("FinalTally");
    }
}

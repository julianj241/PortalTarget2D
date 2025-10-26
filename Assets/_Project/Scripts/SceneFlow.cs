// SceneFlow.cs
using UnityEngine.SceneManagement;
using UnityEngine;
public static class SceneFlow
{
    public static void LoadScene(string name) { Time.timeScale = 1f; SceneManager.LoadScene(name); }
    public static void ToFinalTally(int finalScore) { PlayerPrefs.SetInt("finalScore", finalScore); LoadScene("FinalTally"); }
}


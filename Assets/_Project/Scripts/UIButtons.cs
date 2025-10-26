using UnityEngine;

public class UIButtons : MonoBehaviour
{
    public void RestartGame()
    {
        SceneFlow.LoadScene("Game_Scene1");
    }

    public void GoToMenu()
    {
        SceneFlow.LoadScene("Title");
    }
}

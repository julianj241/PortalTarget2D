using UnityEngine;

public class TitleButtons : MonoBehaviour
{
    public void PlayGame()
    {
        SceneFlow.LoadScene("Game_Scene1");
    }

    public void QuitGame()
    {
        // Works both in the Editor and in a built player
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }


    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Return)) PlayGame();
        if (Input.GetKeyDown(KeyCode.Escape)) QuitGame();
    }

}

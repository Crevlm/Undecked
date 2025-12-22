using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    [SerializeField] private string gameScreenName = "GameScene";


    // Start is called once before the first execution of Update after the MonoBehaviour is created
   public void StartGame()
    {
        SceneManager.LoadScene(gameScreenName);
    }

    public void QuitGame()
    {
        Application.Quit();

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }

}

using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneButtonLoader : MonoBehaviour
{
    [SerializeField] private string sceneName;

    public void LoadConfiguredScene()
    {
        if (string.IsNullOrWhiteSpace(sceneName))
        {
            Debug.LogWarning($"[{nameof(SceneButtonLoader)}] Scene name is empty on {gameObject.name}.", this);
            return;
        }

        SceneManager.LoadScene(sceneName);
    }

    public void LoadMainMenu()
    {
        SceneManager.LoadScene("MainMenu");
    }

    public void LoadControls()
    {
        SceneManager.LoadScene("Controls");
    }

    public void LoadPowerUp()
    {
        SceneManager.LoadScene("Power-up");
    }

    public void LoadGameSetting()
    {
        SceneManager.LoadScene("GameSetting");
    }
}

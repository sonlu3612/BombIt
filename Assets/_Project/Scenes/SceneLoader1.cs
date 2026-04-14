using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoader1 : MonoBehaviour
{
    public void LoadGameScene()
    {
        SceneManager.LoadScene("Controls");
    }
}
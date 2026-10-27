using UnityEngine;
using UnityEngine.SceneManagement;

namespace CookingSystem.UI
{
    /// <summary>
    /// Helper component to trigger scene transitions from UI button OnClick events.
    /// </summary>
    public class SceneLoader : MonoBehaviour
    {
        public void LoadScene(string sceneName)
        {
            if (string.IsNullOrEmpty(sceneName)) return;
            SceneManager.LoadScene(sceneName);
        }

        public void LoadSceneAsync(string sceneName)
        {
            if (string.IsNullOrEmpty(sceneName)) return;
            SceneManager.LoadSceneAsync(sceneName);
        }

        public void LoadMainMenu()
        {
            SceneManager.LoadScene("mainmenuscene");
        }
    }
}

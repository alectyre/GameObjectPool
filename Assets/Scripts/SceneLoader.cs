using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoader : MonoBehaviour
{
    [SerializeField] private bool loadAdditively;

    [SerializeField] public bool LoadAdditively { get { return loadAdditively; } set { loadAdditively = value; } }

    public void LoadScene(int sceneIndex)
    {
        if (SceneManager.GetSceneByBuildIndex(sceneIndex).isLoaded)
            SceneManager.UnloadSceneAsync(sceneIndex);
        else
            SceneManager.LoadSceneAsync(sceneIndex, LoadAdditively ? LoadSceneMode.Additive : LoadSceneMode.Single);
    }
}

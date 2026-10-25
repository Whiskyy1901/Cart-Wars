using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class MainMenu : MonoBehaviour
{
    #if UNITY_EDITOR
    [SerializeField] private SceneAsset gameScene;
    [SerializeField] private SceneAsset creditsScene;
    #endif
 
    [Header("Scene names")]
    [SerializeField] private string _gameSceneName;
    [SerializeField] private string _Whiskyurl = "https://";
    [SerializeField] private string _KSFUrl = "https://";

    public Animator transition; 
    public float transitionTime = 1f;

    #if UNITY_EDITOR
    private void OnValidate()
    {
        if (gameScene != null) _gameSceneName = gameScene.name;
    }
    #endif
    
    IEnumerator LoadLevel()
    {
        if (transition != null)
            transition.SetTrigger("Start");
 
        yield return new WaitForSeconds(transitionTime);
 
        LoadScene(_gameSceneName);
    }

    public void Whisky()
    {
        if (!string.IsNullOrWhiteSpace(_Whiskyurl))
            Application.OpenURL(_Whiskyurl);
    }
    public void KSF()
    {
        if (!string.IsNullOrWhiteSpace(_KSFUrl))
            Application.OpenURL(_KSFUrl);
    }

    public void OnStart()
    {
        LoadScene(_gameSceneName);
    }
    
    public void OnQuit()
    {
        Application.Quit();
    }
    
    private void LoadScene(string sceneName)
    {
        SceneManager.LoadScene(sceneName);
    }
}
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class PauseMenu : MonoBehaviour
{
    [SerializeField] private GameObject _pausePanel;
    private bool _isPaused;

    private void Start()
    {
        _pausePanel.SetActive(false);
    }

    public void Restart()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void MainMenu()
    {
        SceneManager.LoadScene(0);
    }

    void Pause()
    {
        _pausePanel.SetActive(true);
        
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.Confined;
        
        Time.timeScale = 0;

        _isPaused = true;
    }
    
    void Unpause()
    {
        _pausePanel.SetActive(false);
        
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;

        Time.timeScale = 1;

        _isPaused = false;
    }

    private void Update()
    {
        if(Keyboard.current.pKey.wasPressedThisFrame && !_isPaused)
            Pause();
        else if(Keyboard.current.pKey.wasPressedThisFrame && _isPaused)
            Unpause();
    }
}

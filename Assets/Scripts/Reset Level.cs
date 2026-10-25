using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using Unity.VisualScripting;

public class ResetLevel : MonoBehaviour
{
    void Update()
    {
        if(Keyboard.current.rKey.wasPressedThisFrame)
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    void OnTriggerEnter()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}

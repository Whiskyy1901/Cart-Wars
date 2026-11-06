using TMPro;
using UnityEngine;

public class FinishLine : MonoBehaviour
{
    private CheckpointManager _manager;
    private Timer _timer;
    private OrderHandler _handler;

    private bool c1, c2;

    [SerializeField] private GameObject _winScreen;
    [SerializeField] private TextMeshProUGUI _timerText;
    
    
    private void Awake()
    {
        _manager = FindAnyObjectByType<CheckpointManager>();
        _timer = FindAnyObjectByType<Timer>();
        _handler = FindAnyObjectByType<OrderHandler>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (_manager.IsCompleted() && _handler.IsOrderComplete())
            WinSequence();
        else
            Debug.LogError("Incomplete");
    }

    private void WinSequence()
    {
        _timer._isActive = false; 
        int minutes = Mathf.FloorToInt(_timer.PassedTime / 60);
        int seconds = Mathf.FloorToInt(_timer.PassedTime % 60);
        int milliseconds = Mathf.FloorToInt((_timer.PassedTime * 1000) % 1000);
        _winScreen.SetActive(true);
        _timerText.text = string.Format("{0:00}:{1:00}:{2:000}", minutes, seconds, milliseconds);
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
        Time.timeScale = 0;
    }
}
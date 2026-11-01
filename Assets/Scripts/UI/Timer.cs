using UnityEngine;
using TMPro;

public class Timer : MonoBehaviour
{
    private TextMeshProUGUI _timerText;
    private float _passedTime;
    private int _minutes;
    private int _seconds;
    private int _milliseconds;

    public bool _isActive = true;
    
    // For future time based events, like level fail con etc
    public float PassedTime => _passedTime;

    private void Awake()
    {
        _timerText = GetComponent<TextMeshProUGUI>();
    }

    private void Update()
    {
        if (_isActive)
        {
            _passedTime += Time.deltaTime;
            _minutes = Mathf.FloorToInt(_passedTime / 60);
            _seconds = Mathf.FloorToInt(_passedTime % 60);
            _milliseconds = Mathf.FloorToInt((_passedTime * 1000) % 1000);

            _timerText.text = string.Format("{0:00}:{1:00}:{2:000}", _minutes, _seconds, _milliseconds);
        }
    }
}

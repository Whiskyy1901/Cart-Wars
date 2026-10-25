using UnityEngine;
using UnityEngine.InputSystem;

public class DialogueInteraction : InteractableBase
{
    [SerializeField] private string[] _messageStrings;
    private bool _isActive;
    [SerializeField] private DialogueUIPanel _dialoguePanel;
    [SerializeField] private bool _autoNext;
    [SerializeField] private float _autoNextDelay = 2f;
    private int _currentLine = 0;

    protected override void OnInteractLogic()
    {
        base.OnInteractLogic();

        if (_isActive)
            return;

        _dialoguePanel.gameObject.SetActive(true);
        _isActive = true;
        _currentLine = 0;
        _dialoguePanel.Enable();
        _dialoguePanel.SetDialogue(_messageStrings[_currentLine]);
        _currentLine++;

        if (_autoNext)
            AutoPlay();
    }

    private void Update()
    {
        if (!_isActive || _autoNext)
            return;

        if (Keyboard.current.enterKey.wasPressedThisFrame)
            NextLine();
    }

    private async void AutoPlay()
    {
        while (_isActive)
        {
            await Awaitable.WaitForSecondsAsync(_autoNextDelay);

            if (!_isActive)
                return;

            NextLine();
        }
    }

    private void NextLine()
    {
        if (_currentLine < _messageStrings.Length)
        {
            _dialoguePanel.SetDialogue(_messageStrings[_currentLine]);
            _currentLine++;
        }
        else
        {
            _dialoguePanel.gameObject.SetActive(false);
            _isActive = false;
            _currentLine = 0;
        }
    }
}
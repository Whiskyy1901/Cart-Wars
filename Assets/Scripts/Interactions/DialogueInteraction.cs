using UnityEngine;
using UnityEngine.InputSystem; 

public class DialogueInteraction : InteractableBase
{
    [SerializeField] private string[] _messageStrings;
    private bool _isActive;
    [SerializeField] private DialogueUIPanel _dialoguePanel;
    private int _currentLine = 0;


    protected override void OnInteractLogic()
    {
        base.OnInteractLogic();
        _dialoguePanel.gameObject.SetActive(true);
        _isActive = true;
        _currentLine = 0;
        _dialoguePanel.Enable();
        _dialoguePanel.SetDialogue(_messageStrings[_currentLine]);
        _currentLine++;
    }

    private void Update()
    {
        if(!_isActive)
            return;
        
        if (_isActive && Keyboard.current.enterKey.wasPressedThisFrame)
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
                _currentLine = 0; //reset
            }
        }
    }
}

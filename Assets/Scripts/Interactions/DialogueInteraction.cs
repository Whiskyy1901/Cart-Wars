using Mirror;
using UnityEngine;
using UnityEngine.InputSystem; 

public class DialogueInteraction : NetworkedInteractableBase
{
    [SerializeField] private string[] _messageStrings;
    private bool _isActive;
    private DialogueUIPanel _dialoguePanel;
    private int _currentLine = 0; //To keep tract of current position in _messageStrings;


    protected override void OnInteractLocal()
    {
        base.OnInteractLocal();
        _dialoguePanel = NetworkClient.localPlayer.GetComponentInChildren<DialogueUIPanel>(true);
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
                _currentLine = 0; // reset for next time dialogue opens
            }
        }
    }
}

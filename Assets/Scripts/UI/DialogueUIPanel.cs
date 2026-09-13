using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class DialogueUIPanel : MonoBehaviour
{
    [SerializeField] private Image _dialogueBG;
    [SerializeField] private TextMeshProUGUI _dialogue;

    public void SetDialogue(string dialogue)
    {
        _dialogue.text = dialogue;
    }

    public void Enable()
    {
        _dialogue.gameObject.SetActive(true);
        _dialogueBG.gameObject.SetActive(true);
    }

    public void Disable()
    {
        _dialogue.gameObject.SetActive(false);
        _dialogueBG.gameObject.SetActive(false);
    }

}



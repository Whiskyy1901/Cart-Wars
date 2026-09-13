using UnityEngine;
using UnityEngine.InputSystem;

public class BuyUIPanel : MonoBehaviour
{
    private void Update()
    {
        if (Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            this.gameObject.SetActive(false);
        }
    }
}

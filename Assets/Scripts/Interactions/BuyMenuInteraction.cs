using Mirror;
using UnityEngine;

public class BuyMenuInteraction : NetworkedInteractableBase
{
    protected override void OnInteractLocal()
    {
        base.OnInteractLocal();
        BuyUIPanel _panel = NetworkClient.localPlayer.GetComponentInChildren<BuyUIPanel>(true);//Include inactive children
        _panel.gameObject.SetActive(true);
    }
}

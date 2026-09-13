using Mirror;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class InteractionUIPanel : NetworkBehaviour
{
    [SerializeField] private Image _progressBar;
    [SerializeField] private TextMeshProUGUI _tooltip;
    [SerializeField] private TextMeshProUGUI _secondaryTooltip;

    //Disable on clientstart and enabled by local player so that each player has their own menu and doesnt interfere with each other.
    public override void OnStartClient()
    {
        base.OnStartClient();
        this.gameObject.SetActive(false);
    }

    public override void OnStartLocalPlayer()
    {
        base.OnStartLocalPlayer();
        this.gameObject.SetActive(true);
    }

    public void SetTooltip(string tooltip, string secondarytooltip)
    {
        if (!isLocalPlayer)
            return;
        _tooltip.SetText(tooltip);
        _secondaryTooltip.SetText(secondarytooltip);
    }

    public void SetProgressBar(float progress)
    {
        if (!isLocalPlayer)
            return;
        _progressBar.fillAmount = progress;
    }

    public void ResetUI()
    {
        if (!isLocalPlayer)
            return;
        _tooltip.SetText("");
        _progressBar.fillAmount = 0f;
    }
}

using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
public class InteractionUIPanel : MonoBehaviour
{
    [SerializeField] private Image _progressBar;
    [SerializeField] private TextMeshProUGUI _tooltip;
    [SerializeField] private TextMeshProUGUI _secondaryTooltip;
    
    public void SetTooltip(string tooltip, string secondarytooltip)
    {
        _tooltip.SetText(tooltip);
        _secondaryTooltip.SetText(secondarytooltip);
    }

    public void SetProgressBar(float progress)
    {
        _progressBar.fillAmount = progress;
    }

    public void ResetUI()
    {
        _tooltip.SetText("");
        _progressBar.fillAmount = 0f;
    }
}

using System;
using UnityEngine;

public class MenuInteraction : InteractableBase
{
    [SerializeField] private GameObject _panel;

    private void Start()
    {
        _panel.SetActive(false);
    }

    protected override void OnInteractLogic()
    {
        base.OnInteractLogic();
        _panel.SetActive(true);
    }
}

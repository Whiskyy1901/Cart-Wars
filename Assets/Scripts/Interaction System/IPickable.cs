using UnityEngine;

public interface IPickable
{
    Rigidbody Rigidbody { get;}
    void CmdOnPickUp();
    void OnHold();
    void CmdOnRelease();
}
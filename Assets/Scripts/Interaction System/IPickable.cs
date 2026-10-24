using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public interface IPickable
{
    Rigidbody Rigidbody {get;set;}
    void OnPickUp();
    void OnHold();
    void OnRelease();
}


using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class Pickable : MonoBehaviour, IPickable
{
    private void Awake()
    {
        rigid = GetComponent<Rigidbody>();
    }

    private Rigidbody rigid;

    public Rigidbody Rigidbody
    {
        get => rigid; 
        set => rigid = value;
    }

    private bool Picked { get; set; }
    public void OnHold() {}

    public void OnPickUp()
    {
        Picked = true;
        rigid.isKinematic = true;

        Physics.IgnoreLayerCollision(gameObject.layer, LayerMask.NameToLayer("Player"), true);
    }

    public void OnRelease()
    {
        Picked = false;
        rigid.isKinematic = false;
        Physics.IgnoreLayerCollision(gameObject.layer, LayerMask.NameToLayer("Player"), false);
    }
}
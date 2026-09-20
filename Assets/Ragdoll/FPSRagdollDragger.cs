using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;

public class FPSRagdollDragger : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera playerCamera;
    [SerializeField] private InputActionReference grabAction;

    [Header("Interaction Settings")]
    [SerializeField] private float interactionRadius = 3.5f;
    [SerializeField] private float minHoldDistance = 1.5f;
    [SerializeField] private LayerMask grabableLayer;

    [Header("References")]
    [SerializeField] private float SpringForce = 700f;
    [SerializeField] private float damperForce = 70f;

    private SpringJoint currentJoint; 
    private GameObject anchorObject;
    private float currentHoldDistance; 
    private ActiveRagdoll currentActiveRagdoll;

    private void OnEnable()
    {
        grabAction?.action.Enable();
    }

    private void OnDisable()
    {
        grabAction?.action.Disable();

    }


    void Start()
    {
        if(playerCamera == null ) playerCamera = Camera.main;    
    }


    
    void Update()
    {
        if (grabAction == null || playerCamera == null) return;

        bool isHolding = grabAction.action.IsPressed();

        // 1. Start Grab
        if (grabAction.action.WasPressedThisFrame() && anchorObject == null)
        {
            TryGrab();
        }
        // 2. Update Position while holding
        else if (isHolding && anchorObject != null)
        {
            UpdateAnchor();
        }
        // 3. Release Grab whenever button is released
        else if (!isHolding && anchorObject != null)
        {
            ReleaseGrab();
        }

    }

    private void TryGrab()
    {
        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);

        if(Physics.Raycast(ray, out RaycastHit hit, interactionRadius, grabableLayer))
        {

            //find activeRagdoll controller on target
            currentActiveRagdoll = hit.collider.GetComponentInParent<ActiveRagdoll>();
            /*if(currentActiveRagdoll != null)
               currentActiveRagdoll.EnableRagdoll();*/
            Rigidbody hitbody = hit.rigidbody;

            if(hitbody != null && !hitbody.isKinematic)
            {
                currentHoldDistance = Mathf.Max(hit.distance, minHoldDistance);

                //create anchor point
                anchorObject = new GameObject("FPSRagdollAnchor");
                anchorObject.transform.position = hit.point;
                Rigidbody anchorRb = anchorObject.AddComponent<Rigidbody>();
                anchorRb.isKinematic = true;

                //connect grabbed bone to anchor

                currentJoint = hitbody.gameObject.AddComponent<SpringJoint>();
                currentJoint.connectedBody = anchorRb;
                currentJoint.autoConfigureConnectedAnchor = false;
                currentJoint.connectedAnchor = Vector3.zero;
                currentJoint.anchor = hitbody.transform.InverseTransformPoint(hit.point);

                currentJoint.spring = SpringForce;
                currentJoint.damper = damperForce;

            }
        }
    }
    private void UpdateAnchor()
    {
        Vector3 targetPos = playerCamera.transform.position + (playerCamera.transform.forward * currentHoldDistance);
        anchorObject.transform.position = targetPos;
    }
    private void ReleaseGrab()
    {
        if(anchorObject != null ) Destroy(anchorObject);
        if(currentJoint != null ) Destroy(currentJoint);

        if (currentActiveRagdoll != null)
        {
            //currentActiveRagdoll.DisableRagdollWithDelay();
            currentActiveRagdoll = null;
        }
        currentJoint = null;
        anchorObject = null;
    }
}

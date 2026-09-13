using Mirror;
using UnityEngine;

public class InteractionController : NetworkBehaviour
{
   [Header("Data")]
   public InteractionInputData interactionInputData;
   public InteractionData interactionData;
   
   [Header("Ray")]
   [SerializeField] private float _rayLength;
   [SerializeField] private float _rayRadius;
   [SerializeField] private LayerMask _interactionLayer;

   [Header("UI")] 
   [SerializeField] private InteractionUIPanel _uiPanel;

   private Camera _camera;
   private bool _isInteracting;
   private float _holdTime = 0f;

   public override void OnStartLocalPlayer()
   {
      _camera = GetComponentInChildren<Camera>();
      _uiPanel.ResetUI();
      var listener = _camera.GetComponent<AudioListener>();
      if (listener != null)
         listener.enabled = true;
   }

   private void Update()
   {
      if (!isLocalPlayer)
         return;
      CheckForInteractable();
      CheckForInteractableInput();
   }
   
   private void CheckForInteractable()
   {
      Ray _ray = new Ray(_camera.transform.position, _camera.transform.forward);
      RaycastHit _hit;

      bool _hitObject = Physics.SphereCast(_ray, _rayRadius, out _hit, _rayLength, _interactionLayer);

      if (_hitObject)
      {
         IInteractable[] _interactables = _hit.transform.gameObject.GetComponents<IInteractable>();

         if (_interactables.Length > 0)
         {
            if (_interactables.Length>0)
            {
               interactionData.InteractableBases = _interactables;
               _uiPanel.SetTooltip(_interactables[0].Tooltip, _interactables[0].SecondaryTooltip);
            }
         }
         else
         {
            _uiPanel.ResetUI();
            interactionData.ResetData();
         }
      }
      else
      {
         _uiPanel.ResetUI();
         interactionData.ResetData();
      }
   }
   
   private void CheckForInteractableInput()
   {
      if (interactionData.IsEmpty())
         return;
      if (interactionInputData.InteractionPressed)
      {
         _isInteracting = true;
         _holdTime = 0f;
      }

      if (interactionInputData.InteractionReleased)
      {
         _isInteracting = false;
         _holdTime = 0f;
         _uiPanel.SetProgressBar(0f);
      }

      if (_isInteracting)
      {
         if (!interactionData.AllInteractable())
            return;

         if (interactionData.AnyHoldInteract())
         {
            _holdTime += Time.deltaTime;
            float _duration = interactionData.MaxHoldDuration();
            _uiPanel.SetProgressBar(_holdTime / _duration);

            if (_holdTime >= _duration)
            {
               interactionData.Interact();
               _isInteracting = false;
            }
         }
         else
         {
            interactionData.Interact();
            _isInteracting = false;
         }
      }
   }
}
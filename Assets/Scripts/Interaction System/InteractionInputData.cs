using UnityEngine;

[CreateAssetMenu(fileName = "InteractionInputData", menuName = "InteractionSystem/InputData")]
public class InteractionInputData : ScriptableObject
{
   private bool _interactionPressed;
   private bool _interactionReleased;

   public bool InteractionPressed
   {
      get => _interactionPressed;
      set => _interactionPressed = value;
   }

   public bool InteractionReleased
   {
      get => _interactionReleased;
      set => _interactionReleased = value;
   }

   public void ResetData()
   {
      _interactionPressed = false;
      _interactionReleased = false;
   }
}

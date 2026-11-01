using UnityEngine;

public class CheckpointManager : MonoBehaviour
{
    [SerializeField] private Checkpoint[] _checkpoints;

    public bool IsCompleted()
    {
        foreach (Checkpoint checkpoint in _checkpoints)
        {
            if (!checkpoint.HasPassed)
            {
                return false;
            }
        }

        return true;
    }
}

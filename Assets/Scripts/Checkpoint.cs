using UnityEngine;

public class Checkpoint : MonoBehaviour
{
    private bool _hasPassed;

    public bool HasPassed => _hasPassed;

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("Player"))
        {
            _hasPassed = true;
        }
    }
}

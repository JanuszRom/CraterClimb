using UnityEngine;

public class PlatformTrigger : MonoBehaviour
{
    [SerializeField] private WallMove wallParent;
    private CharacterController playerOnPlatform;
    private void OnTriggerEnter(Collider other)
    {
   
        CharacterController controller = other.GetComponentInParent<CharacterController>();
        if (controller != null)
        {
            wallParent.SetPlayerOnPlatform(controller);
            Debug.Log("Player on platform");
        }
    }
    private void OnTriggerExit(Collider other)
    {
        CharacterController controller = other.GetComponentInParent<CharacterController>();
        if (controller != null)
        {
            wallParent.ClearPlayerOnPlatform(controller);
            Debug.Log("Player left platform");
        }
    }
}

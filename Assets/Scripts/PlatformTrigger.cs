using UnityEngine;

public class PlatformTrigger : MonoBehaviour
{
    private CharacterController playerOnPlatform;
    private void OnTriggerEnter(Collider other)
    {

        Debug.Log("Trigger Entered by:" + other.name);
        CharacterController controller = other.GetComponentInParent<CharacterController>();
        if (controller != null)
        {
            playerOnPlatform = controller;
            Debug.Log("Player on platform");
        }

        //if (other.TryGetComponent(out CharacterController controller))
        //{
        //    playerOnPlatform = controller;
        //    Debug.Log("Player on platform");
        //}
        //else
        //{
        //    Debug.Log("Nothing on platform");
        //}
    }
    private void OnTriggerExit(Collider other)
    {
        if (other.TryGetComponent(out CharacterController controller) && controller == playerOnPlatform)
        {
            playerOnPlatform = null;
        }
    }
}

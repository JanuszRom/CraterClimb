using UnityEngine;

public class Wand : MonoBehaviour
{
    [SerializeField] private GameObject wandVisual;
    [SerializeField] private GameObject heldWand;
    private Vector3 rotationSpeed = new Vector3(0, 120f, 0);
    private void Update()
    {
        wandVisual.transform.Rotate(0f, rotationSpeed.y * Time.deltaTime, 0f, Space.World);
    }
    private void OnTriggerEnter(Collider other)
    {
       var holder = other.GetComponentInParent<PlayerWandHolder>();
        if (holder != null)
        {
            if (holder.Equip(heldWand))
            {
                Destroy(gameObject);
            }
        }
    }
}

using UnityEngine;

public class PlayerWandHolder : MonoBehaviour
{
    [SerializeField] private Transform wandHoldPoint;
    public bool HasWand => currentWand;
    private GameObject currentWand;

    public bool Equip(GameObject heldWand)
    {
        if (HasWand) return false;
        currentWand = Instantiate(heldWand, wandHoldPoint);
        currentWand.transform.localPosition = Vector3.zero;
        currentWand.transform.localRotation = Quaternion.identity;
        return true;
    }
}

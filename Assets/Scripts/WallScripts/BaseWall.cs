using UnityEngine;

public class BaseWall : MonoBehaviour
{
    [SerializeField] protected float moveSpeed = 2f;
    [SerializeField] protected Vector3 moveDistance = new Vector3(10f, 0f, 0f);

    public virtual void Interact(Player player)
    {

    }

}

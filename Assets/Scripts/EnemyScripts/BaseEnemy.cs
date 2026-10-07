using UnityEngine;

public class BaseEnemy : MonoBehaviour
{
    [SerializeField] protected float maxHealth = 100f;

    public virtual void Movement()
    {
        // Base movement logic for enemies
    }

    public virtual void Attack()
    {
        // Base attack logic for enemies
    }
}

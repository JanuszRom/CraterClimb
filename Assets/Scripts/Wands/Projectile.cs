using UnityEditor.Rendering.Analytics;
using UnityEngine;

public class Projectile : MonoBehaviour
{
    [SerializeField] private float damage = 10f;
    [SerializeField] private float lifetime = 5f;
    [SerializeField] private bool faceVelocity = true;
    [SerializeField] private GameObject impactVfx;

    private Rigidbody rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.useGravity = true;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
    }
    
    private void Start()
    {
        Destroy(gameObject, lifetime);
    }

    public void Launch(Vector3 velocity)
    {
        if (rb == null) rb = GetComponent<Rigidbody>();
        rb.linearVelocity = velocity;
    }

    private void FixedUpdate()
    {
        if (faceVelocity && rb.linearVelocity.sqrMagnitude > 0.01f)
            rb.MoveRotation(Quaternion.LookRotation(rb.linearVelocity));
    }

    private void OnCollisionEnter(Collision collision)
    {
        var target = collision.collider.GetComponentInParent<IDamageable>();
        if (target != null) target.TakeDamage(damage);

        if (impactVfx != null)
        {
            ContactPoint contact = collision.GetContact(0);
            Instantiate(impactVfx, contact.point, Quaternion.LookRotation(contact.normal));
        }
        Destroy(gameObject);
    }
}

using UnityEditor.Search;
using UnityEngine;

public class Jellyfish : BaseEnemy
{
    [SerializeField] private float minUpwardForce = 2f;
    [SerializeField] private float maxUpwardForce = 5f;
    [SerializeField] private float minForwardForce = -3f;
    [SerializeField] private float maxForwardForce = 3f;    
    [SerializeField] private float flapInterval = 2f;
    [SerializeField] private Transform body;
    [SerializeField] private float gravity = 5f;
    private Rigidbody rb;
    private float flapTimer;
    private float upwardForce;

    private void Awake()
    {
        rb = body.GetComponent<Rigidbody>();
    }

    private void FixedUpdate()
    {
        Movement();
        rb.AddForce(Vector3.down * gravity, ForceMode.Acceleration);
    }
    public override void Movement()
    {
        Vector3 velocity = rb.linearVelocity;
        
        upwardForce = Random.Range(minUpwardForce, maxUpwardForce);
        flapTimer -= Time.fixedDeltaTime;
        if (flapTimer <= 0f)
        {
            velocity.y = 0f;
            rb.linearVelocity = velocity;
            rb.AddForce(Vector3.up * upwardForce, ForceMode.Impulse);
            rb.AddForce(Vector3.forward * Random.Range(minForwardForce, maxForwardForce), ForceMode.Impulse);
            flapTimer = flapInterval;
        }
    }
}

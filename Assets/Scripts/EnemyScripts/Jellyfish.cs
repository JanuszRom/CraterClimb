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
    [SerializeField] private float detectionRayTop = 1f;
    [SerializeField] private float detectionRayMiddle = 0f;
    [SerializeField] private float detectionRayBottom = -1f;
    [SerializeField] private float detectionDistance = 3f;
    [SerializeField] private LayerMask wallLayer;
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
        DetectObstacles();
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
    private void DetectObstacles()
    {
        Debug.Log(transform.position);
        Vector3[] origins =
            { transform.position + Vector3.up * detectionRayTop,
                transform.position + Vector3.up * detectionRayBottom,
                transform.position + Vector3.up * detectionRayMiddle
        };
        Vector3[] directions =
        {
                        transform.forward,
            -transform.forward,
            transform.right,
            -transform.right,
            (transform.forward + transform.right).normalized,
            (transform.forward - transform.right).normalized,
            (-transform.forward + transform.right).normalized,
            (-transform.forward - transform.right).normalized

        };

        foreach (Vector3 origin in origins)
        {
            foreach (Vector3 direction in directions)
            {
                if (Physics.Raycast(origin, direction, out RaycastHit hit, detectionDistance, wallLayer, QueryTriggerInteraction.Ignore))
                
                    {
                        rb.AddForce(direction * Random.Range(minForwardForce, maxForwardForce), ForceMode.Impulse);
                        flapInterval = 0;
                    }
                
                Debug.DrawRay(origin, direction * detectionDistance, Color.red);
            }
        }
    }
}

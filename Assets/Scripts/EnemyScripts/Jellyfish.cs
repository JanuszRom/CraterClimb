using UnityEditor.Search;
using UnityEngine;

public class Jellyfish : BaseEnemy
{
    [SerializeField] private float minUpwardForce = 3f;
    [SerializeField] private float maxUpwardForce = 5f;
    [SerializeField] private float minForwardForce = -3f;
    [SerializeField] private float maxForwardForce = 3f;
    [SerializeField] private float flapInterval = 2.5f;
    [SerializeField] private float detectionInterval = 2f;
    [SerializeField] private Transform body;
    [SerializeField] private float gravity = 3f;
    [SerializeField] private float detectionRayTop = 2f;
    [SerializeField] private float detectionRayMiddle = 0f;
    [SerializeField] private float detectionRayBottom = -2f;
    [SerializeField] private float detectionDistance = 2f;
    [SerializeField] private LayerMask wallLayer;
    private Rigidbody rb;
    private float flapTimer;
    private float detectionTimer;
    private float upwardForce;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
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
            velocity = Vector3.zero;
              
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
        detectionTimer -= Time.fixedDeltaTime;
        if (detectionTimer <= 0f)
        {

            foreach (Vector3 origin in origins)
            {
                foreach (Vector3 direction in directions)
                {
                    if (Physics.Raycast(origin, direction, out RaycastHit hit, detectionDistance, wallLayer, QueryTriggerInteraction.Ignore))

                    {
                        Debug.Log("Obstacle detected");
                        rb.AddForce(direction * Random.Range(minForwardForce, maxForwardForce), ForceMode.Impulse);
                        //flapInterval = 0;
                        detectionTimer = detectionInterval;
                    }

                    Debug.DrawRay(origin, direction * detectionDistance, Color.red);
                }
            }
        }
    }
}
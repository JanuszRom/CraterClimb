using UnityEngine;

public class WallMove : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 2f;
    [SerializeField] private Vector3 moveDistance = new Vector3(10f, 0f, 0f);
    [SerializeField] private float moveTime = 50f;

    private Vector3 startPosition;
    private Vector3 targetPosition;

    private void Start()
    {
        startPosition = transform.position;
        targetPosition = startPosition + moveDistance;
    }

    private void Update()
    {
        Move();
    }
    
    private void Move()
    {
        transform.position = Vector3.Lerp(startPosition, targetPosition, Mathf.PingPong(Time.time / moveTime, 1f));
    }
}

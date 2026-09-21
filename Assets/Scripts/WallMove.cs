using UnityEngine;

public class WallMove : BaseWall
{
    
   

    private Vector3 startPosition;
    private Vector3 targetPosition;

    private void Start()
    {
        startPosition = transform.position;
        targetPosition = startPosition + moveDistance;
    }

    private void Update()
    {
        //Move();
    }
    
    private void Move()
    {
        transform.position = Vector3.MoveTowards(transform.position, targetPosition, moveSpeed * Time.deltaTime);
        //transform.position = Vector3.Lerp(startPosition, targetPosition, Mathf.PingPong(Time.time / moveTime, 1f));
    }
}

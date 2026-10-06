using UnityEngine;

public class WallMove : BaseWall
{



    private Vector3 startPosition;
    private Vector3 targetPosition;
    private Vector3 lastPosition;
    public Vector3 FrameDelta { get; private set; }
    private bool isMoving = false;
    private bool isMovingBack = false;
    private CharacterController playerOnPlatform;

    private void Awake()
    {
        lastPosition = transform.position;
    }

    private void Start()
    {
        startPosition = transform.position;
        targetPosition = startPosition + moveDistance;
    }

    private void Update()
    {
        if (!isMoving && !isMovingBack)
        {
            return;
        }
        if (isMoving)
        {
            Move();
        }
        else if (isMovingBack)
        {
            MoveBack();
        }
        if (transform.position == targetPosition)
        {
            isMoving = false;
        }
        
    }
    private void LateUpdate()
    {
        FrameDelta = transform.position - lastPosition;
        lastPosition = transform.position;

        if (playerOnPlatform != null && FrameDelta != Vector3.zero)
        {
            playerOnPlatform.Move(FrameDelta);
        }
    }

    
    public override void Interact(Player player)
    {
     if (!isMoving && !isMovingBack)
        if (transform.position == startPosition)
            {
                isMoving = true;
            }
            else
            {
                isMovingBack = true;
            }
     else if (isMovingBack)
        {
            isMovingBack = false;
            isMoving = true;
        }
     else
        {
            isMoving = false;
            isMovingBack = true;
        }
    }

   private void Move()
    {
        transform.position = Vector3.MoveTowards(transform.position, targetPosition, moveSpeed * Time.deltaTime);
        
    }
    private void MoveBack()
    {
        transform.position = Vector3.MoveTowards(transform.position, startPosition, moveSpeed * Time.deltaTime);
    }

    public void SetPlayerOnPlatform(CharacterController player)
    {
        playerOnPlatform = player;
    }

    public void ClearPlayerOnPlatform(CharacterController player)
    {
        if (playerOnPlatform == player)
        {
            playerOnPlatform = null;
        }
    }
}

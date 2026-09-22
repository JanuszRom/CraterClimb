using UnityEngine;

public class Player : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 7f;
    [SerializeField] private float rotateSpeed = 7f;
    [SerializeField] private GameInput gameInput;
<<<<<<< Updated upstream
=======
    [SerializeField] private LayerMask wallLayer;
    [SerializeField] private Transform cameraTransform;
    [SerializeField] private Camera playerCamera;
    [SerializeField] private float selectRange = 10f;

    public event EventHandler<OnSelectedWallChangedEventArgs> OnSelectedWallChanged;
    public class OnSelectedWallChangedEventArgs : EventArgs
    {
        public BaseWall selectedWall;
    }
    private const float GROUND_CHECK_RADIUS = .15f;
    private const float GROUND_CHECK_DISTANCE = .2f;
    private const float GRAVITY = -25f;
    private CharacterController controller;
    private float verticalVelocity;
    private bool isWalking;
    private bool wasGrounded;
    private Vector3 lastMovementDir;
    private BaseWall selectedWall;

    private void Awake()
    {
        Instance = this;
        controller = GetComponent<CharacterController>();
    }

    private void Start()
    {
        gameInput.OnJumpAction += GameInput_OnJumpAction;
        gameInput.OnInteractAction += GameInput_OnInteractAction;
    }

    private void GameInput_OnInteractAction(object sender, System.EventArgs e)
    {
        if (selectedWall != null)
        {
            selectedWall.Interact(this);
            Debug.Log($"Interacted with {selectedWall.name}");
        }
    }
    private void GameInput_OnJumpAction(object sender, System.EventArgs e)
    {
        if (controller.isGrounded)
        {
            verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * GRAVITY);
        }
    }
>>>>>>> Stashed changes

    private void Update()
    {
        HandleMovement();
    }
    public void HandleMovement()
    {
        Vector2 inputVector = gameInput.GetMovementVectorNormalized();
        Vector3 moveDir = new Vector3(inputVector.x, 0f, inputVector.y);

        float playerSize = .7f;
        bool canMove = !Physics.Raycast(transform.position, moveDir, playerSize);
        if (canMove)
        {
            transform.position += moveDir * moveSpeed * Time.deltaTime;
        }
        transform.forward = Vector3.Slerp(transform.forward, moveDir, Time.deltaTime * rotateSpeed);

       
    }
    public void Jump()
    {
<<<<<<< Updated upstream
=======
        this.selectedWall = selectedWall;
        OnSelectedWallChanged?.Invoke(this, new OnSelectedWallChangedEventArgs { selectedWall = selectedWall });
       //Debug.Log($"Selected Wall: {selectedWall?.name ?? "None"}");
>>>>>>> Stashed changes

    }
}

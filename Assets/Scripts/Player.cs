using UnityEngine;
using System;

public class Player : MonoBehaviour
{
    public static Player Instance { get; private set; }
    [SerializeField] private float moveSpeed = 7f;
    [SerializeField] private float rotateSpeed = 10f;
    [SerializeField] private float jumpHeight = 1.5f;
    [SerializeField] private GameInput gameInput;
    [SerializeField] private LayerMask groundLayer;
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
        // Handle interaction logic here
    }
    private void GameInput_OnJumpAction(object sender, System.EventArgs e)
    {
        if (controller.isGrounded)
        {
            verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * GRAVITY);
        }
    }

    private void Update()
    {
        HandleMovement();
        HandleInteractions();
    }

    private void HandleInteractions()
    {
        
        Ray cameraRay = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
        
        if (Physics.Raycast(cameraRay, out RaycastHit hit, selectRange, groundLayer /*, QueryTriggerInteraction.Ignore*/))
        {
           
            if (hit.transform.TryGetComponent(out BaseWall baseWall))
            {
                if (baseWall != selectedWall) 
                {
                    SetSelectedWall(baseWall);
                }
            }
            else
            {
                SetSelectedWall(null);
            }
        }
        else
        {
            SetSelectedWall(null);
          
        }
    }
    public void HandleMovement()
    {
        Vector2 inputVector = gameInput.GetMovementVectorNormalized();
        Vector3 moveDir = GetCameraRelativeMoveDir(inputVector);
        Vector3 moveDirReal = new Vector3(inputVector.x, 0f, inputVector.y);

        isWalking = moveDir != Vector3.zero;
        bool grounded = CheckGrounded();
        if (moveDirReal != Vector3.zero)
        {
            lastMovementDir = moveDirReal;
        }
        

       
        if (grounded && verticalVelocity < 0f)
        {
        verticalVelocity = -2f;
        } else if (wasGrounded && verticalVelocity <= 0f)
        {
            
            controller.Move(lastMovementDir * 0.5f + Vector3.down * 0.5f);
        

        }
        
        
        verticalVelocity += GRAVITY * Time.deltaTime;
        Vector3 velocity = moveDir * moveSpeed + Vector3.up * verticalVelocity;
        controller.Move(velocity * Time.deltaTime);
        wasGrounded = grounded;

        if (isWalking)
        {
            transform.forward = Vector3.Slerp(transform.forward, moveDir, Time.deltaTime * rotateSpeed);
        }
       
    }

    private void SetSelectedWall(BaseWall selectedWall)
    {
        this.selectedWall = selectedWall;
        OnSelectedWallChanged?.Invoke(this, new OnSelectedWallChangedEventArgs { selectedWall = selectedWall });
        Debug.Log($"Selected Wall: {selectedWall?.name ?? "None"}");

    }

    private bool CheckGrounded()
    {
        Vector3 origin = transform.position + Vector3.up * GROUND_CHECK_RADIUS;
        return Physics.SphereCast(origin, GROUND_CHECK_RADIUS, Vector3.down, out _, GROUND_CHECK_DISTANCE, groundLayer, QueryTriggerInteraction.Ignore);
    }
    private Vector3 GetCameraRelativeMoveDir(Vector2 inputVector)
    {
        Vector3 camForward = cameraTransform.forward;
        Vector3 camRight = cameraTransform.right;
        camForward.y = 0f;
        camRight.y = 0f;
        camForward.Normalize();
        camRight.Normalize();
        return camForward * inputVector.y + camRight * inputVector.x;
    }
}

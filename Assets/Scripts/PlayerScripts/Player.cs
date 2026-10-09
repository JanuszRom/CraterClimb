

using UnityEngine;
using System;
using Unity.Cinemachine;
using Unity.VisualScripting;

public class Player : MonoBehaviour
{
    public static Player Instance { get; private set; }
    [SerializeField] private float moveSpeed = 7f;
    [SerializeField] private float rotateSpeed = 10f;
    [SerializeField] private float jumpHeight = 1.5f;
    [SerializeField] private GameInput gameInput;
    [SerializeField] private LayerMask wallLayer;
    [SerializeField] private Transform cameraTransform;
    [SerializeField] private Camera playerCamera;
    [SerializeField] private CameraTarget cameraTarget;
    [SerializeField] private float aimTurnSpeed = 1080f;
    [SerializeField] private float selectRange = 10f;

    [SerializeField] private float wallRayDistance = 0.53f;
    [SerializeField] private float wallRayHeight = -1f;
    [SerializeField] private float wallPushStrength = 2.5f;
    [SerializeField] private float wallFallSpeed = 1.5f;
    [SerializeField] private float gravityMultiplier = 2f;
    [SerializeField] private float hardLandSpeed = 5f;
    [SerializeField] private float landRecoveryTime = .15f;

    public event EventHandler<OnSelectedWallChangedEventArgs> OnSelectedWallChanged;
    public class OnSelectedWallChangedEventArgs : EventArgs
    {
        public BaseWall selectedWall;
    }
    [SerializeField] private float GROUND_CHECK_RADIUS = .25f;
    [SerializeField] private float GROUND_CHECK_DISTANCE = 1.1f;
    private const float GRAVITY = -25f;
    private CharacterController controller;
    private float verticalVelocity;
    private bool isWalking;
    private bool grounded;
    private bool wasGrounded;
    private Vector3 lastMovementDir;
    private Vector3 moveDir;
    private BaseWall selectedWall;
    private bool jumpRequested = false;
    public float targetSpeed;
    public bool isAiming = false;
    public float aimX;
    public float aimY;
    private float landRecoveryTimer;


    private void Awake()
    {
        Instance = this;
        controller = GetComponent<CharacterController>();
    }

    private void Start()
    {
        gameInput.OnJumpAction += GameInput_OnJumpAction;
        gameInput.OnInteractAction += GameInput_OnInteractAction;
        gameInput.OnAim += GameInput_OnAim;

    }

    private void GameInput_OnInteractAction(object sender, System.EventArgs e)
    {
        if (selectedWall != null)
        {
            selectedWall.Interact(this);
        }
    }
    private void GameInput_OnAim(bool isAiming)
    {
        this.isAiming = isAiming;
        cameraTarget.SetAiming(isAiming);
    }
   
    private void GameInput_OnJumpAction(object sender, System.EventArgs e)
    {
        if (CheckGrounded())
        {
            jumpRequested = true;
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

        

        if (Physics.Raycast(cameraRay, out RaycastHit hit, selectRange, wallLayer, QueryTriggerInteraction.Ignore))
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
        moveDir = GetCameraRelativeMoveDir(inputVector);
        Vector3 moveDirReal = new Vector3(inputVector.x, 0f, inputVector.y);

        aimX = inputVector.x;
        aimY = inputVector.y;

        if (inputVector != Vector2.zero)
        {
            targetSpeed = 1f;
        }
        else
        {
            targetSpeed = 0f;
        }

        isWalking = moveDir != Vector3.zero;
       
        if (moveDirReal != Vector3.zero)
        {
            lastMovementDir = moveDirReal;
        }
        bool grounded = CheckGrounded();

        if (grounded && !wasGrounded && verticalVelocity < -hardLandSpeed)
        {
            landRecoveryTimer = landRecoveryTime;
        }
        if (grounded)
        {
            if (verticalVelocity < 0f)
            {
                verticalVelocity = -2f;
            }
            jumpRequested = false;
        }
        else
        {
            if (verticalVelocity > 0f)
            {
                verticalVelocity += GRAVITY * Time.deltaTime;
            }
            else
            {

                Vector3 wallPushDirection = GetWallPushDirection();
                if (wallPushDirection != Vector3.zero)
                {
                    controller.Move(wallPushDirection * wallPushStrength * Time.deltaTime);
                    verticalVelocity -= wallFallSpeed;
                }
                else
                {
                    verticalVelocity += GRAVITY * Time.deltaTime * gravityMultiplier;
                }
            }
        }
        float moveFactor = 1f;
        if (landRecoveryTimer > 0f)
        {
            landRecoveryTimer -= Time.deltaTime;
            moveFactor = 1f - Mathf.Clamp01(landRecoveryTimer / landRecoveryTime);
        }
            Vector3 velocity = moveDir * (moveSpeed * moveFactor) + Vector3.up * verticalVelocity;
        Vector3 totalMove = velocity * Time.deltaTime;

       
        
        controller.Move(totalMove);
        HandleRotation();
        wasGrounded = grounded;
        
    }

    private void HandleRotation()
    {
        if (isAiming)
        {
            Quaternion target = Quaternion.Euler(0f, cameraTarget.Yaw, 0f);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, target, aimTurnSpeed * Time.deltaTime);
        }
        else if (isWalking)
        {
            transform.forward = Vector3.Slerp(transform.forward, moveDir, Time.deltaTime * rotateSpeed);
        }
    }

    private bool CheckGrounded()
    {
        Vector3 origin = transform.position + Vector3.up * GROUND_CHECK_RADIUS;
        Debug.DrawRay(origin, Vector3.down * GROUND_CHECK_DISTANCE, Color.green);
        return Physics.SphereCast(origin, GROUND_CHECK_RADIUS, Vector3.down, out RaycastHit hit, GROUND_CHECK_DISTANCE, wallLayer, QueryTriggerInteraction.Ignore);
        
    }

    

    public bool GroundContact()
    {
        return CheckGrounded();
    }
    public bool JumpRequested()
    {
        return jumpRequested;
    }
  
  
    private Vector3 GetWallPushDirection()
    {
        Vector3 origin = transform.position + Vector3.up * wallRayHeight;
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
        Vector3 pushDirection = Vector3.zero;
        foreach (Vector3 direction in directions)
        {
            if (Physics.Raycast(origin, direction, out RaycastHit hit, wallRayDistance, wallLayer, QueryTriggerInteraction.Ignore))
            {
               if (Mathf.Abs(hit.normal.y) < 0.3f)
                {
                    pushDirection += hit.normal;
                }
            }
            Debug.DrawRay(origin, direction * wallRayDistance, Color.red);
        }
        return pushDirection.normalized;
    }
    private void SetSelectedWall(BaseWall selectedWall)
    {
        this.selectedWall = selectedWall;
        OnSelectedWallChanged?.Invoke(this, new OnSelectedWallChangedEventArgs { selectedWall = selectedWall });
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
    public bool IsWalking()
    {
        return isWalking;
    }

}
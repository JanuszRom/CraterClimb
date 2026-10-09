using UnityEngine;

public class PlayerAnimator : MonoBehaviour
{
    [SerializeField] private Player player;
    private const string IS_WALKING = "IsWalking";
    private const string GROUND_CONTACT = "GroundContact";
    private const string JUMP_REQUESTED = "JumpRequested";
    private const string FORWARD_SPEED = "ForwardSpeed";
    private const string IS_AIMING = "IsAiming";
    private const string AIM_X = "AimX";
    private const string AIM_Y = "AimY";
    private Animator animator;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        
    }
    private void Update()
    {
        animator.SetFloat(FORWARD_SPEED, player.targetSpeed, 0.2f, Time.deltaTime);
        animator.SetBool(GROUND_CONTACT, player.GroundContact());
        animator.SetBool(JUMP_REQUESTED, player.JumpRequested());
        animator.SetFloat(AIM_X, player.aimX, 0.1f, Time.deltaTime);
        animator.SetFloat(AIM_Y, player.aimY, 0.1f, Time.deltaTime);
        animator.SetBool(IS_AIMING, player.isAiming);
    }
}

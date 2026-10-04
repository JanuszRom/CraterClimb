using UnityEngine;

public class PlayerAnimator : MonoBehaviour
{
    [SerializeField] private Player player;
    private const string IS_WALKING = "IsWalking";
    private const string GROUND_CONTACT = "GroundContact";
    private const string JUMP_REQUESTED = "JumpRequested";
    private const string FORWARD_SPEED = "ForwardSpeed";
    private Animator animator;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        
    }
    private void Update()
    {
        animator.SetFloat(FORWARD_SPEED, player.speed);
        animator.SetBool(GROUND_CONTACT, player.GroundContact());
        animator.SetBool(JUMP_REQUESTED, player.JumpRequested());
    }
}

using UnityEngine;

public class JellyfishAnimator : MonoBehaviour
{
    [SerializeField] private Jellyfish jellyfish;
    private const string PULSE = "Pulse";
    private Animator animator;
    private void Awake()
    {
        animator = GetComponent<Animator>();
    }
    private void Update()
    {
        if (jellyfish.FlapTimer() <= 0f)
        {
            animator.SetTrigger(PULSE);
        }
    }
}

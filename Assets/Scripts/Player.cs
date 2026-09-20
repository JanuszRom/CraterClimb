using UnityEngine;

public class Player : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 7f;
    [SerializeField] private float rotateSpeed = 7f;
    [SerializeField] private GameInput gameInput;

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

    }
}

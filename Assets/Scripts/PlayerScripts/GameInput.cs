using UnityEngine;
using System;
using UnityEngine.InputSystem;

public class GameInput : MonoBehaviour
{
    public event EventHandler OnJumpAction;
    public event EventHandler OnInteractAction;
    public event Action<bool> OnAim;

    private PlayerInputActions playerInputActions;
    private void Awake()
    {

        playerInputActions = new PlayerInputActions();
        playerInputActions.Player.Enable();
        playerInputActions.Player.Jump.performed += Jump_performed;
        playerInputActions.Player.Interact.performed += Interact_performed;
        playerInputActions.Player.Aim.performed += Aim_performed;
        playerInputActions.Player.Aim.canceled += Aim_canceled;
    }
    private void Aim_performed(InputAction.CallbackContext obj)
    {
        OnAim?.Invoke(true);
    }
    private void Aim_canceled(InputAction.CallbackContext obj)
    {
        OnAim?.Invoke(false);
    }

    private void Jump_performed(InputAction.CallbackContext obj)
    {
        OnJumpAction?.Invoke(this, EventArgs.Empty);
    }
    private void Interact_performed(InputAction.CallbackContext obj)
    {
        OnInteractAction?.Invoke(this, EventArgs.Empty);
    }
    private void OnDestroy()
    {
        playerInputActions.Player.Jump.performed -= Jump_performed;
        playerInputActions.Player.Interact.performed -= Interact_performed;
        playerInputActions.Player.Aim.performed -= Aim_performed;
        playerInputActions.Dispose();
    }

    public Vector2 GetMovementVectorNormalized()
    {
        Vector2 inputVector = playerInputActions.Player.Move.ReadValue<Vector2>();
        inputVector = inputVector.normalized;

        return inputVector;
    }
}
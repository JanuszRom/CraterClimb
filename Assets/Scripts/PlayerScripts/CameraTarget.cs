using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Cinemachine;
public class CameraTarget : MonoBehaviour
{
    [Header("Look")]
    [SerializeField] private float sensitivity = .1f;
    [SerializeField] private Vector2 freePitchRange = new Vector2(-30f, 60f);
    [SerializeField] private Vector2 aimPitchRange = new Vector2(-50f, 60f);

    [Header("Aim Rig")]
    [SerializeField] private Transform aimYawPivot;
    [SerializeField] private Transform aimLookPoint;
    [SerializeField] private float aimLookDistance = 30f;
    [SerializeField] private float aimPitchHeightInfluence = 0f;
    [Header("Cinemachine")]
    [SerializeField] private CinemachineCamera freeCamera;
    [SerializeField] private CinemachineCamera aimCamera;
    private int activePriority = 20;
    private int inactivePriority = 10;
    public float Yaw { get; private set; }
   public bool IsAiming { get; private set; }
    private float pitch = 15f;
    private float aimPivotBaseHeight;

    private void Start()
    {
        Yaw = transform.eulerAngles.y;
        aimPivotBaseHeight = aimYawPivot.localPosition.y;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        SetAiming(false);
    }

    private void LateUpdate()
    {
        Vector2 delta = Mouse.current.delta.ReadValue();
        Vector2 range = IsAiming ? aimPitchRange : freePitchRange;
        pitch = Mathf.Clamp(pitch - delta.y * sensitivity, range.x, range.y);
        Yaw += delta.x * sensitivity;
        Quaternion look = Quaternion.Euler(pitch, Yaw, 0f);

        transform.rotation = look;

        aimYawPivot.rotation = Quaternion.Euler(0f, Yaw, 0f);
        Vector3 local = aimYawPivot.localPosition;
        local.y = aimPivotBaseHeight + pitch * aimPitchHeightInfluence;
        aimYawPivot.localPosition = local;

        aimLookPoint.position = aimYawPivot.position + aimYawPivot.forward * aimLookDistance;
    }

    public void SetAiming(bool aiming)
    {
        IsAiming = aiming;
        freeCamera.Priority = aiming ? inactivePriority : activePriority;
        aimCamera.Priority = aiming ? activePriority : inactivePriority;
        
    }
}

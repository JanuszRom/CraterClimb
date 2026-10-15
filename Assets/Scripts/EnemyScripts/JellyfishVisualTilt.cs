using UnityEngine;

public class JellyfishVisualTilt : MonoBehaviour
{
    [SerializeField] private Rigidbody rb;
    [SerializeField] private Transform visualPivot;
    [Header("Tilt")]
    [SerializeField] private float maxPitch = 20f;
    [SerializeField] private float maxRoll = 15f;
    [SerializeField] private float maxHorizontalSpeed = 4f;
    [SerializeField] private float tiltResponsiveness = 5f;

    private Quaternion neutralRotation;

    private void Awake()
    {
        neutralRotation = visualPivot.localRotation;

    }

    private void LateUpdate()
    {
        Vector3 localVelocity = transform.InverseTransformDirection(rb.linearVelocity);
        float forward = Mathf.Clamp(localVelocity.z / maxHorizontalSpeed, -1f, 1f);
        float sideways = Mathf.Clamp(localVelocity.x / maxHorizontalSpeed, -1f, 1f);
        float pitch = forward * maxPitch;
        float roll = sideways * maxRoll;
        Quaternion targetRotation = Quaternion.Euler(pitch, 0f, roll) * neutralRotation;
        visualPivot.localRotation = Quaternion.Slerp(visualPivot.localRotation, targetRotation, tiltResponsiveness * Time.deltaTime);
    }
}

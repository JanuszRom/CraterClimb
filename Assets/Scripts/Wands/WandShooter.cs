using UnityEngine;

public class WandShooter : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform muzzle;
    [SerializeField] private Projectile projectilePrefab;

    [Header("Aiming")]
    [SerializeField] private LayerMask aimMask;
    [SerializeField] private float maxAimDistance = 100f;
    [Header("Shot")]
    [SerializeField] private float cooldown = 0.5f;
    [SerializeField] private float shotSpeed = 20f;
    [SerializeField] private float minFlightTime = 0.5f;
    [SerializeField] private float maxFlightTime = 2f;

    private GameInput gameInput;
    private CameraTarget cameraTarget;
    private Camera cam;
    private float nextFireTime;


    private void Awake()
    {
        gameInput = FindAnyObjectByType<GameInput>();
        cameraTarget = FindAnyObjectByType<CameraTarget>();
        cam = Camera.main;
    }
    private void OnEnable()
    {
        if (gameInput != null) gameInput.OnShoot += GameInput_OnShoot;
    }
    private void OnDisable()
    {
        if (gameInput != null) gameInput.OnShoot -= GameInput_OnShoot;
    }

    private void GameInput_OnShoot(object sender, System.EventArgs e)
    {
        if (!cameraTarget.IsAiming) return;
        if (Time.time < nextFireTime) return;
        nextFireTime = Time.time + cooldown;

        Vector3 aimPoint = GetAimPoint();
        Vector3 velocity = CalculateLaunchVelocity(muzzle.position, aimPoint);

        Projectile proj = Instantiate(projectilePrefab, muzzle.position, Quaternion.LookRotation(velocity));
        proj.Launch(velocity);

    }

    private Vector3 GetAimPoint()
    {
        Ray ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        if (Physics.Raycast(ray, out RaycastHit hit, maxAimDistance, aimMask, QueryTriggerInteraction.Ignore)) return hit.point;
        return ray.GetPoint(maxAimDistance);
    }

    private Vector3 CalculateLaunchVelocity(Vector3 start, Vector3 target)
    {
        Vector3 offset = target - start;
        float t = Mathf.Clamp(offset.magnitude / shotSpeed, minFlightTime, maxFlightTime);
        return (offset - 0.5f * Physics.gravity * t * t) / t;
    }
}

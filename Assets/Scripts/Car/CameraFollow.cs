using UnityEngine;

// Smooth chase camera behind the car. The field of view widens with the speed.
public class CameraFollow : MonoBehaviour
{
    [SerializeField] private CarController target;
    [SerializeField] private float distance = 7.5f;
    [SerializeField] private float height = 2.8f;
    [SerializeField] private float lookHeight = 1.1f;
    [SerializeField] private float lookAhead = 4f;
    [SerializeField] private float positionSmoothTime = 0.08f;
    [SerializeField] private float rotationSmoothTime = 0.25f;
    [SerializeField] private float baseFieldOfView = 60f;
    [SerializeField] private float maxFieldOfView = 74f;

    private Camera cam;
    private Vector3 velocity;
    private float yaw;
    private float yawVelocity;

    private void Start()
    {
        cam = GetComponent<Camera>();
        yaw = target.transform.eulerAngles.y;
        SnapToTarget();
    }

    private void LateUpdate()
    {
        yaw = Mathf.SmoothDampAngle(yaw, target.transform.eulerAngles.y, ref yawVelocity, rotationSmoothTime);
        transform.position = Vector3.SmoothDamp(transform.position, DesiredPosition(), ref velocity, positionSmoothTime);

        Vector3 forward = Quaternion.Euler(0f, yaw, 0f) * Vector3.forward;
        Vector3 lookPoint = target.transform.position + Vector3.up * lookHeight + forward * lookAhead;
        transform.rotation = Quaternion.LookRotation(lookPoint - transform.position);

        float speed01 = Mathf.Clamp01(target.Speed / target.MaxSpeed);
        cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, Mathf.Lerp(baseFieldOfView, maxFieldOfView, speed01), 3f * Time.deltaTime);
    }

    private Vector3 DesiredPosition()
    {
        Vector3 back = Quaternion.Euler(0f, yaw, 0f) * Vector3.back;
        return target.transform.position + back * distance + Vector3.up * height;
    }

    public void SnapToTarget()
    {
        yaw = target.transform.eulerAngles.y;
        transform.position = DesiredPosition();
        velocity = Vector3.zero;
    }
}

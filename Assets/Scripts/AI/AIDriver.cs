using UnityEngine;

// Waypoint AI for an opponent car. It drives the same CarController as the player:
// - steers towards a point a bit further along the racing line (the faster, the further ahead),
// - picks its speed from the corners ahead (the tighter the corner, the slower) and brakes in time,
// - backs up when it is stuck and respawns at its last checkpoint if that does not help.
public class AIDriver : MonoBehaviour
{
    [SerializeField] private CarController car;
    [SerializeField] private Racer racer;
    [SerializeField] private TrackPath path;

    [Header("Difficulty")]
    [Tooltip("Scales all target speeds. 1 = the car's full potential, lower = slower opponent.")]
    [SerializeField, Range(0.5f, 1.1f)] private float skill = 0.9f;
    [Tooltip("How much of the tyre grip the AI uses in corners.")]
    [SerializeField, Range(0.5f, 1f)] private float cornerGripUsage = 0.9f;
    [SerializeField] private float laneOffset = 0f;     // meters to the right of the racing line

    [Header("Driving")]
    [SerializeField] private float lookAheadMin = 7f;
    [SerializeField] private float lookAheadPerSpeed = 0.3f;
    [SerializeField] private float brakingScanDistance = 90f;
    [SerializeField] private float avoidDistance = 4f;
    [SerializeField] private LayerMask carMask;

    private int waypoint;
    private float avoidOffset;
    private float avoidSide;
    private float avoidHold;
    private float blockedSpeed = float.MaxValue;
    private float stuckTimer;
    private float reverseTimer;
    private int reverseCount;
    private float flippedTimer;
    private float offTrackTimer;

    private void Start()
    {
        waypoint = path.ClosestIndex(transform.position);
    }

    private void FixedUpdate()
    {
        if (!car.ControlsEnabled)
        {
            car.SetInput(0f, 0f, false);
            return;
        }

        float dt = Time.fixedDeltaTime;
        Vector3 position = transform.position;

        // Keep track of where we are on the racing line (after a respawn search the whole line again).
        if ((path.GetPoint(waypoint) - position).sqrMagnitude > 30f * 30f)
            waypoint = path.ClosestIndex(position);
        waypoint = path.AdvanceIndex(waypoint, position);

        float speed = car.ForwardSpeed;

        // Steering: pure pursuit towards a point ahead on the line.
        float lookAhead = lookAheadMin + Mathf.Abs(speed) * lookAheadPerSpeed;
        UpdateAvoidance(dt);
        Vector3 target = path.PointAhead(waypoint, lookAhead);
        Vector3 lineRight = Vector3.Cross(Vector3.up, path.DirectionAt(waypoint));
        target += lineRight * path.ClampOffset(waypoint, laneOffset + avoidOffset);

        Vector3 local = transform.InverseTransformPoint(target);
        float angleToTarget = Mathf.Atan2(local.x, local.z);
        float wantedSteerAngle = Mathf.Atan2(2f * car.WheelBase * Mathf.Sin(angleToTarget), lookAhead) * Mathf.Rad2Deg;
        float steering = Mathf.Clamp(wantedSteerAngle / car.CurrentSteerLimit, -1f, 1f);

        // Speed: slow enough for every corner ahead.
        float targetSpeed = skill * path.TargetSpeed(waypoint, car.SideGrip * cornerGripUsage,
            car.BrakeDeceleration * 0.7f, car.MaxSpeed, brakingScanDistance);
        targetSpeed = Mathf.Min(targetSpeed, blockedSpeed);   // don't crash into a slower car right in front of us
        float throttle = Mathf.Clamp((targetSpeed - speed) * 0.5f, -1f, 1f);
        if (speed < targetSpeed - 1f)
            throttle = 1f;

        // Target far behind us (e.g. after a spin) -> turn around slowly.
        if (local.z < 0f && Mathf.Abs(speed) < 8f)
        {
            throttle = 0.6f;
            steering = Mathf.Sign(local.x);
        }

        HandleProblems(dt, ref throttle, ref steering, position);
        car.SetInput(throttle, steering, false);
    }

    // Overtaking: if a car is in front of us in our lane, move to the side where it is not.
    // The chosen side is kept for a moment, so the car does not swing back and forth.
    private void UpdateAvoidance(float dt)
    {
        float wanted = 0f;
        float closest = float.MaxValue;
        blockedSpeed = float.MaxValue;
        Collider[] nearby = Physics.OverlapSphere(transform.position + transform.forward * 7f, 10f, carMask, QueryTriggerInteraction.Ignore);
        foreach (Collider other in nearby)
        {
            Rigidbody body = other.attachedRigidbody;
            if (body == null || body.transform == transform)
                continue;
            Vector3 local = transform.InverseTransformPoint(body.position);
            if (local.z < -1f || local.z > 16f || Mathf.Abs(local.x) > 4.5f)
                continue;
            if (local.z < closest)
            {
                closest = local.z;
                wanted = (local.x > 0f ? -1f : 1f) * avoidDistance;
                // Still right behind it -> not faster than that car until we are next to it.
                if (local.z > 0f && local.z < 10f && Mathf.Abs(local.x) < 2.3f)
                    blockedSpeed = Mathf.Max(0f, Vector3.Dot(body.linearVelocity, transform.forward)) + 1.5f;
            }
        }

        if (wanted != 0f)
        {
            avoidSide = wanted;
            avoidHold = 1.2f;
        }
        else if (avoidHold > 0f)
        {
            avoidHold -= dt;
            wanted = avoidSide;
        }
        avoidOffset = Mathf.MoveTowards(avoidOffset, wanted, 6f * dt);
    }

    private void HandleProblems(float dt, ref float throttle, ref float steering, Vector3 position)
    {
        // Currently backing up after getting stuck.
        if (reverseTimer > 0f)
        {
            reverseTimer -= dt;
            throttle = -1f;
            steering = -steering;
            return;
        }

        bool wantsToMove = throttle > 0.2f;
        if (wantsToMove && Mathf.Abs(car.ForwardSpeed) < 1.5f)
            stuckTimer += dt;
        else
            stuckTimer = Mathf.Max(0f, stuckTimer - dt);

        if (car.Speed > 8f)
            reverseCount = 0;

        flippedTimer = transform.up.y < 0.3f ? flippedTimer + dt : 0f;
        float distanceToLine = Vector3.Distance(position, path.GetPoint(waypoint));
        offTrackTimer = distanceToLine > 16f ? offTrackTimer + dt : 0f;

        if (stuckTimer > 1.2f)
        {
            stuckTimer = 0f;
            reverseCount++;
            reverseTimer = 1.2f;
        }

        if (reverseCount > 2 || flippedTimer > 2f || offTrackTimer > 3f)
        {
            racer.ResetToLastCheckpoint();
            waypoint = path.ClosestIndex(transform.position);
            reverseCount = 0;
            reverseTimer = 0f;
            flippedTimer = 0f;
            offTrackTimer = 0f;
        }
    }
}

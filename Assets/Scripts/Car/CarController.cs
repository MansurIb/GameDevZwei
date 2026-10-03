using UnityEngine;

// Arcade car physics on a Rigidbody.
// Every wheel is a raycast spring (suspension), all driving forces are added to the Rigidbody.
// The car itself does not read the keyboard: PlayerCarInput (player) or AIDriver (opponents) call SetInput().
[RequireComponent(typeof(Rigidbody))]
public class CarController : MonoBehaviour
{
    [Header("Engine")]
    [SerializeField] private float maxSpeed = 36f;              // m/s (36 m/s = 130 km/h)
    [SerializeField] private float maxReverseSpeed = 9f;
    [SerializeField] private float acceleration = 11f;          // m/s² when starting from standstill
    [SerializeField] private float brakeDeceleration = 20f;
    [SerializeField] private float rollOutDeceleration = 2.5f;  // used when no key is pressed
    [SerializeField] private float airResistance = 0.001f;

    [Header("Steering")]
    [SerializeField] private float maxSteerAngle = 35f;         // full steering angle at low speed
    [SerializeField] private float minSteerAngle = 2.5f;        // steering angle never gets smaller than this
    [SerializeField] private float steerGripFactor = 1.25f;     // > 1 lets the car slide a little at full lock
    [SerializeField] private float steerSpeed = 5f;             // how fast the wheels turn in
    [SerializeField] private float wheelBase = 2.4f;
    [SerializeField] private float sideGrip = 18.5f;            // max sideways acceleration (m/s²) before sliding
    [SerializeField] private float yawResponse = 10f;

    [Header("Handbrake")]
    [SerializeField] private float handbrakeDeceleration = 20f;
    [SerializeField] private float handbrakeGrip = 0.35f;       // part of the side grip that is left while drifting

    [Header("Suspension (order: front left, front right, rear left, rear right)")]
    [SerializeField] private Transform[] suspensionPoints = new Transform[4];
    [SerializeField] private Transform[] wheelVisuals = new Transform[4];
    [SerializeField] private float wheelRadius = 0.42f;
    [SerializeField] private float restLength = 0.4f;
    [SerializeField] private float springStrength = 32000f;
    [SerializeField] private float springDamper = 3200f;
    [SerializeField] private LayerMask groundMask = ~0;

    [Header("Stability")]
    [SerializeField] private Vector3 centerOfMass = new Vector3(0f, 0.3f, 0f);
    [SerializeField] private float downforce = 0.08f;           // extra m/s² per m/s of speed
    [SerializeField] private float airStabilization = 6f;       // keeps the car level while it is in the air
    [SerializeField] private float airGravity = 8f;             // extra gravity in the air, keeps jumps short

    private Rigidbody rb;
    private float throttleInput;
    private float steerInput;
    private bool handbrakeInput;
    private float steer;
    private float frontSpin;
    private float rearSpin;
    private readonly float[] hitDistance = new float[4];
    private Vector3[] wheelRestPositions;
    private int wheelsOnGround;
    private Vector3 groundNormal = Vector3.up;

    public bool ControlsEnabled { get; set; } = true;
    public float Speed => rb.linearVelocity.magnitude;
    public float ForwardSpeed => Vector3.Dot(rb.linearVelocity, transform.forward);
    public float MaxSpeed => maxSpeed;
    public float Throttle => ControlsEnabled ? throttleInput : 0f;
    public bool IsGrounded => wheelsOnGround > 0;
    public float WheelBase => wheelBase;
    public float SideGrip => sideGrip;
    public float BrakeDeceleration => brakeDeceleration;

    // Steering depends on the speed: the faster the car, the smaller the angle,
    // so that a full turn never asks for more grip than the tyres have (no "shopping cart" at top speed).
    // Standing still the car can't turn at all, because the yaw rate below is proportional to the speed.
    public float CurrentSteerLimit
    {
        get
        {
            float v = Mathf.Max(Mathf.Abs(ForwardSpeed), 0.1f);
            float gripLimit = Mathf.Atan(steerGripFactor * sideGrip * wheelBase / (v * v)) * Mathf.Rad2Deg;
            return Mathf.Clamp(gripLimit, minSteerAngle, maxSteerAngle);
        }
    }

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.centerOfMass = centerOfMass;
        wheelRestPositions = new Vector3[wheelVisuals.Length];
        for (int i = 0; i < wheelVisuals.Length; i++)
        {
            wheelRestPositions[i] = wheelVisuals[i].localPosition;
            hitDistance[i] = restLength + wheelRadius;
        }
    }

    public void SetInput(float throttle, float steering, bool handbrake)
    {
        throttleInput = Mathf.Clamp(throttle, -1f, 1f);
        steerInput = Mathf.Clamp(steering, -1f, 1f);
        handbrakeInput = handbrake;
    }

    // Teleports the car and removes all of its velocity.
    public void ResetTo(Vector3 position, Quaternion rotation)
    {
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.position = position;
        rb.rotation = rotation;
        transform.SetPositionAndRotation(position, rotation);
        steer = 0f;
    }

    private void FixedUpdate()
    {
        float dt = Time.fixedDeltaTime;
        float throttle = ControlsEnabled ? throttleInput : 0f;
        bool handbrake = ControlsEnabled && handbrakeInput;
        steer = Mathf.MoveTowards(steer, ControlsEnabled ? steerInput : 0f, steerSpeed * dt);

        UpdateSuspension();

        if (wheelsOnGround == 0)
        {
            // In the air: turn the car upright and damp the tumbling so it lands on its wheels.
            Vector3 w = rb.angularVelocity;
            Vector3 tumble = w - Vector3.Project(w, transform.up);
            rb.AddTorque(Vector3.Cross(transform.up, Vector3.up) * airStabilization - tumble * 2f, ForceMode.Acceleration);
            rb.AddForce(Vector3.down * airGravity, ForceMode.Acceleration);
            return;
        }

        float contact = wheelsOnGround / 4f;
        Vector3 forward = Vector3.ProjectOnPlane(transform.forward, groundNormal).normalized;
        Vector3 right = Vector3.Cross(groundNormal, forward);
        Vector3 velocity = rb.linearVelocity;
        float forwardSpeed = Vector3.Dot(velocity, forward);
        float sideSpeed = Vector3.Dot(velocity, right);

        // Accelerate, brake, reverse or roll out.
        float accel;
        bool slowingDown;
        if (handbrake)
        {
            accel = -Mathf.Sign(forwardSpeed) * handbrakeDeceleration;
            slowingDown = true;
        }
        else if (throttle > 0.01f)
        {
            slowingDown = forwardSpeed < -0.5f;
            accel = slowingDown ? brakeDeceleration * throttle : acceleration * throttle * EngineCurve(forwardSpeed / maxSpeed);
        }
        else if (throttle < -0.01f)
        {
            slowingDown = forwardSpeed > 0.5f;
            accel = slowingDown ? brakeDeceleration * throttle : acceleration * 0.6f * throttle * EngineCurve(-forwardSpeed / maxReverseSpeed);
        }
        else
        {
            accel = -Mathf.Sign(forwardSpeed) * rollOutDeceleration;
            slowingDown = true;
        }
        accel -= airResistance * forwardSpeed * Mathf.Abs(forwardSpeed);
        // Braking and rolling out stop the car, they never push it backwards (only reversing does that).
        if (slowingDown && Mathf.Abs(accel * dt) > Mathf.Abs(forwardSpeed))
            accel = -forwardSpeed / dt;
        rb.AddForce(forward * (accel * contact), ForceMode.Acceleration);

        // Tyre grip: remove sideways sliding, but only up to the grip limit (above that the car drifts).
        float maxSide = sideGrip * (handbrake ? handbrakeGrip : 1f);
        float sideAccel = Mathf.Clamp(-sideSpeed / dt, -maxSide, maxSide);
        rb.AddForce(right * (sideAccel * contact), ForceMode.Acceleration);

        // Steering (bicycle model): yaw rate = speed * tan(steer angle) / wheel base.
        float steerAngle = steer * CurrentSteerLimit;
        float targetYawRate = forwardSpeed * Mathf.Tan(steerAngle * Mathf.Deg2Rad) / wheelBase;
        if (handbrake)
            targetYawRate *= 1.4f;
        float yawRate = Vector3.Dot(rb.angularVelocity, groundNormal);
        rb.AddTorque(groundNormal * ((targetYawRate - yawRate) * yawResponse * contact), ForceMode.Acceleration);

        rb.AddForce(-groundNormal * (downforce * Mathf.Abs(forwardSpeed) * contact), ForceMode.Acceleration);
    }

    private static float EngineCurve(float speedRatio)
    {
        return Mathf.Clamp01(1f - speedRatio * speedRatio);
    }

    private void UpdateSuspension()
    {
        wheelsOnGround = 0;
        Vector3 normalSum = Vector3.zero;
        float maxDistance = restLength + wheelRadius;

        for (int i = 0; i < suspensionPoints.Length; i++)
        {
            Vector3 origin = suspensionPoints[i].position;
            if (Physics.Raycast(origin, -transform.up, out RaycastHit hit, maxDistance, groundMask, QueryTriggerInteraction.Ignore))
            {
                wheelsOnGround++;
                normalSum += hit.normal;
                hitDistance[i] = hit.distance;

                float compression = maxDistance - hit.distance;
                float springVelocity = Vector3.Dot(rb.GetPointVelocity(origin), transform.up);
                float force = compression * springStrength - springVelocity * springDamper;
                rb.AddForceAtPosition(transform.up * Mathf.Max(0f, force), origin);
            }
            else
            {
                hitDistance[i] = maxDistance;
            }
        }
        groundNormal = wheelsOnGround > 0 ? normalSum.normalized : Vector3.up;
    }

    // Wheels spin with the speed, front wheels steer and every wheel follows its suspension.
    private void LateUpdate()
    {
        float degreesPerMeter = Mathf.Rad2Deg / wheelRadius;
        float distance = ForwardSpeed * Time.deltaTime;
        frontSpin = (frontSpin + distance * degreesPerMeter) % 360f;
        if (!(ControlsEnabled && handbrakeInput && IsGrounded))
            rearSpin = (rearSpin + distance * degreesPerMeter) % 360f;

        float visualSteer = steer * Mathf.Max(CurrentSteerLimit, 15f);
        for (int i = 0; i < wheelVisuals.Length; i++)
        {
            bool front = i < 2;
            Vector3 position = wheelRestPositions[i];
            position.y = suspensionPoints[i].localPosition.y - (hitDistance[i] - wheelRadius);
            wheelVisuals[i].localPosition = position;
            wheelVisuals[i].localRotation = front
                ? Quaternion.Euler(0f, visualSteer, 0f) * Quaternion.Euler(frontSpin, 0f, 0f)
                : Quaternion.Euler(rearSpin, 0f, 0f);
        }
    }
}

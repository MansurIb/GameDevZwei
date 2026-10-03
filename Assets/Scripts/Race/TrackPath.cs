using UnityEngine;

// Waypoints along the racing line (the children of this object, in driving order).
// The AI steers along them; the curvature of the line tells it how fast it may take a corner.
public class TrackPath : MonoBehaviour
{
    [SerializeField] private int curvatureSpan = 3;          // neighbours used to measure the corner radius
    [SerializeField] private float[] roomLeft;               // free space (m) left of each waypoint before the barrier
    [SerializeField] private float[] roomRight;              // free space (m) right of each waypoint
    [SerializeField] private Color gizmoColor = new Color(1f, 0.8f, 0f);

    private Vector3[] points;
    private float[] segmentLength;
    private float[] cornerRadius;

    public int Count => points.Length;

    private void Awake()
    {
        Build();
    }

    private void Build()
    {
        int count = transform.childCount;
        points = new Vector3[count];
        for (int i = 0; i < count; i++)
            points[i] = transform.GetChild(i).position;

        segmentLength = new float[count];
        for (int i = 0; i < count; i++)
            segmentLength[i] = Vector3.Distance(points[i], GetPoint(i + 1));

        // Radius of the circle through three points of the line (flat, ignoring height).
        float[] raw = new float[count];
        for (int i = 0; i < count; i++)
            raw[i] = CircleRadius(Flat(GetPoint(i - curvatureSpan)), Flat(points[i]), Flat(GetPoint(i + curvatureSpan)));

        // Use the tightest radius nearby, so the AI is not surprised in the middle of a corner.
        cornerRadius = new float[count];
        for (int i = 0; i < count; i++)
        {
            float r = raw[i];
            for (int k = -2; k <= 2; k++)
                r = Mathf.Min(r, raw[Wrap(i + k)]);
            cornerRadius[i] = r;
        }
    }

    public int Wrap(int i)
    {
        return ((i % points.Length) + points.Length) % points.Length;
    }

    public Vector3 GetPoint(int i)
    {
        return points[Wrap(i)];
    }

    public Vector3 DirectionAt(int i)
    {
        return (GetPoint(i + 1) - GetPoint(i)).normalized;
    }

    // Keeps a sideways offset from the racing line inside the track.
    public float ClampOffset(int i, float offset)
    {
        int k = Wrap(i);
        float left = roomLeft != null && k < roomLeft.Length ? roomLeft[k] : 2f;
        float right = roomRight != null && k < roomRight.Length ? roomRight[k] : 2f;
        return Mathf.Clamp(offset, -left, right);
    }

    public int ClosestIndex(Vector3 position)
    {
        int best = 0;
        float bestDistance = float.MaxValue;
        for (int i = 0; i < points.Length; i++)
        {
            float d = (points[i] - position).sqrMagnitude;
            if (d < bestDistance)
            {
                bestDistance = d;
                best = i;
            }
        }
        return best;
    }

    // Moves the index forward while the next waypoint is closer than the current one.
    public int AdvanceIndex(int index, Vector3 position)
    {
        for (int step = 0; step < 30; step++)
        {
            float current = (GetPoint(index) - position).sqrMagnitude;
            float next = (GetPoint(index + 1) - position).sqrMagnitude;
            if (next > current)
                break;
            index = Wrap(index + 1);
        }
        return index;
    }

    // Point on the line 'distance' meters after waypoint 'index'.
    public Vector3 PointAhead(int index, float distance)
    {
        int i = Wrap(index);
        for (int step = 0; step < points.Length; step++)
        {
            if (distance <= segmentLength[i])
                return Vector3.Lerp(points[i], GetPoint(i + 1), distance / Mathf.Max(segmentLength[i], 0.001f));
            distance -= segmentLength[i];
            i = Wrap(i + 1);
        }
        return points[i];
    }

    // Highest speed that still allows braking down to the corner speed of every corner ahead.
    public float TargetSpeed(int index, float lateralAcceleration, float braking, float maxSpeed, float scanDistance)
    {
        float result = maxSpeed;
        float distance = 0f;
        int i = Wrap(index);
        for (int step = 0; step < points.Length && distance < scanDistance; step++)
        {
            float cornerSpeed = Mathf.Sqrt(lateralAcceleration * cornerRadius[i]);
            float allowed = Mathf.Sqrt(cornerSpeed * cornerSpeed + 2f * braking * distance);
            result = Mathf.Min(result, allowed);
            distance += segmentLength[i];
            i = Wrap(i + 1);
        }
        return result;
    }

    private static Vector3 Flat(Vector3 v)
    {
        return new Vector3(v.x, 0f, v.z);
    }

    private static float CircleRadius(Vector3 a, Vector3 b, Vector3 c)
    {
        float ab = Vector3.Distance(a, b);
        float bc = Vector3.Distance(b, c);
        float ca = Vector3.Distance(c, a);
        float area2 = Vector3.Cross(b - a, c - a).magnitude;   // twice the triangle area
        if (area2 < 0.0001f)
            return 10000f;
        return Mathf.Min(10000f, ab * bc * ca / (2f * area2));
    }

    private void OnDrawGizmos()
    {
        int count = transform.childCount;
        if (count < 2)
            return;
        Gizmos.color = gizmoColor;
        for (int i = 0; i < count; i++)
        {
            Vector3 a = transform.GetChild(i).position;
            Vector3 b = transform.GetChild((i + 1) % count).position;
            Gizmos.DrawLine(a, b);
            Gizmos.DrawSphere(a, 0.35f);
        }
    }
}

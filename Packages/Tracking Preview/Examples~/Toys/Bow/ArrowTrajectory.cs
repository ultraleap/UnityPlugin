using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class ArrowTrajectory : MonoBehaviour
{
    public ObjectLauncher launcher;

    public int resolution;
    public Transform startPoint;

    float velocity;

    private float angle;
    private LineRenderer lr;
    private float g;
    private float rads;

    void Start()
    {
        lr = this.GetComponent<LineRenderer>();
        g = Mathf.Abs(Physics.gravity.y);
    }

    void RenderArc()
    {
        angle = -startPoint.rotation.eulerAngles.x;
        lr.positionCount = resolution + 1;
        lr.SetPositions(CalculateArcPoints());
    }

    void Update()
    {
        CalculateVelocity();

        if (velocity < 1)
            return;

        this.transform.position = startPoint.position;
        RenderArc();
    }

    void CalculateVelocity()
    {
        Vector3 dir = launcher.startPos - launcher.lastPinchPos;
        velocity = dir.magnitude * launcher.launchVelocityMultiplier;
    }

    Vector3[] CalculateArcPoints()
    {
        Vector3[] arcPoints = new Vector3[resolution + 1];

        rads = Mathf.Deg2Rad * angle;

        float maxDistance = ((velocity * Mathf.Cos(rads)) / g) * (velocity * Mathf.Sin(rads) + Mathf.Sqrt(Mathf.Pow(velocity * Mathf.Sin(rads), 2) + 2 * g * startPoint.transform.position.y));

        for (int i = 0; i <= resolution; i++)
        {
            float t = (float)i / (float)resolution;
            arcPoints[i] = RotatePointAroundPivot(CalculatePoint(t, maxDistance) + transform.position, startPoint.position, new Vector3(0, startPoint.eulerAngles.y, 0));
        }

        return arcPoints;
    }

    Vector3 CalculatePoint(float t, float maxDist)
    {
        float x = t * maxDist;
        float y = x * Mathf.Tan(rads) - ((g * x * x) / (2 * Mathf.Pow(velocity * Mathf.Cos(rads), 2)));
        float z = 0f;

        return new Vector3(z, y, x);
    }

    public Vector3 RotatePointAroundPivot(Vector3 point, Vector3 pivot, Vector3 angles)
    {
        return Quaternion.Euler(angles) * (point - pivot) + pivot;
    }
}
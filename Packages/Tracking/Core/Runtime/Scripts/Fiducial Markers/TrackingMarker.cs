using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TrackingMarker : MonoBehaviour
{
    [Tooltip("The AprilTag marker ID associated with this marker." +
        "\n\nNote: This must be unique within the scene")]
    public int id;

    [HideInInspector]
    public Vector3 poitionOffset;

    [HideInInspector]
    public Quaternion rotationOffset;
}

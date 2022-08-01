using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Leap.Unity;

public abstract class PoseDetectionBase :MonoBehaviour
{
    public CapsuleHand capsuleHand;

    public float maxAngle = 5;
    // error angle in degrees
    public float[] boneErrorPitch;
    public float[] boneErrorYaw;
}

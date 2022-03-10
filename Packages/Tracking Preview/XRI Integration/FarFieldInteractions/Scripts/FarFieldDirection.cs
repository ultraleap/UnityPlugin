using System;
using System.Collections.Generic;
using Leap;
using Leap.Unity;
//using NaughtyAttributes;
using UnityEngine;


public struct FarFieldHandDirection
{
    public Hand Hand;
    public Vector3 RayOrigin;
    public Vector3 RayOriginRaw;
    public Vector3 AimPosition;
    public Vector3 AimPositionRaw;
    public Vector3 Direction;
    public Vector3 DebugRayOrigin;
    public Vector3 DebugAimPosition;
    public Vector3 DebugDirection;
}

[RequireComponent(typeof(InferredBodyPositions))]
public class FarFieldDirection : MonoBehaviour
{
    public enum RayOrigin
    {
        SHOULDER,
        ELBOW,
        WRIST,
        WRIST_SHOULDER_LERP
    }

    public enum AimPosition
    {
        PREDICTED_PINCH,
        STABLE_PINCH,
    }

    public struct HandShoulder
    {
        public Hand Hand;
        public Vector3 ShoulderPosition;
    }

    public delegate void FarFieldHandDirectionFrame(FarFieldHandDirection[] farFieldHandDirection);
    public static event FarFieldHandDirectionFrame OnFarFieldHandDirectionFrame;
    public InferredBodyPositions inferredBodyPositions;
    public AimPosition aimPosition = AimPosition.STABLE_PINCH;
    public RayOrigin rayOrigin = RayOrigin.SHOULDER;
    public Vector3 elbowOffset = Vector3.zero;
    //public bool useOneEuroFilter = true;

    public Vector3 wristOffset = new Vector3(0.0425f, 0.0652f, 0.0f);
    [Range(0.01f, 1)] public float wristShoulderLerpAmount;

    public bool showCubes;
    public GameObject leftCube, rightCube;
    public bool fakeRightHandData = false;

    [HideInInspector] public HandShoulder[] HandShoulders = new HandShoulder[2];
    [HideInInspector] public FarFieldHandDirection[] FarFieldRays = new FarFieldHandDirection[2];

    private Transform transformHelper;
    //private OneEuroFilter<Vector3>[] aimPositionFilters;
    //private OneEuroFilter<Vector3>[] rayOriginFilters;

    // Debug filters used in case we need to output a value which isn't currently being filtered.
    // E.g. if we want to compare a different aim position to the one being used
    //private OneEuroFilter<Vector3>[] debugAimPositionFilters;
    //public float oneEuroBeta = 100;
    //public float oneEuroMinCutoff = 0.4f;
    //private readonly float oneEurofreq = 30;

    // Start is called before the first frame update
    void Start()
    {
        transformHelper = new GameObject("FarFieldRaycast_TransformHelper").transform;
        transformHelper.SetParent(transform);

        if (inferredBodyPositions == null)
        {
            inferredBodyPositions = GetComponent<InferredBodyPositions>();
        }

        //aimPositionFilters = new OneEuroFilter<Vector3>[2] { new OneEuroFilter<Vector3>(oneEurofreq), new OneEuroFilter<Vector3>(oneEurofreq) };
        //debugAimPositionFilters = new OneEuroFilter<Vector3>[2] { new OneEuroFilter<Vector3>(oneEurofreq), new OneEuroFilter<Vector3>(oneEurofreq) };
        //rayOriginFilters = new OneEuroFilter<Vector3>[2] { new OneEuroFilter<Vector3>(oneEurofreq), new OneEuroFilter<Vector3>(oneEurofreq) };
    }

    // Update is called once per frame
    void Update()
    {
        if (Hands.Provider == null || Hands.Provider.CurrentFrame == null)
        {
            return;
        }
        leftCube.SetActive(false);
        rightCube.SetActive(false);

        //for (int i = 0; i < aimPositionFilters.Length; i++)
        //{
        //    aimPositionFilters[i].UpdateParams(oneEurofreq, oneEuroMinCutoff, oneEuroBeta);
        //    debugAimPositionFilters[i].UpdateParams(oneEurofreq, oneEuroMinCutoff, oneEuroBeta);
        //    rayOriginFilters[i].UpdateParams(oneEurofreq, oneEuroMinCutoff, oneEuroBeta);
        //}

        PopulateHandShoulders();
        CastRays();

        //Debug.Log(FarFieldRays[1].Direction);
    }

    private void PopulateHandShoulders()
    {
        List<Hand> hands = Hands.Provider.CurrentFrame.Hands;

        HandShoulders[0].Hand = null;
        HandShoulders[1].Hand = null;

        foreach (Hand hand in hands)
        {
            int index = hand.IsLeft ? 0 : 1;
            HandShoulders[index].Hand = hand;
            HandShoulders[index].ShoulderPosition = inferredBodyPositions.ShoulderPositions[index];
        }
    }

    private void CastRays()
    {
        for (int i = 0; i < HandShoulders.Length; i++)
        {
            FarFieldRays[i].Hand = HandShoulders[i].Hand;
            if (HandShoulders[i].Hand == null)
            {
                continue;
            }

            FarFieldRays[i].AimPositionRaw = GetAimPosition(HandShoulders[i], AimPosition.PREDICTED_PINCH);
            FarFieldRays[i].RayOriginRaw = GetRayOrigin(HandShoulders[i]);

            //Filtering using the One Euro filter reduces jitter from both positions
            FarFieldRays[i].AimPosition = GetAimPosition(HandShoulders[i]);
            FarFieldRays[i].RayOrigin = FarFieldRays[i].RayOriginRaw;

            //if (useOneEuroFilter)
            //{
            //    //Filtering using the One Euro filter reduces jitter from both positions
            //    FarFieldRays[i].AimPosition = aimPositionFilters[i].Filter(GetAimPosition(HandShoulders[i]), Time.time);
            //    FarFieldRays[i].RayOrigin = rayOriginFilters[i].Filter(FarFieldRays[i].RayOriginRaw, Time.time);
            //}
            FarFieldRays[i].Direction = (FarFieldRays[i].AimPosition - FarFieldRays[i].RayOrigin).normalized;

            FarFieldRays[i].DebugAimPosition = FarFieldRays[i].AimPosition;
            FarFieldRays[i].DebugRayOrigin = FarFieldRays[i].RayOrigin;
            FarFieldRays[i].DebugDirection = (FarFieldRays[i].DebugAimPosition - FarFieldRays[i].DebugRayOrigin).normalized;

            UpdateCubePosition(HandShoulders[i]);

        }
        OnFarFieldHandDirectionFrame?.Invoke(FarFieldRays);
    }

    private Vector3 GetAimPosition(HandShoulder handShoulder)
    {
        return GetAimPosition(handShoulder, aimPosition);
    }

    private Vector3 GetAimPosition(HandShoulder handShoulder, AimPosition aimPos)
    {
        if (fakeRightHandData && handShoulder.Hand.IsRight)
        {
            return new Vector3(0.215f, 1.23f, -0.164f);
        }

        switch (aimPos)
        {
            case AimPosition.PREDICTED_PINCH:
                return handShoulder.Hand.GetPredictedPinchPosition();
            case AimPosition.STABLE_PINCH:
            default:
                return inferredBodyPositions.StablePinchPosition[handShoulder.Hand.IsLeft ? 0 : 1];
        }
    }

    private Vector3 GetRayOrigin(HandShoulder handShoulder)
    {
        Vector3 newRayOrigin = Vector3.zero;
        switch (rayOrigin)
        {
            case RayOrigin.SHOULDER:
                newRayOrigin = handShoulder.ShoulderPosition;
                break;

            case RayOrigin.ELBOW:
                newRayOrigin = GetElbowOffsetPosition(handShoulder);
                break;

            case RayOrigin.WRIST:
                newRayOrigin = GetWristOffsetPosition(handShoulder);
                break;

            case RayOrigin.WRIST_SHOULDER_LERP:
                newRayOrigin = Vector3.Lerp(GetWristOffsetPosition(handShoulder), handShoulder.ShoulderPosition, wristShoulderLerpAmount);
                break;
        }
        return newRayOrigin;
    }

    private Vector3 GetWristOffsetPosition(HandShoulder handShoulder)
    {
        if (fakeRightHandData && handShoulder.Hand.IsRight)
        {
            return new Vector3(0.199f, 1.255f, -0.307f);
        }

        Vector3 worldWristPosition = wristOffset;
        if (handShoulder.Hand.IsRight)
        {
            worldWristPosition.x = -worldWristPosition.x;
        }

        transformHelper.transform.position = handShoulder.Hand.WristPosition.ToVector3();
        transformHelper.transform.rotation = handShoulder.Hand.Rotation.ToQuaternion();

        return transformHelper.TransformPoint(worldWristPosition);
    }

    private Vector3 GetElbowOffsetPosition(HandShoulder handShoulder)
    {
        Vector3 worldElbowPosition = elbowOffset;
        if (handShoulder.Hand.IsRight)
        {
            worldElbowPosition.x = -worldElbowPosition.x;
        }

        transformHelper.transform.position = handShoulder.Hand.Arm.ElbowPosition.ToVector3();
        transformHelper.transform.rotation = handShoulder.Hand.Arm.Rotation.ToQuaternion();
        return transformHelper.TransformPoint(worldElbowPosition);
    }

    private void UpdateCubePosition(HandShoulder handShoulder)
    {
        if (!showCubes)
        {
            return;
        }

        GameObject cube;
        if (handShoulder.Hand.IsLeft)
        {
            cube = leftCube;
        }
        else
        {
            cube = rightCube;
        }
        cube.SetActive(true);
        cube.transform.position = FarFieldRays[handShoulder.Hand.IsLeft ? 0 : 1].RayOrigin;
    }
}

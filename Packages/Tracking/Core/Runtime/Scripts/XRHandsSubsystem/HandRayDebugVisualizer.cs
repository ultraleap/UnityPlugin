using Leap.InputActions;
using Leap;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Hands;
using UnityEditor.Overlays;

public class HandRayDebugVisualizer : MonoBehaviour
{
   
    [SerializeField]
    public Handedness handedness;

    [SerializeField]
    public XRHandTrackingEvents handTrackingEvents;

    [Header("Debug Gizmos")]
    [SerializeField] private bool drawDebugGizmos;

    [SerializeField] private bool drawRay = true;
    [SerializeField] private Color rayColor = Color.green;

    [SerializeField] private bool drawRayAimAndOrigin = true;
    [SerializeField] private Color rayAimAndOriginColor = Color.red;

    [SerializeField] private bool drawWristShoulderBlend = false;
    [SerializeField] private Color wristShoulderBlendColor = Color.blue;

    [SerializeField]
    public Color debugGizmoColor = Color.green;
    [SerializeField]
    public bool drawHeadPosition = true;
    [SerializeField]
    public bool drawEyePositions = true;
    [SerializeField]
    public bool drawNeckPosition = true;
    [SerializeField]
    public bool drawShoulderPositions = true;
    [SerializeField]
    public bool drawHipPositions = true;
    [SerializeField]
    public bool drawConnectionBetweenHipsAndShoulders = true;

    [SerializeField]
    LeapXRServiceProvider leapXRServiceProvider;

    [SerializeField]
    public bool drawElbowPosition = false;
    [SerializeField]
    public bool drawConnectionBetweenElbowAndWrist = false;
    [SerializeField]
    public bool drawConnectionBetweenElbowAndShoulder = false;


    private float headGizmoRadius = 0.09f;
    private float neckGizmoRadius = 0.02f;
    private float shoulderHipGizmoRadius = 0.02f;
    private float eyeGizmoRadius = 0.01f;

    private XRHand hand;
    private float gizmoRadius = 0.01f;

    private void Start()
    {
        if (!Application.isPlaying)
        {
            return;
        }

        if (handTrackingEvents != null && Application.isPlaying)
        {
            handTrackingEvents.jointsUpdated.AddListener(UpdateHands);
        }
    }

    private void OnDestroy()
    {
        if (handTrackingEvents != null)
        {
            handTrackingEvents.jointsUpdated.RemoveListener(UpdateHands);
        }
    }

    private void UpdateHands(XRHandJointsUpdatedEventArgs arg0)
    {
        if (arg0.hand.handedness == handedness)
        {
            hand = arg0.hand;
        }
    }

    private void OnDrawGizmos()
    {
        if (!drawDebugGizmos || !Application.isPlaying)
        {
            return;
        }

        if (drawRay)
        {
            Gizmos.color = rayColor;
            Gizmos.DrawRay(XRHandsInputActionUpdater.GetRayOrigin(hand), XRHandsInputActionUpdater.GetRayDirection(hand) * 10);
        }

        if (drawWristShoulderBlend)
        {
            Gizmos.color = wristShoulderBlendColor;

            Vector3 shoulderPos = XRHandsInputActionUpdater.GetInferredShoulderPosition(hand.handedness == Handedness.Left ? 0 : 1);
            Vector3 wristPos = XRHandsInputActionUpdater.GetWristOffsetPosition(hand);
            Gizmos.DrawSphere(shoulderPos, gizmoRadius);
            Gizmos.DrawSphere(wristPos, gizmoRadius);
            Gizmos.DrawLine(shoulderPos, wristPos);
        }

        if (drawRayAimAndOrigin)
        {
            Gizmos.color = rayAimAndOriginColor;
            Gizmos.DrawCube(XRHandsInputActionUpdater.GetRayOrigin(hand), Vector3.one * gizmoRadius);
            Gizmos.DrawSphere(XRHandsInputActionUpdater.GetAimPosition(hand), gizmoRadius);
        }

        if (drawHeadPosition)
        {
            Gizmos.matrix = Matrix4x4.TRS(XRHandsInputActionUpdater.Head.position, XRHandsInputActionUpdater.Head.rotation, Vector3.one);
            Gizmos.DrawCube(Vector3.zero, Vector3.one * headGizmoRadius);
            Gizmos.matrix = Matrix4x4.identity;
        }

        if (drawShoulderPositions)
        {
            Gizmos.DrawSphere(XRHandsInputActionUpdater.ShoulderPositions[0], shoulderHipGizmoRadius);
            Gizmos.DrawSphere(XRHandsInputActionUpdater.ShoulderPositions[1], shoulderHipGizmoRadius);
            Gizmos.DrawLine(XRHandsInputActionUpdater.ShoulderPositions[0], XRHandsInputActionUpdater.ShoulderPositions[1]);
        }

        if (drawNeckPosition)
        {
            Gizmos.DrawSphere(XRHandsInputActionUpdater.NeckPosition, neckGizmoRadius);
            Gizmos.DrawLine(XRHandsInputActionUpdater.Head.position, XRHandsInputActionUpdater.NeckPosition);
        }

        if (drawHipPositions)
        {
            Gizmos.color = debugGizmoColor;

            Gizmos.DrawSphere(XRHandsInputActionUpdater.HipPositions[0], shoulderHipGizmoRadius);
            Gizmos.DrawSphere(XRHandsInputActionUpdater.HipPositions[1], shoulderHipGizmoRadius);
            Gizmos.DrawLine(XRHandsInputActionUpdater.HipPositions[0], XRHandsInputActionUpdater.HipPositions[1]);
        }

        if (drawConnectionBetweenHipsAndShoulders)
        {
            Gizmos.DrawLine(XRHandsInputActionUpdater.HipPositions[0], XRHandsInputActionUpdater.ShoulderPositions[0]);
            Gizmos.DrawLine(XRHandsInputActionUpdater.HipPositions[1], XRHandsInputActionUpdater.ShoulderPositions[1]);
        }


        // Seems suspect, using the leap elbow position with XRHand data doesn't look right
        if (drawElbowPosition && leapXRServiceProvider != null)
        {
            var leapHand = leapXRServiceProvider.GetHand(handedness == Handedness.Left ? Chirality.Left : Chirality.Right);

            if (leapHand != null)
            {
                Gizmos.DrawSphere(leapHand.Arm.ElbowPosition, gizmoRadius); 

                if (drawConnectionBetweenElbowAndShoulder)
                {
                    Gizmos.DrawLine(XRHandsInputActionUpdater.ShoulderPositions[handedness == Handedness.Left ? 0 : 1],
                       leapHand.Arm.ElbowPosition);

                }

                if (drawConnectionBetweenElbowAndWrist && hand.isTracked)
                {
                    Pose wristPose;

                    if (hand.GetJoint(XRHandJointID.Wrist).TryGetPose(out wristPose))
                    {

                        Gizmos.DrawLine(wristPose.position,
                           leapHand.Arm.ElbowPosition);
                    }
                }
            }

        }
        

    }
}

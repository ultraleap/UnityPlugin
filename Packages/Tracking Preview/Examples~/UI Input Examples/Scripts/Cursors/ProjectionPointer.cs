using Leap;
using Leap.Unity;
using Leap.Unity.InputModule;
using UnityEditor;
using UnityEngine;

public class ProjectionPointer : MonoBehaviour
{
    [SerializeField] private UIInputModule uiInputModule;
    [SerializeField] Chirality chirality;
    [SerializeField] private LineRenderer line;
    [SerializeField] private UIInputCursor cursor;

    LeapProvider leapDataProvider;

    private Hand hand;

    public float lerpSpeed = 10;

    private void Awake()
    {
        leapDataProvider = uiInputModule.LeapDataProvider;
    }

    private void Update()
    {
        hand = leapDataProvider.CurrentFrame.GetHand(chirality);
        if (uiInputModule.InteractionMode == InteractionCapability.Direct)
        {
            return;
        }

        if (hand == null)
        {
            return;
        }

        var handRay = hand.IsLeft ? uiInputModule.leftHandRay : uiInputModule.rightHandRay;
        if (handRay == null)
        {
            return;
        }

        var target = cursor.transform.position;
        line.SetPosition(0, handRay.handRayDirection.VisualAimPosition);
        line.SetPosition(1, target);
    }
}
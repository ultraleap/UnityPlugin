using Leap;
using Leap.Unity;
using Leap.Unity.InputModule;
using UnityEditor;
using UnityEngine;

public class ProjectionPointer : MonoBehaviour
{
    [SerializeField] private UIInputModule module;
    [SerializeField] Chirality chirality;
    [SerializeField] private LineRenderer line;
    [SerializeField] private UIInputCursor cursor;

    LeapProvider leapDataProvider;

    private Hand hand;

    public float lerpSpeed = 10;

    private void Awake()
    {
        leapDataProvider = module.LeapDataProvider;
    }

    private void Update()
    {
        hand = leapDataProvider.CurrentFrame.GetHand(chirality);
        if (module.InteractionMode == InteractionCapability.Direct)
        {
            return;
        }

        if (hand == null)
        {
            return;
        }

        var projection = module.ProjectionOriginProvider;
        if (projection == null)
        {
            return;
        }

        var source = projection.ProjectionOriginForHand(hand);
        var target = cursor.transform.position;
        line.SetPosition(0, source);
        line.SetPosition(1, target);
    }
}
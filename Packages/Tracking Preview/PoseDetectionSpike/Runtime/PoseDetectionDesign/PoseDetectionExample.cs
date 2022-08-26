using Leap.Unity;
using System;

public class PoseDetectionExample
{
    public void ThumbsUp()
    {
        // Define shape
        HandShapeDefinition thumbsUpShape = new HandShapeDefinition();
        thumbsUpShape.ThumbStraight()
            .IndexCurled()
            .MiddleCurled()
            .RingCurled()
            .PinkyCurled();
       
        HandOrientation thumbsPointingUp = new HandOrientation();
        thumbsPointingUp.

        PoseDetector thumbsUpShapeDetector = new PoseDetector().Add(thumbsUpShape); // Provide shape and overridden detector

        var thumbsUpPoseObserver = new AndStateObserver()
             .Add(thumbsUpShapeDetector).Add(thumbsPointingUp);

        thumbsUpPoseObserver.OnStateObserved += ThumbsUpPoseObserver_OnStateObserved;
    }

    public void Pinch()
    {
        // Define shape
        HandShapeDefinition pinchShape = new HandShapeDefinition();
        pinchShape.PinkyCurled()
            .IndexIgnored()
            .MiddleIgnored()
            .RingIgnored()
            .ThumbCurled();

        HandProximity thumbAndFingerClose = new HandProximity();

        // TODO define thumb and finger proximity...
        thumbAndFingerClose.ThumbDistal.IsWithin(0.1f).Of(thumbAndFingerClose.IndexDistal);

        PoseDetector pinchDetector = new PoseDetector().Add(pinchShape); // Provide shape and overridden detector

        var pinchPoseObserver = new AndStateObserver()
             .Add(pinchDetector).Add(thumbAndFingerClose);

        pinchPoseObserver.OnStateObserved += PinchDetector_OnStateObserved;
    }

    void Point()
    {
        // Pointing
        HandShapeDefinition pointingIndexExtended = new HandShapeDefinition(),
                            pointingIndexSlightlyBentAtProximal = new HandShapeDefinition();

        /// Define two hand shapes for pointing
        pointingIndexExtended.PinkyIgnored()
            .IndexExtended()
            .MiddleIgnored()
            .RingIgnored()
            .ThumbIgnored();

        pointingIndexSlightlyBentAtProximal.PinkyIgnored()
            .IndexProximalRelativeCurl(expectedCurl: 45, tolerance: 15, hysteresisAllowance:10).IndexIntermediateRelativeCurl(ShapeValidity.ignored).IndexDistalRelativeCurl(ShapeValidity.ignored)
            .MiddleIgnored()
            .RingIgnored()
            .ThumbIgnored();

        // Allow either to be detected
        var pointingPose = new OrStateObserver();

        pointingPose.Add(new PoseDetector().Add(pointingIndexExtended))
                    .Add(new PoseDetector().Add(pointingIndexSlightlyBentAtProximal));

        pointingPose.OnStateObserved += PointingPose_OnStateObserved;
    }

    void Fist()
    { 
        // Fist ?
        HandShapeDefinition fistShape = new HandShapeDefinition();

        fistShape.ThumbProximalRelativeCurl(ShapeValidity.ignored).ThumbIntermediateRelativeCurl(expectedCurl:45, tolerance:15).ThumbDistalRelativeCurl(ShapeValidity.ignored).ThumbYaw(ShapeValidity.ignored)
            .IndexCurled()
            .MiddleCurled()
            .RingCurled()
            .PinkyCurled();

        PoseDetector fistDetector = new PoseDetector().Add(fistShape);
        fistDetector.OnStateObserved += FistDetector_OnStateObserved;
    }

    private void FistDetector_OnStateObserved(object sender, EventArgs e)
    {
        throw new NotImplementedException();
    }

    private void PinchDetector_OnStateObserved(object sender, EventArgs e)
    {
        throw new NotImplementedException();
    }

    private void PointingPose_OnStateObserved(object sender, EventArgs e)
    {
        throw new NotImplementedException();
    }

    private void ThumbsUpPoseObserver_OnStateObserved(object sender, EventArgs e)
    {
        throw new NotImplementedException();
    }
}
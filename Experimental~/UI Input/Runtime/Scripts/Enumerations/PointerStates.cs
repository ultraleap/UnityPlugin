namespace Leap.Unity.InputModule
{
    public enum PointerStates
    {
        OnCanvas,
        OnElement,
        PinchingToCanvas,  // Why is this related to not forcing tip raycast? Is this a code for when we are in projective mode ? Also, Canvas here is code for something that is not clickable, which is a canvas and other stuff too ....
        PinchingToElement, // Why is this related to not forcing tip raycast? Is this a code for when we are in projective mode ?
        NearCanvas,
        TouchingCanvas,   // Tactile mode only?
        TouchingElement,  // Tactile mode only?
        OffCanvas         // Off UI, including canvas and anything inside it....
    };
}

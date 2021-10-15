namespace Leap.Unity.InputModule
{
    /// <summary>
    /// Enumeration of states that the pointer can be in
    /// </summary>
    public enum PointerStates
    {
        OnCanvas,
        OnElement,
        PinchingToCanvas, 
        PinchingToElement,
        NearCanvas,
        TouchingCanvas,   
        TouchingElement,  
        OffCanvas         
    };
}
